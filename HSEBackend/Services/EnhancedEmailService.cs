using HSEBackend.Data;
using HSEBackend.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using System.Text;

namespace HSEBackend.Services
{
    public interface IEnhancedEmailService
    {
        // Profile assignment emails
        Task SendProfileAssignmentEmailAsync(string profileUserId, int subActionId);
        
        // HSE periodic emails
        Task SendHSEUpdateEmailsAsync();
        
        // Admin periodic emails
        Task SendAdminOverviewEmailsAsync();
        
        // Instant emails
        Task SendInstantReportNotificationToHSEAsync(int reportId);
        
        // Configuration
        Task<EmailConfiguration> GetEmailConfigurationAsync();
        Task UpdateEmailConfigurationAsync(EmailConfiguration config, string updatedByUserId);
        
        // Email templates
        Task<List<EmailTemplate>> GetEmailTemplatesAsync();
        Task UpdateEmailTemplateAsync(EmailTemplate template, string updatedByUserId);
        
        // Test methods
        Task SendTestHSEUpdateEmailsAsync();
        Task SendTestAdminOverviewEmailsAsync();
        
        // Deadline notification methods
        Task SendDeadlineNotificationsAsync();
    }

    public class EnhancedEmailService : IEnhancedEmailService
    {
        private readonly AppDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IEmailService _baseEmailService;
        private readonly ILogger<EnhancedEmailService> _logger;

        public EnhancedEmailService(
            AppDbContext context,
            UserManager<ApplicationUser> userManager,
            IEmailService baseEmailService,
            ILogger<EnhancedEmailService> logger)
        {
            _context = context;
            _userManager = userManager;
            _baseEmailService = baseEmailService;
            _logger = logger;
        }

        public async Task SendProfileAssignmentEmailAsync(string profileUserId, int subActionId)
        {
            try
            {
                var config = await GetEmailConfigurationAsync();
                if (!config.IsEmailingEnabled || !config.SendProfileAssignmentEmails)
                    return;

                var profileUser = await _userManager.FindByIdAsync(profileUserId);
                var subAction = await _context.SubActions
                    .Include(sa => sa.CorrectiveAction)
                    .ThenInclude(ca => ca.Report)
                    .FirstOrDefaultAsync(sa => sa.Id == subActionId);

                if (profileUser == null || subAction == null || string.IsNullOrEmpty(profileUser.Email))
                    return;

                var subject = $"New Assignment: {subAction.Title}";
                var body = GenerateProfileAssignmentEmailBody(profileUser, subAction);

                await _baseEmailService.SendGenericEmail(profileUser.Email, subject, body);

                // Log the email
                await LogEmailAsync(profileUser.Email, profileUser.Id, subject, "ProfileAssignment", "Sent");

                _logger.LogInformation($"Sent profile assignment email to {profileUser.Email} for sub-action {subActionId}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending profile assignment email for sub-action {SubActionId}", subActionId);
                
                // Log the error
                var user = await _userManager.FindByIdAsync(profileUserId);
                if (user != null)
                {
                    await LogEmailAsync(user.Email, profileUserId, $"Assignment notification", "ProfileAssignment", "Failed", ex.Message);
                }
            }
        }

        public async Task SendHSEUpdateEmailsAsync()
        {
            try
            {
                var config = await GetEmailConfigurationAsync();
                if (!config.IsEmailingEnabled || !config.SendHSEUpdateEmails)
                    return;

                var hseUsers = await _userManager.GetUsersInRoleAsync("HSE");
                var activeHSEUsers = hseUsers.Where(u => u.IsActive).ToList();

                var cutoffTime = DateTime.UtcNow.AddMinutes(-config.HSEUpdateIntervalMinutes);

                foreach (var hseUser in activeHSEUsers)
                {
                    var updateData = await GenerateHSEUpdateDataAsync(hseUser.Id, cutoffTime);
                    
                    _logger.LogInformation($"📊 HSE Update Data for {hseUser.Email} (ID: {hseUser.Id}):");
                    _logger.LogInformation($"   - Total Reports: {updateData.TotalReports}");
                    _logger.LogInformation($"   - New Reports: {updateData.NewReports}");
                    _logger.LogInformation($"   - Total Actions: {updateData.TotalActions}");
                    _logger.LogInformation($"   - Has Updates: {updateData.HasUpdates}");
                    _logger.LogInformation($"   - Interval Hours: {updateData.IntervalHours}");
                    
                    // Send email to ALL active HSE users regardless of updates
                    var subject = $"HSE Update - {DateTime.Now:MM/dd/yyyy HH:mm}";
                    var body = GenerateHSEUpdateEmailBody(hseUser, updateData);

                    await _baseEmailService.SendGenericEmail(hseUser.Email!, subject, body);
                    await LogEmailAsync(hseUser.Email!, hseUser.Id, subject, "HSEUpdate", "Sent");

                    _logger.LogInformation($"✅ Sent HSE update email to {hseUser.Email} (HasUpdates: {updateData.HasUpdates})");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending HSE update emails");
            }
        }

        public async Task SendAdminOverviewEmailsAsync()
        {
            try
            {
                var config = await GetEmailConfigurationAsync();
                if (!config.IsEmailingEnabled || !config.SendAdminOverviewEmails)
                    return;

                var adminUsers = await GetAdminUsersForEmailsAsync(config);
                var cutoffTime = DateTime.UtcNow.AddMinutes(-config.AdminOverviewIntervalMinutes);

                var overviewData = await GenerateAdminOverviewDataAsync(cutoffTime);

                foreach (var adminUser in adminUsers)
                {
                    var subject = $"Admin Overview - {DateTime.Now:MM/dd/yyyy HH:mm}";
                    var body = GenerateAdminOverviewEmailBody(adminUser, overviewData);

                    await _baseEmailService.SendGenericEmail(adminUser.Email!, subject, body);
                    await LogEmailAsync(adminUser.Email!, adminUser.Id, subject, "AdminOverview", "Sent");

                    _logger.LogInformation($"Sent admin overview email to {adminUser.Email}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending admin overview emails");
            }
        }

        public async Task SendInstantReportNotificationToHSEAsync(int reportId)
        {
            try
            {
                var config = await GetEmailConfigurationAsync();
                if (!config.IsEmailingEnabled || !config.SendHSEInstantReportEmails)
                    return;

                var report = await _context.Reports
                    .Include(r => r.ZoneRef)
                    .FirstOrDefaultAsync(r => r.Id == reportId);

                if (report == null) return;

                // Find the reporter user
                var reporter = await _userManager.Users
                    .FirstOrDefaultAsync(u => u.CompanyId == report.ReporterCompanyId && u.IsActive);

                // Find HSE users for this zone
                var hseUsers = await GetHSEUsersForZoneAsync(report.ZoneRef?.Name ?? report.Zone);

                foreach (var hseUser in hseUsers)
                {
                    var subject = $"Instant Alert: New {report.Type} Report - {report.Title}";
                    var body = GenerateInstantReportEmailBody(hseUser, report, reporter);

                    await _baseEmailService.SendGenericEmail(hseUser.Email!, subject, body);
                    await LogEmailAsync(hseUser.Email!, hseUser.Id, subject, "InstantReport", "Sent");

                    _logger.LogInformation($"Sent instant report notification to {hseUser.Email} for report {reportId}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending instant report notification for report {ReportId}", reportId);
            }
        }

        public async Task<EmailConfiguration> GetEmailConfigurationAsync()
        {
            try
            {
                _logger.LogInformation("GetEmailConfigurationAsync: Starting");
                
                _logger.LogInformation("GetEmailConfigurationAsync: Querying database");
                var config = await _context.EmailConfigurations
                    .Include(ec => ec.UpdatedByUser)
                    .FirstOrDefaultAsync();
                
                _logger.LogInformation("GetEmailConfigurationAsync: Query completed, config is null: {IsNull}", config == null);
                
                if (config == null)
                {
                    _logger.LogInformation("GetEmailConfigurationAsync: Creating default configuration");
                    // Create default configuration
                    config = new EmailConfiguration
                    {
                        IsEmailingEnabled = true,
                        SendProfileAssignmentEmails = true,
                        SendHSEUpdateEmails = true,
                        HSEUpdateIntervalMinutes = 360,
                        SendHSEInstantReportEmails = true,
                        SendAdminOverviewEmails = true,
                        AdminOverviewIntervalMinutes = 360,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    };

                    _logger.LogInformation("GetEmailConfigurationAsync: Adding to context");
                    _context.EmailConfigurations.Add(config);
                    
                    _logger.LogInformation("GetEmailConfigurationAsync: Saving changes");
                    await _context.SaveChangesAsync();
                    
                    _logger.LogInformation("GetEmailConfigurationAsync: Default configuration created with ID: {Id}", config.Id);
                }

                _logger.LogInformation("GetEmailConfigurationAsync: Returning config with ID: {Id}", config.Id);
                return config;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "GetEmailConfigurationAsync: Error occurred - {Message}", ex.Message);
                _logger.LogError("GetEmailConfigurationAsync: Stack trace - {StackTrace}", ex.StackTrace);
                throw;
            }
        }

        public async Task UpdateEmailConfigurationAsync(EmailConfiguration config, string updatedByUserId)
        {
            var existingConfig = await _context.EmailConfigurations.FirstOrDefaultAsync();
            
            if (existingConfig != null)
            {
                existingConfig.IsEmailingEnabled = config.IsEmailingEnabled;
                existingConfig.SendProfileAssignmentEmails = config.SendProfileAssignmentEmails;
                existingConfig.SendHSEUpdateEmails = config.SendHSEUpdateEmails;
                existingConfig.HSEUpdateIntervalMinutes = config.HSEUpdateIntervalMinutes;
                existingConfig.SendHSEInstantReportEmails = config.SendHSEInstantReportEmails;
                existingConfig.SendAdminOverviewEmails = config.SendAdminOverviewEmails;
                existingConfig.AdminOverviewIntervalMinutes = config.AdminOverviewIntervalMinutes;
                existingConfig.SuperAdminUserIds = config.SuperAdminUserIds;
                existingConfig.UpdatedAt = DateTime.UtcNow;
                existingConfig.UpdatedByUserId = updatedByUserId;
            }
            else
            {
                config.CreatedAt = DateTime.UtcNow;
                config.UpdatedAt = DateTime.UtcNow;
                config.UpdatedByUserId = updatedByUserId;
                _context.EmailConfigurations.Add(config);
            }

            await _context.SaveChangesAsync();
        }

        public async Task<List<EmailTemplate>> GetEmailTemplatesAsync()
        {
            return await _context.EmailTemplates
                .Where(t => t.IsActive)
                .OrderBy(t => t.TemplateName)
                .ToListAsync();
        }

        public async Task UpdateEmailTemplateAsync(EmailTemplate template, string updatedByUserId)
        {
            var existingTemplate = await _context.EmailTemplates
                .FirstOrDefaultAsync(t => t.TemplateName == template.TemplateName);

            if (existingTemplate != null)
            {
                existingTemplate.Subject = template.Subject;
                existingTemplate.HtmlContent = template.HtmlContent;
                existingTemplate.PlainTextContent = template.PlainTextContent;
                existingTemplate.UpdatedAt = DateTime.UtcNow;
                existingTemplate.UpdatedByUserId = updatedByUserId;
            }
            else
            {
                template.CreatedAt = DateTime.UtcNow;
                template.UpdatedAt = DateTime.UtcNow;
                template.UpdatedByUserId = updatedByUserId;
                _context.EmailTemplates.Add(template);
            }

            await _context.SaveChangesAsync();
        }

        public async Task SendTestHSEUpdateEmailsAsync()
        {
            try
            {
                var config = await GetEmailConfigurationAsync();
                if (!config.IsEmailingEnabled || !config.SendHSEUpdateEmails)
                {
                    _logger.LogInformation("HSE test emails skipped - emailing disabled in configuration");
                    return;
                }

                var hseUsers = await _userManager.GetUsersInRoleAsync("HSE");
                var activeHSEUsers = hseUsers.Where(u => u.IsActive && !string.IsNullOrEmpty(u.Email)).ToList();

                _logger.LogInformation($"Found {activeHSEUsers.Count} active HSE users for test emails");

                if (!activeHSEUsers.Any())
                {
                    _logger.LogWarning("No active HSE users found for test emails");
                    return;
                }

                var cutoffTime = DateTime.UtcNow.AddMinutes(-config.HSEUpdateIntervalMinutes);

                foreach (var hseUser in activeHSEUsers)
                {
                    // Use real data from the database (same as automatic emails)
                    var updateData = await GenerateHSEUpdateDataAsync(hseUser.Id, cutoffTime);
                    
                    // Force sending test emails even if no updates (add test indicator)
                    updateData.HasUpdates = true;
                    
                    _logger.LogInformation($"📊 TEST HSE Update Data for {hseUser.Email} (ID: {hseUser.Id}):");
                    _logger.LogInformation($"   - Total Reports: {updateData.TotalReports}");
                    _logger.LogInformation($"   - New Reports: {updateData.NewReports}");
                    _logger.LogInformation($"   - Total Actions: {updateData.TotalActions}");
                    _logger.LogInformation($"   - Interval Hours: {updateData.IntervalHours}");

                    var subject = $"🧪 TEST HSE Update - {DateTime.Now:MM/dd/yyyy HH:mm}";
                    var body = GenerateHSEUpdateEmailBody(hseUser, updateData);

                    await _baseEmailService.SendGenericEmail(hseUser.Email!, subject, body);
                    await LogEmailAsync(hseUser.Email!, hseUser.Id, subject, "TestHSEUpdate", "Sent");

                    _logger.LogInformation($"✅ Sent TEST HSE update email to {hseUser.Email}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending TEST HSE update emails");
                throw;
            }
        }

        public async Task SendTestAdminOverviewEmailsAsync()
        {
            try
            {
                var config = await GetEmailConfigurationAsync();
                if (!config.IsEmailingEnabled || !config.SendAdminOverviewEmails)
                {
                    _logger.LogInformation("Admin test emails skipped - emailing disabled in configuration");
                    return;
                }

                var adminUsers = await GetAdminUsersForEmailsAsync(config);
                
                _logger.LogInformation($"Found {adminUsers.Count} admin users for test emails");

                if (!adminUsers.Any())
                {
                    _logger.LogWarning("No admin users found for test emails");
                    return;
                }

                // Use real data from database (same as automatic emails)
                var cutoffTime = DateTime.UtcNow.AddMinutes(-config.AdminOverviewIntervalMinutes);
                var overviewData = await GenerateAdminOverviewDataAsync(cutoffTime);

                _logger.LogInformation($"📊 TEST Admin Overview Data:");
                _logger.LogInformation($"   - Interval Hours: {overviewData.IntervalHours}");
                _logger.LogInformation($"   - Total Reports: {overviewData.TotalReports}, New Reports: {overviewData.NewReports}");
                _logger.LogInformation($"   - Total Actions: {overviewData.TotalActions}, New Actions: {overviewData.NewActions}");
                _logger.LogInformation($"   - Completed Actions: {overviewData.CompletedActions}, Not Started: {overviewData.NotStartedActions}");
                _logger.LogInformation($"   - Overdue Items: {overviewData.OverdueItems}, Pending Registrations: {overviewData.PendingRegistrations}");

                foreach (var adminUser in adminUsers)
                {
                    var subject = $"🧪 TEST Admin Overview - {DateTime.Now:MM/dd/yyyy HH:mm}";
                    var body = GenerateAdminOverviewEmailBody(adminUser, overviewData);

                    await _baseEmailService.SendGenericEmail(adminUser.Email!, subject, body);
                    await LogEmailAsync(adminUser.Email!, adminUser.Id, subject, "TestAdminOverview", "Sent");

                    _logger.LogInformation($"✅ Sent TEST admin overview email to {adminUser.Email}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending TEST admin overview emails");
                throw;
            }
        }

        public async Task SendDeadlineNotificationsAsync()
        {
            try
            {
                var config = await GetEmailConfigurationAsync();
                if (!config.IsEmailingEnabled)
                {
                    _logger.LogInformation("Deadline notifications skipped - emailing disabled in configuration");
                    return;
                }

                var today = DateTime.UtcNow.Date;
                var threeDaysFromNow = DateTime.UtcNow.AddDays(3);

                // Find overdue and approaching deadline CORRECTIVE actions (ignore fake Actions table)
                var overdueActions = await _context.CorrectiveActions
                    .Include(ca => ca.Report)
                    .Where(ca => ca.DueDate.Date < today && !ca.IsCompleted)
                    .ToListAsync();

                var approachingActions = await _context.CorrectiveActions
                    .Include(ca => ca.Report)
                    .Where(ca => ca.DueDate <= threeDaysFromNow && ca.DueDate.Date >= today && !ca.IsCompleted)
                    .ToListAsync();

                // Find overdue and approaching deadline sub-actions
                var overdueSubActions = await _context.SubActions
                    .Include(sa => sa.AssignedTo)
                    .Include(sa => sa.CorrectiveAction).ThenInclude(ca => ca.CreatedByHSE)
                    .Include(sa => sa.Action)
                    .Where(sa => sa.DueDate.HasValue && sa.DueDate.Value.Date < today && sa.Status != "Completed")
                    .ToListAsync();

                var approachingSubActions = await _context.SubActions
                    .Include(sa => sa.AssignedTo)
                    .Include(sa => sa.CorrectiveAction).ThenInclude(ca => ca.CreatedByHSE)
                    .Include(sa => sa.Action)
                    .Where(sa => sa.DueDate.HasValue && sa.DueDate.Value <= threeDaysFromNow && sa.DueDate.Value.Date >= today && sa.Status != "Completed")
                    .ToListAsync();

                // Send action deadline notifications to HSE users
                await SendActionDeadlineNotificationsAsync(overdueActions, approachingActions);

                // Send sub-action deadline notifications to assigned users and HSE creators
                await SendSubActionDeadlineNotificationsAsync(overdueSubActions, approachingSubActions);

                _logger.LogInformation($"Sent deadline notifications: {overdueActions.Count + approachingActions.Count} actions, {overdueSubActions.Count + approachingSubActions.Count} sub-actions");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending deadline notifications");
                throw;
            }
        }

        private async Task SendActionDeadlineNotificationsAsync(List<CorrectiveAction> overdueActions, List<CorrectiveAction> approachingActions)
        {
            var hseUsers = await _userManager.GetUsersInRoleAsync("HSE");
            var activeHSEUsers = hseUsers.Where(u => u.IsActive && !string.IsNullOrEmpty(u.Email)).ToList();

            foreach (var hseUser in activeHSEUsers)
            {
                var userOverdueActions = overdueActions.Where(a => a.Report?.AssignedHSEId == hseUser.Id).ToList();
                var userApproachingActions = approachingActions.Where(a => a.Report?.AssignedHSEId == hseUser.Id).ToList();

                if (userOverdueActions.Any() || userApproachingActions.Any())
                {
                    var subject = $"⚠️ Action Deadline Alert - {userOverdueActions.Count} Overdue, {userApproachingActions.Count} Approaching";
                    var body = GenerateActionDeadlineEmailBody(hseUser, userOverdueActions, userApproachingActions);

                    await _baseEmailService.SendGenericEmail(hseUser.Email!, subject, body);
                    await LogEmailAsync(hseUser.Email!, hseUser.Id, subject, "ActionDeadlineAlert", "Sent");

                    _logger.LogInformation($"Sent action deadline notification to HSE user {hseUser.Email}");
                }
            }
        }

        private async Task SendSubActionDeadlineNotificationsAsync(List<SubAction> overdueSubActions, List<SubAction> approachingSubActions)
        {
            // Group by assigned user
            var userSubActions = overdueSubActions.Concat(approachingSubActions)
                .Where(sa => !string.IsNullOrEmpty(sa.AssignedToId))
                .GroupBy(sa => sa.AssignedToId)
                .ToList();

            foreach (var userGroup in userSubActions)
            {
                var assignedUser = await _userManager.FindByIdAsync(userGroup.Key!);
                if (assignedUser != null && assignedUser.IsActive && !string.IsNullOrEmpty(assignedUser.Email))
                {
                    var userOverdue = userGroup.Where(sa => sa.DueDate!.Value.Date < DateTime.UtcNow.Date).ToList();
                    var userApproaching = userGroup.Where(sa => sa.DueDate!.Value.Date >= DateTime.UtcNow.Date).ToList();

                    var subject = $"📋 Task Deadline Alert - {userOverdue.Count} Overdue, {userApproaching.Count} Due Soon";
                    var body = GenerateSubActionDeadlineEmailBody(assignedUser, userOverdue, userApproaching, isForAssignedUser: true);

                    await _baseEmailService.SendGenericEmail(assignedUser.Email!, subject, body);
                    await LogEmailAsync(assignedUser.Email!, assignedUser.Id, subject, "SubActionDeadlineAlert", "Sent");

                    _logger.LogInformation($"Sent sub-action deadline notification to assigned user {assignedUser.Email}");
                }
            }

            // Also notify HSE users who created these sub-actions
            var hseCreatorSubActions = overdueSubActions.Concat(approachingSubActions)
                .Where(sa => sa.CorrectiveAction?.CreatedByHSEId != null)
                .GroupBy(sa => sa.CorrectiveAction!.CreatedByHSEId)
                .ToList();

            foreach (var hseGroup in hseCreatorSubActions)
            {
                var hseUser = await _userManager.FindByIdAsync(hseGroup.Key!);
                if (hseUser != null && hseUser.IsActive && !string.IsNullOrEmpty(hseUser.Email))
                {
                    var userOverdue = hseGroup.Where(sa => sa.DueDate!.Value.Date < DateTime.UtcNow.Date).ToList();
                    var userApproaching = hseGroup.Where(sa => sa.DueDate!.Value.Date >= DateTime.UtcNow.Date).ToList();

                    var subject = $"🔧 Sub-Action Deadline Alert (HSE) - {userOverdue.Count} Overdue, {userApproaching.Count} Due Soon";
                    var body = GenerateSubActionDeadlineEmailBody(hseUser, userOverdue, userApproaching, isForAssignedUser: false);

                    await _baseEmailService.SendGenericEmail(hseUser.Email!, subject, body);
                    await LogEmailAsync(hseUser.Email!, hseUser.Id, subject, "SubActionDeadlineAlertHSE", "Sent");

                    _logger.LogInformation($"Sent sub-action deadline notification to HSE creator {hseUser.Email}");
                }
            }
        }

        private string GenerateActionDeadlineEmailBody(ApplicationUser hseUser, List<CorrectiveAction> overdueActions, List<CorrectiveAction> approachingActions)
        {
            var sb = new StringBuilder();
            sb.AppendLine($"<h2>⚠️ Action Deadline Alert</h2>");
            sb.AppendLine($"<p>Dear {hseUser.FullName},</p>");
            sb.AppendLine($"<p>You have actions that require immediate attention due to approaching or overdue deadlines:</p>");

            if (overdueActions.Any())
            {
                sb.AppendLine($"<div style='background-color: #ffebee; padding: 15px; border-left: 4px solid #f44336; margin: 15px 0;'>");
                sb.AppendLine($"<h3 style='color: #d32f2f; margin-top: 0;'>🚨 Overdue Actions ({overdueActions.Count})</h3>");
                sb.AppendLine($"<ul>");
                foreach (var action in overdueActions.Take(10))
                {
                    var daysOverdue = (DateTime.UtcNow.Date - action.DueDate.Date).Days;
                    sb.AppendLine($"<li><strong>{action.Title}</strong> - Overdue by {daysOverdue} days (Due: {action.DueDate:MM/dd/yyyy})</li>");
                }
                if (overdueActions.Count > 10)
                {
                    sb.AppendLine($"<li>... and {overdueActions.Count - 10} more overdue actions</li>");
                }
                sb.AppendLine($"</ul>");
                sb.AppendLine($"</div>");
            }

            if (approachingActions.Any())
            {
                sb.AppendLine($"<div style='background-color: #fff3e0; padding: 15px; border-left: 4px solid #ff9800; margin: 15px 0;'>");
                sb.AppendLine($"<h3 style='color: #f57c00; margin-top: 0;'>⏰ Actions Due Soon ({approachingActions.Count})</h3>");
                sb.AppendLine($"<ul>");
                foreach (var action in approachingActions.Take(10))
                {
                    var daysUntilDue = (action.DueDate.Date - DateTime.UtcNow.Date).Days;
                    sb.AppendLine($"<li><strong>{action.Title}</strong> - Due in {daysUntilDue} days ({action.DueDate:MM/dd/yyyy})</li>");
                }
                if (approachingActions.Count > 10)
                {
                    sb.AppendLine($"<li>... and {approachingActions.Count - 10} more actions due soon</li>");
                }
                sb.AppendLine($"</ul>");
                sb.AppendLine($"</div>");
            }

            sb.AppendLine($"<div style='text-align: center; margin: 30px 0;'>");
            sb.AppendLine($"<a href='http://192.168.0.245:4200/hse-dashboard' style='background-color: #f44336; color: white; padding: 12px 24px; text-decoration: none; border-radius: 5px; display: inline-block;'>View All Actions</a>");
            sb.AppendLine($"</div>");

            sb.AppendLine($"<p>Please review these actions and take appropriate measures to ensure timely completion.</p>");
            sb.AppendLine($"<p><strong>HSE Management System</strong></p>");

            return sb.ToString();
        }

        private string GenerateSubActionDeadlineEmailBody(ApplicationUser user, List<SubAction> overdueSubActions, List<SubAction> approachingSubActions, bool isForAssignedUser)
        {
            var sb = new StringBuilder();
            sb.AppendLine($"<h2>📋 Task Deadline Alert</h2>");
            sb.AppendLine($"<p>Dear {user.FullName},</p>");
            
            if (isForAssignedUser)
            {
                sb.AppendLine($"<p>You have tasks that require immediate attention due to approaching or overdue deadlines:</p>");
            }
            else
            {
                sb.AppendLine($"<p>Tasks you created have approaching or overdue deadlines and require follow-up:</p>");
            }

            if (overdueSubActions.Any())
            {
                sb.AppendLine($"<div style='background-color: #ffebee; padding: 15px; border-left: 4px solid #f44336; margin: 15px 0;'>");
                sb.AppendLine($"<h3 style='color: #d32f2f; margin-top: 0;'>🚨 Overdue Tasks ({overdueSubActions.Count})</h3>");
                sb.AppendLine($"<ul>");
                foreach (var subAction in overdueSubActions.Take(10))
                {
                    var daysOverdue = (DateTime.UtcNow.Date - subAction.DueDate!.Value.Date).Days;
                    var assignedInfo = isForAssignedUser ? "" : $" (Assigned to: {subAction.AssignedTo?.FullName ?? "Unknown"})";
                    sb.AppendLine($"<li><strong>{subAction.Title}</strong> - Overdue by {daysOverdue} days (Due: {subAction.DueDate.Value:MM/dd/yyyy}){assignedInfo}</li>");
                }
                if (overdueSubActions.Count > 10)
                {
                    sb.AppendLine($"<li>... and {overdueSubActions.Count - 10} more overdue tasks</li>");
                }
                sb.AppendLine($"</ul>");
                sb.AppendLine($"</div>");
            }

            if (approachingSubActions.Any())
            {
                sb.AppendLine($"<div style='background-color: #fff3e0; padding: 15px; border-left: 4px solid #ff9800; margin: 15px 0;'>");
                sb.AppendLine($"<h3 style='color: #f57c00; margin-top: 0;'>⏰ Tasks Due Soon ({approachingSubActions.Count})</h3>");
                sb.AppendLine($"<ul>");
                foreach (var subAction in approachingSubActions.Take(10))
                {
                    var daysUntilDue = (subAction.DueDate!.Value.Date - DateTime.UtcNow.Date).Days;
                    var assignedInfo = isForAssignedUser ? "" : $" (Assigned to: {subAction.AssignedTo?.FullName ?? "Unknown"})";
                    sb.AppendLine($"<li><strong>{subAction.Title}</strong> - Due in {daysUntilDue} days ({subAction.DueDate.Value:MM/dd/yyyy}){assignedInfo}</li>");
                }
                if (approachingSubActions.Count > 10)
                {
                    sb.AppendLine($"<li>... and {approachingSubActions.Count - 10} more tasks due soon</li>");
                }
                sb.AppendLine($"</ul>");
                sb.AppendLine($"</div>");
            }

            var dashboardUrl = isForAssignedUser ? "http://192.168.0.245:4200/profile/tasks" : "http://192.168.0.245:4200/hse-dashboard";
            sb.AppendLine($"<div style='text-align: center; margin: 30px 0;'>");
            sb.AppendLine($"<a href='{dashboardUrl}' style='background-color: #f44336; color: white; padding: 12px 24px; text-decoration: none; border-radius: 5px; display: inline-block;'>View All Tasks</a>");
            sb.AppendLine($"</div>");

            if (isForAssignedUser)
            {
                sb.AppendLine($"<p>Please complete these tasks as soon as possible and update their status in the system.</p>");
            }
            else
            {
                sb.AppendLine($"<p>Please follow up with the assigned users to ensure timely completion of these tasks.</p>");
            }
            
            sb.AppendLine($"<p><strong>HSE Management System</strong></p>");

            return sb.ToString();
        }

        // Private helper methods
        private string GenerateProfileAssignmentEmailBody(ApplicationUser profileUser, SubAction subAction)
        {
            var sb = new StringBuilder();
            sb.AppendLine($"<h2>📋 New Task Assignment</h2>");
            sb.AppendLine($"<p>Hello <strong>{profileUser.FullName}</strong>,</p>");
            sb.AppendLine($"<p>You have been assigned a new task that requires your attention:</p>");
            
            sb.AppendLine($"<div style='background-color: #e3f2fd; padding: 20px; border-left: 4px solid #2196F3; margin: 20px 0; border-radius: 5px;'>");
            sb.AppendLine($"<h3 style='color: #1976D2; margin-top: 0;'>{subAction.Title}</h3>");
            
            if (!string.IsNullOrEmpty(subAction.Description))
            {
                sb.AppendLine($"<p><strong>Description:</strong></p>");
                sb.AppendLine($"<p style='background-color: #f5f5f5; padding: 10px; border-radius: 3px;'>{subAction.Description}</p>");
            }
            
            if (subAction.DueDate.HasValue)
            {
                var daysUntilDue = (subAction.DueDate.Value.Date - DateTime.UtcNow.Date).Days;
                var dueDateColor = daysUntilDue <= 3 ? "#d32f2f" : daysUntilDue <= 7 ? "#ff9800" : "#388e3c";
                sb.AppendLine($"<p><strong>Due Date:</strong> <span style='color: {dueDateColor}; font-weight: bold;'>{subAction.DueDate.Value:MM/dd/yyyy}</span></p>");
                
                if (daysUntilDue < 0)
                {
                    sb.AppendLine($"<p style='color: #d32f2f; font-weight: bold;'>⚠️ This task is overdue by {Math.Abs(daysUntilDue)} days!</p>");
                }
                else if (daysUntilDue <= 3)
                {
                    sb.AppendLine($"<p style='color: #ff9800; font-weight: bold;'>⏰ This task is due in {daysUntilDue} days</p>");
                }
            }
            
            sb.AppendLine($"<p><strong>Current Status:</strong> <span style='background-color: #e8f5e8; padding: 3px 8px; border-radius: 3px; color: #2e7d32;'>{subAction.Status}</span></p>");
            sb.AppendLine($"</div>");
            
            sb.AppendLine($"<div style='background-color: #fff3e0; padding: 15px; border-radius: 5px; margin: 20px 0;'>");
            sb.AppendLine($"<p style='margin: 0;'><strong>📞 Contact Information:</strong></p>");
            sb.AppendLine($"<p style='margin: 5px 0;'>Your Company ID: <strong>{profileUser.CompanyId}</strong></p>");
            sb.AppendLine($"<p style='margin: 5px 0;'>For questions about this task, please contact the HSE team.</p>");
            sb.AppendLine($"</div>");
            
            sb.AppendLine($"<div style='text-align: center; margin: 30px 0;'>");
            sb.AppendLine($"<a href='http://192.168.0.245:4200/profile/tasks' style='background-color: #2196F3; color: white; padding: 12px 24px; text-decoration: none; border-radius: 5px; display: inline-block;'>View Task Details</a>");
            sb.AppendLine($"</div>");
            
            sb.AppendLine($"<p>Please update the task status when you have completed your work.</p>");
            sb.AppendLine($"<p>Thank you for your attention to safety,<br><strong>HSE Management Team</strong><br>TE Connectivity</p>");

            return sb.ToString();
        }

        private string GenerateHSEUpdateEmailBody(ApplicationUser hseUser, HSEUpdateData updateData)
        {
            var sb = new StringBuilder();
            sb.AppendLine($"<h2>HSE Update for {hseUser.FullName}</h2>");
            sb.AppendLine($"<p>Here's your {updateData.IntervalHours}-hour update on HSE activities assigned to you:</p>");

            // Reports Summary
            sb.AppendLine($"<div style='background-color: #e8f4fd; padding: 15px; border-left: 4px solid #2196F3; margin: 15px 0;'>");
            sb.AppendLine($"<h3 style='color: #1976D2; margin-top: 0;'>📋 Reports Summary</h3>");
            sb.AppendLine($"<ul style='margin: 0; padding-left: 20px;'>");
            sb.AppendLine($"<li><strong>Total assigned reports:</strong> {updateData.TotalReports}</li>");
            sb.AppendLine($"<li><strong>New reports (last {updateData.IntervalHours}h):</strong> {updateData.NewReports}</li>");
            sb.AppendLine($"<li><strong>Unopened reports:</strong> {updateData.NotStartedReports}</li>");
            sb.AppendLine($"</ul>");
            sb.AppendLine($"</div>");

            // Actions Summary
            sb.AppendLine($"<div style='background-color: #f3e5f5; padding: 15px; border-left: 4px solid #9C27B0; margin: 15px 0;'>");
            sb.AppendLine($"<h3 style='color: #7B1FA2; margin-top: 0;'>⚡ Actions Summary</h3>");
            sb.AppendLine($"<ul style='margin: 0; padding-left: 20px;'>");
            sb.AppendLine($"<li><strong>Total actions:</strong> {updateData.TotalActions}</li>");
            sb.AppendLine($"<li><strong>New actions (last {updateData.IntervalHours}h):</strong> {updateData.NewActions}</li>");
            sb.AppendLine($"<li><strong>Not started:</strong> {updateData.NotStartedActions}</li>");
            sb.AppendLine($"<li><strong>Approaching deadline:</strong> {updateData.ApproachingDeadlines}</li>");
            sb.AppendLine($"<li style='color: #d32f2f;'><strong>Overdue:</strong> {updateData.OverdueActions}</li>");
            sb.AppendLine($"</ul>");
            sb.AppendLine($"</div>");

            // Sub-Actions Summary (only show if there are sub-actions)
            if (updateData.TotalSubActions > 0)
            {
                sb.AppendLine($"<div style='background-color: #fff3e0; padding: 15px; border-left: 4px solid #FF9800; margin: 15px 0;'>");
                sb.AppendLine($"<h3 style='color: #F57C00; margin-top: 0;'>🔧 Sub-Actions Overview</h3>");
                sb.AppendLine($"<ul style='margin: 0; padding-left: 20px;'>");
                sb.AppendLine($"<li><strong>Total sub-actions:</strong> {updateData.TotalSubActions}</li>");
                sb.AppendLine($"<li><strong>New sub-actions (last {updateData.IntervalHours}h):</strong> {updateData.NewSubActions}</li>");
                sb.AppendLine($"<li><strong>Not started:</strong> {updateData.NotStartedSubActions}</li>");
                sb.AppendLine($"<li><strong>In progress:</strong> {updateData.InProgressSubActions}</li>");
                sb.AppendLine($"<li><strong>Completed:</strong> {updateData.CompletedSubActions}</li>");
                sb.AppendLine($"<li><strong>Approaching deadline:</strong> {updateData.ApproachingDeadlineSubActions}</li>");
                sb.AppendLine($"<li style='color: #d32f2f;'><strong>Overdue:</strong> {updateData.OverdueSubActions}</li>");
                sb.AppendLine($"</ul>");
                sb.AppendLine($"</div>");
            }

            // Activity Summary section removed as requested

            sb.AppendLine($"<div style='text-align: center; margin: 30px 0;'>");
            sb.AppendLine($"<a href='http://192.168.0.245:4200/hse-dashboard' style='background-color: #2c5aa0; color: white; padding: 12px 24px; text-decoration: none; border-radius: 5px; display: inline-block;'>Access HSE System</a>");
            sb.AppendLine($"</div>");

            sb.AppendLine($"<p>Best regards,<br><strong>HSE Management System</strong></p>");

            return sb.ToString();
        }

        private string GenerateAdminOverviewEmailBody(ApplicationUser adminUser, AdminOverviewData overviewData)
        {
            var sb = new StringBuilder();
            sb.AppendLine($"<h2>Admin Overview for {adminUser.FullName}</h2>");
            sb.AppendLine($"<p>Here's your {overviewData.IntervalHours}-hour overview of the HSE system:</p>");

            sb.AppendLine($"<div style='background-color: #f8f9fa; padding: 15px; margin: 15px 0;'>");
            sb.AppendLine($"<h3>Reports Summary</h3>");
            sb.AppendLine($"<ul>");
            sb.AppendLine($"<li>Total reports: {overviewData.TotalReports}</li>");
            sb.AppendLine($"<li>New reports: {overviewData.NewReports}</li>");
            sb.AppendLine($"<li>Opened reports: {overviewData.OpenedReports}</li>");
            sb.AppendLine($"<li>Closed reports: {overviewData.CompletedReports}</li>");
            sb.AppendLine($"</ul>");
            sb.AppendLine($"</div>");

            sb.AppendLine($"<div style='background-color: #f0f8f0; padding: 15px; border-left: 4px solid #28a745; margin: 15px 0;'>");
            sb.AppendLine($"<h3 style='color: #155724; margin-top: 0;'>⚡ Actions Summary</h3>");
            sb.AppendLine($"<ul style='margin: 0; padding-left: 20px;'>");
            sb.AppendLine($"<li><strong>Total actions:</strong> {overviewData.TotalActions}</li>");
            sb.AppendLine($"<li><strong>New actions created:</strong> {overviewData.NewActions}</li>");
            sb.AppendLine($"<li><strong>Actions completed:</strong> {overviewData.CompletedActions}</li>");
            sb.AppendLine($"<li><strong>Not started actions:</strong> {overviewData.NotStartedActions}</li>");
            sb.AppendLine($"<li style='color: #d32f2f;'><strong>Overdue actions:</strong> {overviewData.OverdueItems}</li>");
            sb.AppendLine($"</ul>");
            sb.AppendLine($"</div>");

            // Sub-Actions Summary (only show if there are sub-actions)
            if (overviewData.TotalSubActions > 0)
            {
                sb.AppendLine($"<div style='background-color: #fff3e0; padding: 15px; border-left: 4px solid #FF9800; margin: 15px 0;'>");
                sb.AppendLine($"<h3 style='color: #F57C00; margin-top: 0;'>🔧 Sub-Actions Overview</h3>");
                sb.AppendLine($"<ul style='margin: 0; padding-left: 20px;'>");
                sb.AppendLine($"<li><strong>Total sub-actions:</strong> {overviewData.TotalSubActions}</li>");
                sb.AppendLine($"<li><strong>New sub-actions (last {overviewData.IntervalHours}h):</strong> {overviewData.NewSubActions}</li>");
                sb.AppendLine($"<li><strong>Not started:</strong> {overviewData.NotStartedSubActions}</li>");
                sb.AppendLine($"<li><strong>In progress:</strong> {overviewData.InProgressSubActions}</li>");
                sb.AppendLine($"<li><strong>Completed:</strong> {overviewData.CompletedSubActions}</li>");
                sb.AppendLine($"<li><strong>Approaching deadline:</strong> {overviewData.ApproachingDeadlineSubActions}</li>");
                sb.AppendLine($"<li style='color: #d32f2f;'><strong>Overdue:</strong> {overviewData.OverdueSubActions}</li>");
                sb.AppendLine($"</ul>");
                sb.AppendLine($"</div>");
            }

            if (overviewData.PendingRegistrations > 0)
            {
                sb.AppendLine($"<div style='background-color: #fff3cd; padding: 15px; margin: 15px 0; border-left: 4px solid #ffc107;'>");
                sb.AppendLine($"<h3>⚠️ Pending Account Requests</h3>");
                sb.AppendLine($"<p>There are {overviewData.PendingRegistrations} pending user registration requests requiring approval.</p>");
                sb.AppendLine($"</div>");
            }

            sb.AppendLine($"<p>Please log in to the HSE admin panel for detailed information and management.</p>");
            sb.AppendLine($"<p>Best regards,<br>HSE Management System</p>");

            return sb.ToString();
        }

        private string GenerateInstantReportEmailBody(ApplicationUser hseUser, Report report, ApplicationUser? reporter = null)
        {
            var sb = new StringBuilder();
            sb.AppendLine($"<h2>🚨 Instant Alert: New Report Submitted</h2>");
            sb.AppendLine($"<p>Dear {hseUser.FullName},</p>");
            sb.AppendLine($"<p>A new report has been submitted in your zone and requires immediate attention:</p>");

            sb.AppendLine($"<div style='background-color: #f8f9fa; padding: 15px; border-left: 4px solid #dc3545; margin: 15px 0;'>");
            sb.AppendLine($"<h3>{report.Title}</h3>");
            sb.AppendLine($"<p><strong>Type:</strong> {report.Type}</p>");
            sb.AppendLine($"<p><strong>Zone:</strong> {report.ZoneRef?.Name ?? report.Zone}</p>");
            
            // Display reporter information
            if (reporter != null)
            {
                sb.AppendLine($"<p><strong>Reporter:</strong> {reporter.FullName} (ID: {report.ReporterCompanyId})</p>");
            }
            else
            {
                sb.AppendLine($"<p><strong>Reporter ID:</strong> {report.ReporterCompanyId}</p>");
            }
            
            sb.AppendLine($"<p><strong>Submitted:</strong> {report.CreatedAt:MM/dd/yyyy HH:mm}</p>");
            
            if (report.InjuredPersonsCount > 0)
            {
                sb.AppendLine($"<p style='color: #dc3545;'><strong>⚠️ Injured Persons:</strong> {report.InjuredPersonsCount}</p>");
            }
            
            sb.AppendLine($"<p><strong>Tracking Number:</strong> {report.TrackingNumber}</p>");
            sb.AppendLine($"</div>");

            sb.AppendLine($"<div style='text-align: center; margin: 30px 0;'>");
            sb.AppendLine($"<a href='http://192.168.0.245:4200/reports/{report.Id}' style='background-color: #dc3545; color: white; padding: 12px 24px; text-decoration: none; border-radius: 5px; display: inline-block;'>Review Report</a>");
            sb.AppendLine($"</div>");

            sb.AppendLine($"<p>Please log in to the HSE system immediately to review this report and take appropriate action.</p>");
            sb.AppendLine($"<p>This is an automated instant notification.</p>");
            sb.AppendLine($"<p><strong>HSE Management System</strong></p>");

            return sb.ToString();
        }

        private async Task<HSEUpdateData> GenerateHSEUpdateDataAsync(string hseUserId, DateTime cutoffTime)
        {
            var data = new HSEUpdateData
            {
                IntervalHours = (int)(DateTime.UtcNow - cutoffTime).TotalMinutes / 60 // Convert to hours for display
            };

            // Get HSE user's directly assigned reports ONLY (no zone-based)
            var assignedReports = await _context.Reports
                .Where(r => r.AssignedHSEId == hseUserId)
                .Select(r => r.Id)
                .ToListAsync();

            // Get user info for logging
            var userInfo = await _context.Users.FirstOrDefaultAsync(u => u.Id == hseUserId);
            var userName = userInfo != null ? $"{userInfo.FirstName} {userInfo.LastName} ({userInfo.Email})" : hseUserId;
            
            _logger.LogInformation($"🔍 HSE User: {userName}");
            _logger.LogInformation($"   - User ID: {hseUserId}");
            _logger.LogInformation($"   - Directly assigned reports: {assignedReports.Count} [{string.Join(", ", assignedReports)}]");

            if (assignedReports.Any())
            {
                var today = DateTime.UtcNow.Date;
                var threeDaysFromNow = DateTime.UtcNow.AddDays(3);

                // Reports statistics
                data.TotalReports = assignedReports.Count;
                data.NewReports = await _context.Reports
                    .Where(r => assignedReports.Contains(r.Id) && r.CreatedAt >= cutoffTime)
                    .CountAsync();
                data.NotStartedReports = await _context.Reports
                    .Where(r => assignedReports.Contains(r.Id) && r.Status == "Unopened")
                    .CountAsync();

                // Corrective Actions statistics - Actions authored/created by HSE user
                var allCorrectiveActions = await _context.CorrectiveActions
                    .Where(ca => ca.CreatedByHSEId == hseUserId)
                    .ToListAsync();

                _logger.LogInformation($"🎯 CorrectiveActions authored by {userName}:");
                _logger.LogInformation($"   - Total CorrectiveActions: {allCorrectiveActions.Count}");
                _logger.LogInformation($"   - CorrectiveAction IDs: [{string.Join(", ", allCorrectiveActions.Select(ca => ca.Id))}]");
                _logger.LogInformation($"   - CorrectiveAction details: [{string.Join(", ", allCorrectiveActions.Select(ca => $"ID{ca.Id}:'{ca.Title}':{(ca.IsCompleted ? "Completed" : "Not Started")}"))}]");

                data.TotalActions = allCorrectiveActions.Count;
                data.NewActions = allCorrectiveActions.Count(ca => ca.CreatedAt >= cutoffTime);
                data.NotStartedActions = allCorrectiveActions.Count(ca => ca.Status == "Not Started");
                data.OverdueActions = allCorrectiveActions.Count(ca => ca.DueDate.Date < today && ca.Status != "Completed" && ca.Status != "Aborted");

                // Sub-actions statistics - SubActions from CorrectiveActions authored by this HSE user
                var allSubActions = await _context.SubActions
                    .Where(sa => 
                        // Sub-actions belonging to CorrectiveActions authored by this HSE user
                        sa.CorrectiveActionId.HasValue && 
                        allCorrectiveActions.Select(ca => ca.Id).Contains(sa.CorrectiveActionId.Value)
                    )
                    .ToListAsync();

                _logger.LogInformation($"🔧 SubActions for HSE User {hseUserId}:");
                _logger.LogInformation($"   - Total SubActions found: {allSubActions.Count}");
                _logger.LogInformation($"   - SubAction IDs: [{string.Join(", ", allSubActions.Select(sa => sa.Id))}]");
                _logger.LogInformation($"   - SubAction statuses: [{string.Join(", ", allSubActions.Select(sa => $"ID{sa.Id}:{sa.Status}"))}]");

                data.TotalSubActions = allSubActions.Count;
                data.NewSubActions = allSubActions.Count(sa => sa.CreatedAt >= cutoffTime);
                data.NotStartedSubActions = allSubActions.Count(sa => sa.Status == "Not Started");
                data.InProgressSubActions = allSubActions.Count(sa => sa.Status == "In Progress");
                data.CompletedSubActions = allSubActions.Count(sa => sa.Status == "Completed");
                data.OverdueSubActions = allSubActions.Count(sa => sa.DueDate.HasValue && sa.DueDate.Value.Date < today && sa.Status != "Completed");
                data.ApproachingDeadlineSubActions = allSubActions.Count(sa => sa.DueDate.HasValue && sa.DueDate.Value <= threeDaysFromNow && sa.DueDate.Value.Date >= today && sa.Status != "Completed");

                // Count new comments in reports containing actions authored by this HSE user
                var reportsWithMyActions = allCorrectiveActions.Where(ca => ca.ReportId.HasValue).Select(ca => ca.ReportId.Value).Distinct().ToList();
                var combinedReports = assignedReports.Union(reportsWithMyActions).ToList();
                data.NewComments = await _context.Comments
                    .Where(c => combinedReports.Contains(c.ReportId) && c.CreatedAt >= cutoffTime)
                    .CountAsync();

                // Count approaching deadlines (corrective actions only)
                data.ApproachingDeadlines = allCorrectiveActions.Count(ca => ca.DueDate <= threeDaysFromNow && ca.DueDate.Date >= today && !ca.IsCompleted);

                // Simplified update detection - focus on meaningful changes
                data.HasUpdates = data.NewReports > 0 ||
                                data.NewActions > 0 ||
                                data.NewSubActions > 0 ||
                                data.NewComments > 0 ||
                                data.ApproachingDeadlines > 0 ||
                                data.OverdueActions > 0 ||
                                data.OverdueSubActions > 0;

                _logger.LogInformation($"📈 Summary for HSE User {hseUserId}:");
                _logger.LogInformation($"   - New Reports: {data.NewReports}, New Actions: {data.NewActions}, New SubActions: {data.NewSubActions}");
                _logger.LogInformation($"   - New Comments: {data.NewComments}, Approaching Deadlines: {data.ApproachingDeadlines}");
                _logger.LogInformation($"   - Overdue Actions: {data.OverdueActions}, Overdue SubActions: {data.OverdueSubActions}");
                _logger.LogInformation($"   - Has Updates: {data.HasUpdates}");
            }

            return data;
        }

        private async Task<AdminOverviewData> GenerateAdminOverviewDataAsync(DateTime cutoffTime)
        {
            var data = new AdminOverviewData
            {
                IntervalHours = (int)(DateTime.UtcNow - cutoffTime).TotalMinutes / 60 // Convert to hours for display
            };

            // Total reports
            data.TotalReports = await _context.Reports.CountAsync();

            // New reports since cutoff
            data.NewReports = await _context.Reports
                .CountAsync(r => r.CreatedAt >= cutoffTime);

            // Opened reports
            data.OpenedReports = await _context.Reports
                .CountAsync(r => r.Status == "Opened");

            // Closed reports since cutoff
            data.CompletedReports = await _context.Reports
                .CountAsync(r => r.Status == "Closed" && r.UpdatedAt >= cutoffTime);

            // Get all CORRECTIVE ACTIONS (not the useless Actions table)
            var allCorrectiveActions = await _context.CorrectiveActions.ToListAsync();
            var today = DateTime.UtcNow.Date;

            _logger.LogInformation($"🔍 DEBUG Admin Overview - All CorrectiveActions:");
            _logger.LogInformation($"   - Total CorrectiveActions in DB: {allCorrectiveActions.Count}");
            _logger.LogInformation($"   - CorrectiveAction IDs: [{string.Join(", ", allCorrectiveActions.Select(ca => ca.Id))}]");
            _logger.LogInformation($"   - CorrectiveAction statuses: [{string.Join(", ", allCorrectiveActions.Select(ca => $"ID{ca.Id}:{(ca.IsCompleted ? "Completed" : "Not Started")}"))}]");
            _logger.LogInformation($"   - Cutoff time: {cutoffTime}");

            // Corrective Actions statistics (using the REAL actions, not fake Actions table)
            data.TotalActions = allCorrectiveActions.Count;
            data.NewActions = allCorrectiveActions.Count(ca => ca.CreatedAt >= cutoffTime);
            data.CompletedActions = allCorrectiveActions.Count(ca => ca.Status == "Completed" && ca.UpdatedAt.HasValue && ca.UpdatedAt >= cutoffTime);
            data.NotStartedActions = allCorrectiveActions.Count(ca => ca.Status == "Not Started");
            
            // Overdue corrective actions - actions with due date in the past that are not completed/aborted
            var overdueActions = allCorrectiveActions.Where(ca => ca.DueDate.Date < today && ca.Status != "Completed" && ca.Status != "Aborted").ToList();
            data.OverdueItems = overdueActions.Count;

            _logger.LogInformation($"📊 DEBUG Admin CorrectiveActions Results:");
            _logger.LogInformation($"   - Total CorrectiveActions: {data.TotalActions}");
            _logger.LogInformation($"   - New CorrectiveActions (since {cutoffTime}): {data.NewActions}");
            _logger.LogInformation($"   - Completed CorrectiveActions: {data.CompletedActions}");
            _logger.LogInformation($"   - Not Started CorrectiveActions: {data.NotStartedActions}");
            _logger.LogInformation($"   - Overdue CorrectiveActions: {data.OverdueItems} (IDs: [{string.Join(", ", overdueActions.Select(ca => ca.Id))}])");

            // Pending registrations
            data.PendingRegistrations = await _context.RegistrationRequests
                .CountAsync(r => r.Status == "Pending");

            // Sub-actions statistics
            var allSubActions = await _context.SubActions.ToListAsync();
            var threeDaysFromNow = DateTime.UtcNow.AddDays(3);

            data.TotalSubActions = allSubActions.Count;
            data.NewSubActions = allSubActions.Count(sa => sa.CreatedAt >= cutoffTime);
            data.NotStartedSubActions = allSubActions.Count(sa => sa.Status == "Not Started");
            data.InProgressSubActions = allSubActions.Count(sa => sa.Status == "In Progress");
            data.CompletedSubActions = allSubActions.Count(sa => sa.Status == "Completed");
            data.OverdueSubActions = allSubActions.Count(sa => sa.DueDate.HasValue && sa.DueDate.Value.Date < today && sa.Status != "Completed");
            data.ApproachingDeadlineSubActions = allSubActions.Count(sa => sa.DueDate.HasValue && sa.DueDate.Value <= threeDaysFromNow && sa.DueDate.Value.Date >= today && sa.Status != "Completed");

            return data;
        }

        private async Task<List<ApplicationUser>> GetAdminUsersForEmailsAsync(EmailConfiguration config)
        {
            var adminUsers = new List<ApplicationUser>();

            _logger.LogInformation("👥 SuperAdminUserIds from config: '{SuperAdminIds}'", config.SuperAdminUserIds ?? "(null)");

            // Get super admin users if specified
            if (!string.IsNullOrEmpty(config.SuperAdminUserIds))
            {
                var superAdminIds = config.SuperAdminUserIds.Split(',', StringSplitOptions.RemoveEmptyEntries);
                _logger.LogInformation("👥 Parsed SuperAdmin CompanyIDs: [{SuperAdminIds}]", string.Join(", ", superAdminIds));
                
                foreach (var companyId in superAdminIds)
                {
                    var trimmedId = companyId.Trim();
                    _logger.LogInformation("👤 Looking for admin user with CompanyID: '{CompanyId}'", trimmedId);
                    
                    // Search by CompanyId instead of Id
                    var user = await _context.Users
                        .FirstOrDefaultAsync(u => u.CompanyId == trimmedId && u.IsActive && !string.IsNullOrEmpty(u.Email));
                    
                    if (user != null)
                    {
                        // Verify user has Admin role
                        var userRoles = await _userManager.GetRolesAsync(user);
                        if (userRoles.Contains("Admin"))
                        {
                            _logger.LogInformation("✅ Found admin user: {Email} (CompanyID: {CompanyId})", user.Email, user.CompanyId);
                            adminUsers.Add(user);
                        }
                        else
                        {
                            _logger.LogWarning("❌ User with CompanyID {CompanyId} found but is not an Admin (Role: {Role})", trimmedId, string.Join(", ", userRoles));
                        }
                    }
                    else
                    {
                        _logger.LogWarning("❌ No active admin user found with CompanyID: '{CompanyId}'", trimmedId);
                    }
                }
                
                _logger.LogInformation("🔍 Total admin users found from selection: {Count}", adminUsers.Count);
            }

            // If no super admin users specified or found, get all admin users
            if (!adminUsers.Any())
            {
                if (!string.IsNullOrEmpty(config.SuperAdminUserIds))
                {
                    _logger.LogWarning("⚠️ No valid super admin users found from selection, falling back to ALL admin users");
                }
                else
                {
                    _logger.LogInformation("ℹ️ No super admin users specified, sending to ALL admin users");
                }
                
                var allAdminUsers = await _userManager.GetUsersInRoleAsync("Admin");
                adminUsers = allAdminUsers.Where(u => u.IsActive && !string.IsNullOrEmpty(u.Email)).ToList();
                _logger.LogInformation("📧 Will send to {Count} admin users: [{Emails}]", 
                    adminUsers.Count, string.Join(", ", adminUsers.Select(u => u.Email)));
            }
            else
            {
                _logger.LogInformation("📧 Will send to {Count} selected admin users: [{Emails}]", 
                    adminUsers.Count, string.Join(", ", adminUsers.Select(u => u.Email)));
            }

            return adminUsers;
        }

        private async Task<List<ApplicationUser>> GetHSEUsersForZoneAsync(string zoneName)
        {
            var currentTime = DateTime.UtcNow;
            
            // Get the zone ID from the zone name
            var zone = await _context.Zones.FirstOrDefaultAsync(z => z.Name == zoneName);
            if (zone == null)
            {
                _logger.LogWarning("Zone '{ZoneName}' not found for email notification", zoneName);
                return new List<ApplicationUser>();
            }

            // Get original HSE users assigned to this zone
            var originalHSEUsers = await _context.Users
                .Include(u => u.ResponsibleZones)
                .ThenInclude(rz => rz.Zone)
                .Where(u => u.IsActive && 
                           !string.IsNullOrEmpty(u.Email) &&
                           u.ResponsibleZones.Any(rz => rz.Zone.Name == zoneName && rz.IsActive))
                .ToListAsync();

            // Get delegated HSE users for this zone (active delegations only)
            var delegatedHSEUsers = await _context.HSEZoneDelegations
                .Include(hzd => hzd.ToHSEUser)
                .Include(hzd => hzd.FromHSEUser)
                .Where(hzd => hzd.ZoneId == zone.Id && 
                             hzd.IsActive &&
                             hzd.StartDate <= currentTime && 
                             hzd.EndDate >= currentTime)
                .Select(hzd => hzd.ToHSEUser)
                .Where(u => u.IsActive && !string.IsNullOrEmpty(u.Email))
                .ToListAsync();

            // Combine both lists and remove duplicates
            var allHSEUsers = originalHSEUsers.Union(delegatedHSEUsers).ToList();

            _logger.LogInformation("Email notification for zone '{ZoneName}': {OriginalCount} original HSE users + {DelegatedCount} delegated HSE users = {TotalCount} total", 
                zoneName, originalHSEUsers.Count, delegatedHSEUsers.Count, allHSEUsers.Count);

            return allHSEUsers;
        }

        private async Task LogEmailAsync(string recipientEmail, string? recipientUserId, string subject, string emailType, string status, string? errorMessage = null)
        {
            var emailLog = new EmailLog
            {
                RecipientEmail = recipientEmail,
                RecipientUserId = recipientUserId,
                Subject = subject,
                EmailType = emailType,
                Status = status,
                ErrorMessage = errorMessage,
                CreatedAt = DateTime.UtcNow,
                SentAt = status == "Sent" ? DateTime.UtcNow : null
            };

            _context.EmailLogs.Add(emailLog);
            await _context.SaveChangesAsync();
        }
    }

    // Helper classes for email data
    public class HSEUpdateData
    {
        public int IntervalHours { get; set; }
        public int ActionsStatusChanged { get; set; }
        public int SubActionsStatusChanged { get; set; }
        public int NewComments { get; set; }
        public int ApproachingDeadlines { get; set; }
        public List<string> RecentActivities { get; set; } = new List<string>();
        public bool HasUpdates { get; set; }
        
        // New comprehensive statistics
        public int TotalReports { get; set; }
        public int NewReports { get; set; }
        public int NotStartedReports { get; set; }
        public int TotalActions { get; set; }
        public int NewActions { get; set; }
        public int NotStartedActions { get; set; }
        public int OverdueActions { get; set; }
        public int TotalSubActions { get; set; }
        public int NewSubActions { get; set; }
        public int NotStartedSubActions { get; set; }
        public int InProgressSubActions { get; set; }
        public int CompletedSubActions { get; set; }
        public int OverdueSubActions { get; set; }
        public int ApproachingDeadlineSubActions { get; set; }
    }

    public class AdminOverviewData
    {
        public int IntervalHours { get; set; }
        public int TotalReports { get; set; }
        public int NewReports { get; set; }
        public int OpenedReports { get; set; }
        public int CompletedReports { get; set; }
        public int TotalActions { get; set; }
        public int NewActions { get; set; }
        public int CompletedActions { get; set; }
        public int NotStartedActions { get; set; }
        public int OverdueItems { get; set; }
        public int PendingRegistrations { get; set; }
        
        // Sub-actions statistics
        public int TotalSubActions { get; set; }
        public int NewSubActions { get; set; }
        public int NotStartedSubActions { get; set; }
        public int InProgressSubActions { get; set; }
        public int CompletedSubActions { get; set; }
        public int OverdueSubActions { get; set; }
        public int ApproachingDeadlineSubActions { get; set; }
    }
}