using HSEBackend.Data;
using HSEBackend.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using System.Text;

namespace HSEBackend.Services
{
    public interface INotificationService
    {
        // HSE Notifications
        Task NotifyHSEOnNewReportSubmissionAsync(int reportId, string submittedByUserId);
        Task NotifyHSEOnReportAssignmentAsync(int reportId, string assignedHSEUserId, string assignedByUserId);
        Task NotifyHSEOnNewCommentAsync(int reportId, string commentAuthorUserId, string hseDUserId);
        Task NotifyHSEOnActionStatusUpdateAsync(int actionId, string updatedByUserId, string oldStatus, string newStatus);
        Task NotifyHSEOnSubActionStatusUpdateAsync(int subActionId, string updatedByUserId, string oldStatus, string newStatus);
        Task NotifyHSEOnActionAddedAsync(int actionId, string addedByUserId, string hseUserId);
        Task NotifyHSEOnSubActionAddedAsync(int subActionId, string addedByUserId, string hseUserId);
        Task NotifyHSEOnActionAbortedAsync(int actionId, string abortedByUserId, string hseUserId);
        Task NotifyHSEOnCorrectiveActionAddedAsync(int correctiveActionId, string addedByUserId, string hseUserId);
        Task NotifyHSEOnCorrectiveActionStatusUpdateAsync(int correctiveActionId, string updatedByUserId, string oldStatus, string newStatus);
        Task NotifyHSEOnNewRegistrationRequestAsync(int requestId, string fullName, string companyId);

        // Admin Notifications
        Task NotifyAdminOnDailyUpdatesAsync();
        Task NotifyAdminOnNewRegistrationRequestAsync(int requestId);
        Task NotifyAdminOnNewReportSubmissionAsync(int reportId, string submittedByUserId);
        Task NotifyAdminOnNewActionCreatedAsync(int actionId, string createdByUserId);
        Task NotifyAdminOnNewCorrectiveActionCreatedAsync(int correctiveActionId, string createdByUserId);
        Task NotifyAdminOnOverdueItemsAsync();
        Task NotifyAdminOnHSEActionCancelledAsync(int actionId, string cancelledByHSEUserId);

        // Missing Features Implementation
        Task NotifyActionAuthorOnSubActionUpdateAsync(int subActionId, string updatedByUserId);
        Task NotifyHSEOnAdminActionCancelledAsync(int actionId, string cancelledByAdminId);
        Task NotifyHSEOnAdminSubActionCancelledAsync(int subActionId, string cancelledByAdminId);
        Task NotifyHSEOnCorrectiveActionAbortedAsync(int correctiveActionId, string abortedByAdminId, string reason);
        Task NotifyHSEOnAdminActionAbortedAsync(int actionId, string abortedByAdminId);
        Task NotifyHSEOnAdminActionCreatedAsync(int actionId, string createdByAdminId, string hseUserId);
        Task NotifyHSEOnOverdueCorrectiveActionsAsync();
        Task NotifyHSEOnOverdueSubActionsAsync();
        
        // Zone Delegation Notifications
        Task NotifyOnZoneDelegationCreatedAsync(int delegationId);
        Task NotifyOnZoneDelegationEndedAsync(int delegationId);
        
        // General Notifications
        Task NotifyOnDeadlineApproachingAsync();
        Task<List<Notification>> GetUserNotificationsAsync(string userId, int page = 1, int pageSize = 20);
        Task MarkNotificationAsReadAsync(int notificationId, string userId);
        Task MarkAllNotificationsAsReadAsync(string userId);
        Task<int> GetUnreadNotificationCountAsync(string userId);
    }

    public class NotificationService : INotificationService
    {
        private readonly AppDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger<NotificationService> _logger;
        private readonly IEmailService _emailService;
        private readonly IConfiguration _configuration;

        public NotificationService(
            AppDbContext context,
            UserManager<ApplicationUser> userManager,
            ILogger<NotificationService> logger,
            IEmailService emailService,
            IConfiguration configuration)
        {
            _context = context;
            _userManager = userManager;
            _logger = logger;
            _emailService = emailService;
            _configuration = configuration;
        }

        public async Task NotifyHSEOnNewReportSubmissionAsync(int reportId, string submittedByUserId)
        {
            try
            {
                var report = await _context.Reports
                    .Include(r => r.ZoneRef)
                    .FirstOrDefaultAsync(r => r.Id == reportId);

                if (report == null) return;

                // Find HSE users responsible for this zone
                var hseUsers = await GetHSEUsersForZoneAsync(report.ZoneRef?.Name ?? report.Zone);

                foreach (var hseUser in hseUsers)
                {
                    var notification = new Notification
                    {
                        Title = "New Report Submitted",
                        Message = $"A new {report.Type} report '{report.Title}' has been submitted in {report.ZoneRef?.Name ?? report.Zone} by Reporter ID: {report.ReporterCompanyId}.",
                        Type = "ReportSubmitted",
                        UserId = hseUser.Id,
                        TriggeredByUserId = submittedByUserId,
                        RelatedReportId = reportId,
                        CreatedAt = DateTime.UtcNow
                    };

                    _context.Notifications.Add(notification);
                }

                await _context.SaveChangesAsync();
                _logger.LogInformation("Created {Count} notifications for new report {ReportId}", hseUsers.Count, reportId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating notifications for new report {ReportId}", reportId);
            }
        }

        public async Task NotifyHSEOnReportAssignmentAsync(int reportId, string assignedHSEUserId, string assignedByUserId)
        {
            try
            {
                var report = await _context.Reports
                    .FirstOrDefaultAsync(r => r.Id == reportId);

                if (report == null) return;

                var notification = new Notification
                {
                    Title = "Report Assigned to You",
                    Message = $"Report '{report.Title}' has been assigned to you by an administrator.",
                    Type = "ReportAssigned",
                    UserId = assignedHSEUserId,
                    TriggeredByUserId = assignedByUserId,
                    RelatedReportId = reportId,
                    CreatedAt = DateTime.UtcNow
                };

                _context.Notifications.Add(notification);
                await _context.SaveChangesAsync();

                _logger.LogInformation($"Created assignment notification for HSE user {assignedHSEUserId} for report {reportId}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating assignment notification for report {ReportId}", reportId);
            }
        }

        public async Task NotifyHSEOnNewCommentAsync(int reportId, string commentAuthorUserId, string hseUserId)
        {
            try
            {
                var report = await _context.Reports
                    .FirstOrDefaultAsync(r => r.Id == reportId);

                var commentAuthor = await _userManager.FindByIdAsync(commentAuthorUserId);

                if (report == null || commentAuthor == null) return;

                var notification = new Notification
                {
                    Title = "New Comment on Assigned Report",
                    Message = $"{commentAuthor.FullName} added a comment to report '{report.Title}' assigned to you.",
                    Type = "CommentAdded",
                    UserId = hseUserId,
                    TriggeredByUserId = commentAuthorUserId,
                    RelatedReportId = reportId,
                    CreatedAt = DateTime.UtcNow
                };

                _context.Notifications.Add(notification);
                await _context.SaveChangesAsync();

                _logger.LogInformation($"Created comment notification for HSE user {hseUserId} on report {reportId}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating comment notification for report {ReportId}", reportId);
            }
        }

        public async Task NotifyHSEOnActionStatusUpdateAsync(int actionId, string updatedByUserId, string oldStatus, string newStatus)
        {
            try
            {
                var action = await _context.Actions
                    .Include(a => a.Report)
                    .ThenInclude(r => r.AssignedHSE)
                    .FirstOrDefaultAsync(a => a.Id == actionId);

                var updatedBy = await _userManager.FindByIdAsync(updatedByUserId);

                if (action?.Report?.AssignedHSE == null || updatedBy == null) return;

                var notification = new Notification
                {
                    Title = "Action Status Updated",
                    Message = $"{updatedBy.FullName} updated action '{action.Title}' status from '{oldStatus}' to '{newStatus}' in report '{action.Report.Title}'.",
                    Type = "ActionStatusChanged",
                    UserId = action.Report.AssignedHSE.Id,
                    TriggeredByUserId = updatedByUserId,
                    RelatedActionId = actionId,
                    RelatedReportId = action.ReportId,
                    CreatedAt = DateTime.UtcNow
                };

                _context.Notifications.Add(notification);
                await _context.SaveChangesAsync();

                _logger.LogInformation($"Created action status update notification for HSE user {action.Report.AssignedHSE.Id}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating action status notification for action {ActionId}", actionId);
            }
        }

        public async Task NotifyHSEOnSubActionStatusUpdateAsync(int subActionId, string updatedByUserId, string oldStatus, string newStatus)
        {
            try
            {
                var subAction = await _context.SubActions
                    .Include(sa => sa.CorrectiveAction)
                    .ThenInclude(ca => ca.Report)
                    .ThenInclude(r => r.AssignedHSE)
                    .FirstOrDefaultAsync(sa => sa.Id == subActionId);

                var updatedBy = await _userManager.FindByIdAsync(updatedByUserId);

                if (subAction?.CorrectiveAction?.Report?.AssignedHSE == null || updatedBy == null) return;

                var notification = new Notification
                {
                    Title = "Sub-Action Status Updated",
                    Message = $"{updatedBy.FullName} updated sub-action '{subAction.Title}' status from '{oldStatus}' to '{newStatus}' in corrective action '{subAction.CorrectiveAction.Title}'.",
                    Type = "SubActionStatusChanged",
                    UserId = subAction.CorrectiveAction.Report.AssignedHSE.Id,
                    TriggeredByUserId = updatedByUserId,
                    RelatedCorrectiveActionId = subAction.CorrectiveActionId,
                    RelatedReportId = subAction.CorrectiveAction.ReportId,
                    CreatedAt = DateTime.UtcNow
                };

                _context.Notifications.Add(notification);
                await _context.SaveChangesAsync();

                _logger.LogInformation($"Created sub-action status update notification for HSE user {subAction.CorrectiveAction.Report.AssignedHSE.Id}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating sub-action status notification for sub-action {SubActionId}", subActionId);
            }
        }

        public async Task NotifyHSEOnActionAddedAsync(int actionId, string addedByUserId, string hseUserId)
        {
            try
            {
                var action = await _context.Actions
                    .Include(a => a.Report)
                    .FirstOrDefaultAsync(a => a.Id == actionId);

                var addedBy = await _userManager.FindByIdAsync(addedByUserId);

                if (action == null || addedBy == null) return;
                
                // Don't notify if the HSE user is notifying themselves
                if (addedByUserId == hseUserId) return;

                var notification = new Notification
                {
                    Title = "New Action Added to Your Report",
                    Message = $"{addedBy.FullName} added a new action '{action.Title}' to report '{action.Report?.Title}' assigned to you.",
                    Type = "ActionAdded",
                    UserId = hseUserId,
                    TriggeredByUserId = addedByUserId,
                    RelatedActionId = actionId,
                    RelatedReportId = action.ReportId,
                    CreatedAt = DateTime.UtcNow
                };

                _context.Notifications.Add(notification);
                await _context.SaveChangesAsync();

                _logger.LogInformation($"Created action added notification for HSE user {hseUserId}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating action added notification for action {ActionId}", actionId);
            }
        }

        public async Task NotifyHSEOnSubActionAddedAsync(int subActionId, string addedByUserId, string hseUserId)
        {
            try
            {
                var subAction = await _context.SubActions
                    .Include(sa => sa.CorrectiveAction)
                    .FirstOrDefaultAsync(sa => sa.Id == subActionId);

                var addedBy = await _userManager.FindByIdAsync(addedByUserId);

                if (subAction == null || addedBy == null) return;
                
                // Don't notify if the HSE user is notifying themselves
                if (addedByUserId == hseUserId) return;

                var notification = new Notification
                {
                    Title = "New Sub-Action Added",
                    Message = $"{addedBy.FullName} added a new sub-action '{subAction.Title}' to corrective action '{subAction.CorrectiveAction?.Title}'.",
                    Type = "SubActionAdded",
                    UserId = hseUserId,
                    TriggeredByUserId = addedByUserId,
                    RelatedCorrectiveActionId = subAction.CorrectiveActionId,
                    CreatedAt = DateTime.UtcNow
                };

                _context.Notifications.Add(notification);
                await _context.SaveChangesAsync();

                _logger.LogInformation($"Created sub-action added notification for HSE user {hseUserId}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating sub-action added notification for sub-action {SubActionId}", subActionId);
            }
        }

        public async Task NotifyHSEOnActionAbortedAsync(int actionId, string abortedByUserId, string hseUserId)
        {
            try
            {
                var action = await _context.Actions
                    .Include(a => a.Report)
                    .FirstOrDefaultAsync(a => a.Id == actionId);

                var abortedBy = await _userManager.FindByIdAsync(abortedByUserId);

                if (action == null || abortedBy == null) return;

                var notification = new Notification
                {
                    Title = "Action Aborted",
                    Message = $"{abortedBy.FullName} aborted action '{action.Title}' in report '{action.Report?.Title}'.",
                    Type = "ActionAborted",
                    UserId = hseUserId,
                    TriggeredByUserId = abortedByUserId,
                    RelatedActionId = actionId,
                    RelatedReportId = action.ReportId,
                    CreatedAt = DateTime.UtcNow
                };

                _context.Notifications.Add(notification);
                await _context.SaveChangesAsync();

                _logger.LogInformation($"Created action aborted notification for HSE user {hseUserId}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating action aborted notification for action {ActionId}", actionId);
            }
        }

        public async Task NotifyHSEOnCorrectiveActionAddedAsync(int correctiveActionId, string addedByUserId, string hseUserId)
        {
            try
            {
                var correctiveAction = await _context.CorrectiveActions
                    .Include(ca => ca.Report)
                    .FirstOrDefaultAsync(ca => ca.Id == correctiveActionId);

                var addedBy = await _userManager.FindByIdAsync(addedByUserId);

                if (correctiveAction == null || addedBy == null) return;
                
                // Don't notify if the HSE user is notifying themselves
                if (addedByUserId == hseUserId) return;

                var notification = new Notification
                {
                    Title = "Corrective Action Added",
                    Message = $"{addedBy.FullName} added corrective action '{correctiveAction.Title}' to report '{correctiveAction.Report?.Title}'.",
                    Type = "CorrectiveActionAdded",
                    UserId = hseUserId,
                    TriggeredByUserId = addedByUserId,
                    RelatedCorrectiveActionId = correctiveActionId,
                    RelatedReportId = correctiveAction.ReportId,
                    CreatedAt = DateTime.UtcNow
                };

                _context.Notifications.Add(notification);
                await _context.SaveChangesAsync();

                _logger.LogInformation($"Created corrective action added notification for HSE user {hseUserId}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating corrective action added notification for corrective action {CorrectiveActionId}", correctiveActionId);
            }
        }

        public async Task NotifyHSEOnCorrectiveActionStatusUpdateAsync(int correctiveActionId, string updatedByUserId, string oldStatus, string newStatus)
        {
            try
            {
                var correctiveAction = await _context.CorrectiveActions
                    .Include(ca => ca.Report)
                    .FirstOrDefaultAsync(ca => ca.Id == correctiveActionId);

                var updatedBy = await _userManager.FindByIdAsync(updatedByUserId);

                if (correctiveAction == null || updatedBy == null) return;

                var notification = new Notification
                {
                    Title = "Corrective Action Status Updated",
                    Message = $"{updatedBy.FullName} updated corrective action '{correctiveAction.Title}' status from '{oldStatus}' to '{newStatus}' in report '{correctiveAction.Report?.Title}'.",
                    Type = "CorrectiveActionStatusUpdate",
                    UserId = correctiveAction.Report?.AssignedHSEId,
                    TriggeredByUserId = updatedByUserId,
                    RelatedCorrectiveActionId = correctiveActionId,
                    RelatedReportId = correctiveAction.ReportId,
                    CreatedAt = DateTime.UtcNow
                };

                _context.Notifications.Add(notification);
                await _context.SaveChangesAsync();

                _logger.LogInformation($"Created corrective action status update notification for corrective action {correctiveActionId}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating corrective action status update notification for corrective action {CorrectiveActionId}", correctiveActionId);
            }
        }

        public async Task NotifyHSEOnNewRegistrationRequestAsync(int requestId, string fullName, string companyId)
        {
            try
            {
                // Get all HSE users to notify them about the new registration request
                var hseUsers = await GetHSEUsersAsync();

                foreach (var hseUser in hseUsers)
                {
                    var notification = new Notification
                    {
                        Title = "New Registration Request",
                        Message = $"New registration request submitted by {fullName} (ID: {companyId}). Please review and approve/reject.",
                        Type = "RegistrationRequest",
                        UserId = hseUser.Id,
                        TriggeredByUserId = "system",
                        CreatedAt = DateTime.UtcNow
                    };

                    _context.Notifications.Add(notification);
                }

                await _context.SaveChangesAsync();

                _logger.LogInformation($"Created registration request notifications for {hseUsers.Count} HSE users for request {requestId}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating registration request notifications for request {RequestId}", requestId);
            }
        }

        public async Task NotifyAdminOnDailyUpdatesAsync()
        {
            try
            {
                var yesterday = DateTime.UtcNow.AddDays(-1);
                var adminUsers = await GetAdminUsersAsync();

                // Calculate statistics
                var completedReports = await _context.Reports
                    .CountAsync(r => r.Status == "Closed" && r.UpdatedAt >= yesterday);

                var newActions = await _context.Actions
                    .CountAsync(a => a.CreatedAt >= yesterday);

                var completedActions = await _context.Actions
                    .CountAsync(a => a.Status == "Completed" && a.UpdatedAt >= yesterday);

                foreach (var admin in adminUsers)
                {
                    var notification = new Notification
                    {
                        Title = "Daily HSE System Update",
                        Message = $"Last 24h: {completedReports} reports completed, {newActions} new actions created, {completedActions} actions completed.",
                        Type = "DailyUpdate",
                        UserId = admin.Id,
                        CreatedAt = DateTime.UtcNow
                    };

                    _context.Notifications.Add(notification);
                }

                await _context.SaveChangesAsync();
                _logger.LogInformation($"Created daily update notifications for {adminUsers.Count} admin users");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating daily update notifications");
            }
        }

        public async Task NotifyAdminOnNewRegistrationRequestAsync(int requestId)
        {
            try
            {
                var request = await _context.RegistrationRequests
                    .FirstOrDefaultAsync(r => r.Id == requestId);

                if (request == null) return;

                var adminUsers = await GetAdminUsersAsync();

                foreach (var admin in adminUsers)
                {
                    var notification = new Notification
                    {
                        Title = "New Registration Request",
                        Message = $"New user registration request from {request.FullName} ({request.Email}) for {request.Department} department.",
                        Type = "RegistrationRequest",
                        UserId = admin.Id,
                        CreatedAt = DateTime.UtcNow
                    };

                    _context.Notifications.Add(notification);
                }

                await _context.SaveChangesAsync();
                _logger.LogInformation($"Created registration request notifications for {adminUsers.Count} admin users");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating registration request notifications");
            }
        }

        public async Task NotifyAdminOnNewReportSubmissionAsync(int reportId, string submittedByUserId)
        {
            try
            {
                var report = await _context.Reports
                    .FirstOrDefaultAsync(r => r.Id == reportId);

                if (report == null) return;

                var adminUsers = await GetAdminUsersAsync();

                foreach (var admin in adminUsers)
                {
                    var notification = new Notification
                    {
                        Title = "New Report Submitted",
                        Message = $"A new {report.Type} report '{report.Title}' has been submitted by Company ID: {report.ReporterCompanyId} in {report.Zone}.",
                        Type = "ReportSubmitted",
                        UserId = admin.Id,
                        TriggeredByUserId = submittedByUserId,
                        RelatedReportId = reportId,
                        CreatedAt = DateTime.UtcNow
                    };

                    _context.Notifications.Add(notification);
                }

                await _context.SaveChangesAsync();

                _logger.LogInformation($"Created admin report submission notifications for {adminUsers.Count} admin users for report {reportId}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating admin report submission notifications for report {ReportId}", reportId);
            }
        }

        public async Task NotifyAdminOnNewActionCreatedAsync(int actionId, string createdByUserId)
        {
            try
            {
                var action = await _context.Actions
                    .Include(a => a.Report)
                    .Include(a => a.CreatedBy)
                    .FirstOrDefaultAsync(a => a.Id == actionId);

                if (action == null) return;

                var adminUsers = await GetAdminUsersAsync();

                foreach (var admin in adminUsers)
                {
                    // Don't notify if the admin is notifying themselves
                    if (admin.Id == createdByUserId) continue;
                    
                    var notification = new Notification
                    {
                        Title = "New Action Created",
                        Message = $"HSE user {action.CreatedBy?.FullName} created action '{action.Title}' for report '{action.Report?.Title}'.",
                        Type = "ActionCreated",
                        UserId = admin.Id,
                        TriggeredByUserId = createdByUserId,
                        RelatedActionId = actionId,
                        RelatedReportId = action.ReportId,
                        CreatedAt = DateTime.UtcNow
                    };

                    _context.Notifications.Add(notification);
                }

                await _context.SaveChangesAsync();

                _logger.LogInformation($"Created admin action creation notifications for {adminUsers.Count} admin users for action {actionId}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating admin action creation notifications for action {ActionId}", actionId);
            }
        }

        public async Task NotifyAdminOnNewCorrectiveActionCreatedAsync(int correctiveActionId, string createdByUserId)
        {
            try
            {
                var correctiveAction = await _context.CorrectiveActions
                    .Include(ca => ca.Report)
                    .Include(ca => ca.CreatedByHSE)
                    .FirstOrDefaultAsync(ca => ca.Id == correctiveActionId);

                if (correctiveAction == null) return;

                var adminUsers = await GetAdminUsersAsync();

                foreach (var admin in adminUsers)
                {
                    // Don't notify if the admin is notifying themselves
                    if (admin.Id == createdByUserId) continue;
                    
                    var notification = new Notification
                    {
                        Title = "New Corrective Action Created",
                        Message = $"HSE user {correctiveAction.CreatedByHSE?.FullName} created corrective action '{correctiveAction.Title}' for report '{correctiveAction.Report?.Title}'.",
                        Type = "CorrectiveActionCreated",
                        UserId = admin.Id,
                        TriggeredByUserId = createdByUserId,
                        RelatedCorrectiveActionId = correctiveActionId,
                        RelatedReportId = correctiveAction.ReportId,
                        CreatedAt = DateTime.UtcNow
                    };

                    _context.Notifications.Add(notification);
                }

                await _context.SaveChangesAsync();

                _logger.LogInformation($"Created admin corrective action creation notifications for {adminUsers.Count} admin users for corrective action {correctiveActionId}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating admin corrective action creation notifications for corrective action {CorrectiveActionId}", correctiveActionId);
            }
        }

        public async Task NotifyAdminOnOverdueItemsAsync()
        {
            try
            {
                var today = DateTime.UtcNow.Date;
                var adminUsers = await GetAdminUsersAsync();

                var overdueActions = await _context.Actions
                    .Where(a => a.DueDate.HasValue && a.DueDate.Value.Date < today && a.Status != "Completed" && a.Status != "Aborted")
                    .CountAsync();

                var overdueSubActions = await _context.SubActions
                    .Where(sa => sa.DueDate.HasValue && sa.DueDate.Value.Date < today && sa.Status != "Completed" && sa.Status != "Cancelled")
                    .CountAsync();

                if (overdueActions > 0 || overdueSubActions > 0)
                {
                    foreach (var admin in adminUsers)
                    {
                        var notification = new Notification
                        {
                            Title = "Overdue Items Alert",
                            Message = $"System has {overdueActions} overdue actions and {overdueSubActions} overdue sub-actions requiring attention.",
                            Type = "OverdueAlert",
                            UserId = admin.Id,
                            CreatedAt = DateTime.UtcNow
                        };

                        _context.Notifications.Add(notification);
                    }

                    await _context.SaveChangesAsync();
                    _logger.LogInformation($"Created overdue alerts for {adminUsers.Count} admin users");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating overdue notifications");
            }
        }

        public async Task NotifyAdminOnHSEActionCancelledAsync(int actionId, string cancelledByHSEUserId)
        {
            try
            {
                var action = await _context.Actions
                    .Include(a => a.Report)
                    .FirstOrDefaultAsync(a => a.Id == actionId);

                var cancelledBy = await _userManager.FindByIdAsync(cancelledByHSEUserId);
                if (action == null || cancelledBy == null) return;

                var adminUsers = await GetAdminUsersAsync();

                foreach (var admin in adminUsers)
                {
                    var notification = new Notification
                    {
                        Title = "Action Cancelled by HSE",
                        Message = $"HSE user {cancelledBy.FullName} cancelled action '{action.Title}' in report '{action.Report?.Title}'.",
                        Type = "ActionCancelled",
                        UserId = admin.Id,
                        TriggeredByUserId = cancelledByHSEUserId,
                        RelatedActionId = actionId,
                        RelatedReportId = action.ReportId,
                        CreatedAt = DateTime.UtcNow
                    };

                    _context.Notifications.Add(notification);
                }

                await _context.SaveChangesAsync();
                _logger.LogInformation($"Created action cancelled notifications for {adminUsers.Count} admin users");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating action cancelled notifications");
            }
        }

        public async Task NotifyOnDeadlineApproachingAsync()
        {
            try
            {
                var threeDaysFromNow = DateTime.UtcNow.AddDays(3).Date;
                var today = DateTime.UtcNow.Date;

                // Find actions approaching deadline
                var approachingActions = await _context.Actions
                    .Include(a => a.Report)
                    .ThenInclude(r => r.AssignedHSE)
                    .Where(a => a.DueDate.HasValue && 
                               a.DueDate.Value.Date <= threeDaysFromNow && 
                               a.DueDate.Value.Date >= today &&
                               a.Status != "Completed" && 
                               a.Status != "Aborted")
                    .ToListAsync();

                // Find sub-actions approaching deadline
                var approachingSubActions = await _context.SubActions
                    .Include(sa => sa.CorrectiveAction)
                    .ThenInclude(ca => ca.Report)
                    .ThenInclude(r => r.AssignedHSE)
                    .Where(sa => sa.DueDate.HasValue && 
                                sa.DueDate.Value.Date <= threeDaysFromNow && 
                                sa.DueDate.Value.Date >= today &&
                                sa.Status != "Completed" && 
                                sa.Status != "Cancelled")
                    .ToListAsync();

                // Create notifications for approaching action deadlines
                foreach (var action in approachingActions)
                {
                    if (action.Report?.AssignedHSE == null) continue;

                    var daysLeft = (action.DueDate!.Value.Date - today).Days;
                    var notification = new Notification
                    {
                        Title = "Action Deadline Approaching",
                        Message = $"Action '{action.Title}' is due in {daysLeft} day(s) ({action.DueDate.Value:MM/dd/yyyy}).",
                        Type = "DeadlineApproaching",
                        UserId = action.Report.AssignedHSE.Id,
                        RelatedActionId = action.Id,
                        RelatedReportId = action.ReportId,
                        CreatedAt = DateTime.UtcNow
                    };

                    _context.Notifications.Add(notification);
                }

                // Create notifications for approaching sub-action deadlines
                foreach (var subAction in approachingSubActions)
                {
                    if (subAction.CorrectiveAction?.Report?.AssignedHSE == null) continue;

                    var daysLeft = (subAction.DueDate!.Value.Date - today).Days;
                    var notification = new Notification
                    {
                        Title = "Sub-Action Deadline Approaching",
                        Message = $"Sub-action '{subAction.Title}' is due in {daysLeft} day(s) ({subAction.DueDate.Value:MM/dd/yyyy}).",
                        Type = "DeadlineApproaching",
                        UserId = subAction.CorrectiveAction.Report.AssignedHSE.Id,
                        RelatedCorrectiveActionId = subAction.CorrectiveActionId,
                        RelatedReportId = subAction.CorrectiveAction.ReportId,
                        CreatedAt = DateTime.UtcNow
                    };

                    _context.Notifications.Add(notification);
                }

                await _context.SaveChangesAsync();
                _logger.LogInformation($"Created {approachingActions.Count + approachingSubActions.Count} deadline approaching notifications");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating deadline approaching notifications");
            }
        }

        public async Task<List<Notification>> GetUserNotificationsAsync(string userId, int page = 1, int pageSize = 20)
        {
            return await _context.Notifications
                .Include(n => n.TriggeredByUser)
                .Include(n => n.RelatedReport)
                .Include(n => n.RelatedAction)
                .Include(n => n.RelatedCorrectiveAction)
                .Where(n => n.UserId == userId)
                .OrderByDescending(n => n.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
        }

        public async Task MarkNotificationAsReadAsync(int notificationId, string userId)
        {
            var notification = await _context.Notifications
                .FirstOrDefaultAsync(n => n.Id == notificationId && n.UserId == userId);

            if (notification != null && !notification.IsRead)
            {
                notification.IsRead = true;
                notification.ReadAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();
            }
        }

        public async Task MarkAllNotificationsAsReadAsync(string userId)
        {
            var unreadNotifications = await _context.Notifications
                .Where(n => n.UserId == userId && !n.IsRead)
                .ToListAsync();

            foreach (var notification in unreadNotifications)
            {
                notification.IsRead = true;
                notification.ReadAt = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync();
        }

        public async Task<int> GetUnreadNotificationCountAsync(string userId)
        {
            return await _context.Notifications
                .CountAsync(n => n.UserId == userId && !n.IsRead);
        }

        // ===== MISSING FEATURES IMPLEMENTATION =====
        
        /// <summary>
        /// 1. Profile User Updates Assigned Sub-Actions - Notify action/sub-action author
        /// </summary>
        public async Task NotifyActionAuthorOnSubActionUpdateAsync(int subActionId, string updatedByUserId)
        {
            try
            {
                var subAction = await _context.SubActions
                    .Include(sa => sa.Action)
                    .ThenInclude(a => a.CreatedBy)
                    .Include(sa => sa.CorrectiveAction)
                    .ThenInclude(ca => ca.CreatedByHSE)
                    .FirstOrDefaultAsync(sa => sa.Id == subActionId);

                var updatedBy = await _userManager.FindByIdAsync(updatedByUserId);
                if (subAction == null || updatedBy == null) return;

                // Work with both actions and corrective actions, but prefer showing corrective action in messages
                string? authorId = null;
                string? authorName = null;
                string parentType = "corrective action";
                string parentTitle = "";

                if (subAction.CorrectiveAction?.CreatedByHSE != null)
                {
                    // Corrective action case
                    authorId = subAction.CorrectiveAction.CreatedByHSE.Id;
                    authorName = subAction.CorrectiveAction.CreatedByHSE.FullName;
                    parentTitle = subAction.CorrectiveAction.Title;
                    parentType = "corrective action";
                }
                else if (subAction.Action?.CreatedBy != null)
                {
                    // Action case - but show corrective action title if available, otherwise action title
                    authorId = subAction.Action.CreatedBy.Id;
                    authorName = subAction.Action.CreatedBy.FullName;
                    // Try to get corrective action title first, fallback to action title
                    parentTitle = subAction.CorrectiveAction?.Title ?? subAction.Action.Title;
                    parentType = "corrective action";
                }
                else
                {
                    _logger.LogWarning("SubAction {SubActionId} has no valid parent Action or CorrectiveAction author", subActionId);
                    return;
                }

                if (authorId == null || authorId == updatedByUserId) return; // Don't notify self

                // Generate redirect URL based on parent type
                string redirectUrl = "/actions"; // Default to actions page
                if (subAction.Action?.ReportId != null)
                {
                    redirectUrl = $"http://192.168.0.245:4200/reports/{subAction.Action.ReportId}#action-{subAction.ActionId}";
                }
                else if (subAction.CorrectiveAction?.ReportId != null)
                {
                    redirectUrl = $"http://192.168.0.245:4200/reports/{subAction.CorrectiveAction.ReportId}#corrective-action-{subAction.CorrectiveActionId}";
                }

                var notification = new Notification
                {
                    Title = "Sub-Action Updated by Profile User",
                    Message = $"{updatedBy.FullName} updated sub-action '{subAction.Title}' assigned to them in your corrective action '{parentTitle}'.",
                    Type = "SubActionUpdatedByProfile",
                    UserId = authorId,
                    TriggeredByUserId = updatedByUserId,
                    RelatedCorrectiveActionId = subAction.CorrectiveActionId,
                    RelatedActionId = subAction.ActionId,
                    RedirectUrl = redirectUrl,
                    CreatedAt = DateTime.UtcNow
                };

                _context.Notifications.Add(notification);
                await _context.SaveChangesAsync();

                // Send email notification
                var author = await _userManager.FindByIdAsync(authorId);
                if (author != null && !string.IsNullOrEmpty(author.Email))
                {
                    var subject = "Sub-Action Updated by Profile User";
                    var emailBody = GenerateSubActionUpdateEmailBody(author, updatedBy, subAction, "corrective action", parentTitle, redirectUrl);
                    
                    try
                    {
                        await _emailService.SendGenericEmail(author.Email, subject, emailBody);
                        
                        // Mark notification as email sent
                        notification.IsEmailSent = true;
                        notification.EmailSentAt = DateTime.UtcNow;
                        await _context.SaveChangesAsync();
                        
                        _logger.LogInformation($"Sent email notification to {author.Email} for sub-action update by profile user");
                    }
                    catch (Exception emailEx)
                    {
                        _logger.LogError(emailEx, "Failed to send email notification for sub-action update by profile user");
                    }
                }

                _logger.LogInformation($"Created sub-action update notification for author {authorId} by profile user {updatedByUserId}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating sub-action update notification for sub-action {SubActionId}", subActionId);
            }
        }

        /// <summary>
        /// 2. Admin cancels a sub-action - Notify assigned HSE user
        /// </summary>
        public async Task NotifyHSEOnAdminSubActionCancelledAsync(int subActionId, string cancelledByAdminId)
        {
            try
            {
                var subAction = await _context.SubActions
                    .Include(sa => sa.Action)
                    .ThenInclude(a => a.CreatedBy)
                    .Include(sa => sa.Action)
                    .ThenInclude(a => a.Report)
                    .Include(sa => sa.CorrectiveAction)
                    .ThenInclude(ca => ca.CreatedByHSE)
                    .Include(sa => sa.CorrectiveAction)
                    .ThenInclude(ca => ca.Report)
                    .FirstOrDefaultAsync(sa => sa.Id == subActionId);

                var cancelledBy = await _userManager.FindByIdAsync(cancelledByAdminId);
                if (subAction == null || cancelledBy == null) return;

                string? hseUserId = null;
                string? reportTitle = null;
                int? reportId = null;
                string? hseUserName = null;

                // Notify the HSE author who created the corrective action, not the assigned HSE
                if (subAction.CorrectiveAction?.CreatedByHSE != null)
                {
                    hseUserId = subAction.CorrectiveAction.CreatedByHSE.Id;
                    hseUserName = subAction.CorrectiveAction.CreatedByHSE.FullName;
                    reportTitle = subAction.CorrectiveAction.Report?.Title ?? "Unknown Report";
                    reportId = subAction.CorrectiveAction.ReportId;
                }
                else if (subAction.Action?.CreatedBy != null)
                {
                    hseUserId = subAction.Action.CreatedBy.Id;
                    hseUserName = subAction.Action.CreatedBy.FullName;
                    reportTitle = subAction.Action.Report?.Title ?? "Unknown Report";
                    reportId = subAction.Action.ReportId;
                }

                if (hseUserId == null) 
                {
                    _logger.LogWarning("No HSE author found for sub-action {SubActionId} - cannot send admin cancellation notification", subActionId);
                    return;
                }

                // Generate redirect URL based on parent type
                string redirectUrl = "/actions"; // Default
                if (reportId != null)
                {
                    if (subAction.ActionId != null)
                    {
                        redirectUrl = $"http://192.168.0.245:4200/reports/{reportId}#action-{subAction.ActionId}";
                    }
                    else if (subAction.CorrectiveActionId != null)
                    {
                        redirectUrl = $"http://192.168.0.245:4200/reports/{reportId}#corrective-action-{subAction.CorrectiveActionId}";
                    }
                }

                var parentTitle = subAction.CorrectiveAction?.Title ?? subAction.Action?.Title ?? "Unknown";
                
                var notification = new Notification
                {
                    Title = "Sub-Action Cancelled by Admin",
                    Message = $"Admin {cancelledBy.FullName} cancelled sub-action '{subAction.Title}' in your corrective action '{parentTitle}' (Report: '{reportTitle}').",
                    Type = "SubActionCancelledByAdmin", 
                    UserId = hseUserId,
                    TriggeredByUserId = cancelledByAdminId,
                    RelatedCorrectiveActionId = subAction.CorrectiveActionId,
                    RelatedActionId = subAction.ActionId,
                    RelatedReportId = reportId,
                    RedirectUrl = redirectUrl,
                    CreatedAt = DateTime.UtcNow
                };

                _context.Notifications.Add(notification);
                await _context.SaveChangesAsync();
                
                _logger.LogInformation("✅ Created admin cancellation notification for HSE user {HSEUserId} ({HSEUserName}) - SubAction {SubActionId} cancelled by admin {AdminId} ({AdminName})", 
                    hseUserId, hseUserName, subActionId, cancelledByAdminId, cancelledBy.FullName);

                // Send email notification
                var hseUser = await _userManager.FindByIdAsync(hseUserId);
                if (hseUser != null && !string.IsNullOrEmpty(hseUser.Email))
                {
                    var subject = "Sub-Action Cancelled by Admin";
                    var emailBody = GenerateAdminActionEmailBody(hseUser, cancelledBy, subAction.Title, reportTitle, "Cancelled", redirectUrl);
                    
                    try
                    {
                        await _emailService.SendGenericEmail(hseUser.Email, subject, emailBody);
                        
                        notification.IsEmailSent = true;
                        notification.EmailSentAt = DateTime.UtcNow;
                        await _context.SaveChangesAsync();
                        
                        _logger.LogInformation("✅ Sent admin cancellation email to {HSEEmail} for sub-action {SubActionId}", hseUser.Email, subActionId);
                    }
                    catch (Exception emailEx)
                    {
                        _logger.LogError(emailEx, "❌ Failed to send email notification for sub-action cancellation by admin to {HSEEmail}", hseUser.Email);
                    }
                }
                else
                {
                    _logger.LogWarning("⚠️ No email sent for admin cancellation - HSE user {HSEUserId} not found or has no email", hseUserId);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating sub-action cancellation notification for sub-action {SubActionId}", subActionId);
            }
        }

        /// <summary>
        /// 2B. Admin aborts a corrective action - Notify HSE author
        /// </summary>
        public async Task NotifyHSEOnCorrectiveActionAbortedAsync(int correctiveActionId, string abortedByAdminId, string reason)
        {
            try
            {
                var correctiveAction = await _context.CorrectiveActions
                    .Include(ca => ca.CreatedByHSE)
                    .Include(ca => ca.Report)
                    .FirstOrDefaultAsync(ca => ca.Id == correctiveActionId);

                var abortedBy = await _userManager.FindByIdAsync(abortedByAdminId);
                if (correctiveAction == null || abortedBy == null || correctiveAction.CreatedByHSE == null) 
                {
                    _logger.LogWarning("Cannot send corrective action abort notification - CorrectiveAction: {CAExists}, AbortedBy: {AdminExists}, HSEAuthor: {HSEExists}", 
                        correctiveAction != null, abortedBy != null, correctiveAction?.CreatedByHSE != null);
                    return;
                }

                string hseUserId = correctiveAction.CreatedByHSE.Id;
                string hseUserName = correctiveAction.CreatedByHSE.FullName;
                string reportTitle = correctiveAction.Report?.Title ?? "Unknown Report";
                int? reportId = correctiveAction.ReportId;

                // Generate redirect URL
                string redirectUrl = $"http://192.168.0.245:4200/reports/{reportId}#corrective-action-{correctiveActionId}";

                var notification = new Notification
                {
                    Title = "Corrective Action Aborted by Admin",
                    Message = $"Admin {abortedBy.FullName} aborted your corrective action '{correctiveAction.Title}' in report '{reportTitle}'. Reason: {reason}",
                    Type = "CorrectiveActionAbortedByAdmin",
                    UserId = hseUserId,
                    TriggeredByUserId = abortedByAdminId,
                    RelatedCorrectiveActionId = correctiveActionId,
                    RelatedReportId = reportId,
                    RedirectUrl = redirectUrl,
                    CreatedAt = DateTime.UtcNow
                };

                _context.Notifications.Add(notification);
                await _context.SaveChangesAsync();
                
                _logger.LogInformation("✅ Created corrective action abort notification for HSE user {HSEUserId} ({HSEUserName}) - CA {CAId} aborted by admin {AdminId} ({AdminName})", 
                    hseUserId, hseUserName, correctiveActionId, abortedByAdminId, abortedBy.FullName);

                // Send email notification with specific corrective action abort template
                if (!string.IsNullOrEmpty(correctiveAction.CreatedByHSE.Email))
                {
                    var subject = "Corrective Action Aborted by Admin";
                    var emailBody = GenerateCorrectiveActionAbortEmailBody(correctiveAction.CreatedByHSE, abortedBy, correctiveAction.Title, reportTitle, reason, redirectUrl);
                    
                    try
                    {
                        await _emailService.SendGenericEmail(correctiveAction.CreatedByHSE.Email, subject, emailBody);
                        
                        notification.IsEmailSent = true;
                        notification.EmailSentAt = DateTime.UtcNow;
                        await _context.SaveChangesAsync();
                        
                        _logger.LogInformation("✅ Sent corrective action abort email to {HSEEmail} for CA {CAId}", correctiveAction.CreatedByHSE.Email, correctiveActionId);
                    }
                    catch (Exception emailEx)
                    {
                        _logger.LogError(emailEx, "❌ Failed to send corrective action abort email to {HSEEmail}", correctiveAction.CreatedByHSE.Email);
                    }
                }
                else
                {
                    _logger.LogWarning("⚠️ No email sent for corrective action abort - HSE user {HSEUserId} has no email", hseUserId);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating corrective action abort notification for CA {CAId}", correctiveActionId);
            }
        }

        /// <summary>
        /// 2. Admin cancels an action - Notify assigned HSE user
        /// </summary>
        public async Task NotifyHSEOnAdminActionCancelledAsync(int actionId, string cancelledByAdminId)
        {
            try
            {
                var action = await _context.Actions
                    .Include(a => a.Report)
                    .ThenInclude(r => r.AssignedHSE)
                    .FirstOrDefaultAsync(a => a.Id == actionId);

                var cancelledBy = await _userManager.FindByIdAsync(cancelledByAdminId);
                if (action?.Report?.AssignedHSE == null || cancelledBy == null) return;

                var notification = new Notification
                {
                    Title = "Action Cancelled by Admin",
                    Message = $"Admin {cancelledBy.FullName} cancelled action '{action.Title}' in report '{action.Report.Title}'.",
                    Type = "ActionCancelledByAdmin",
                    UserId = action.Report.AssignedHSE.Id,
                    TriggeredByUserId = cancelledByAdminId,
                    RelatedActionId = actionId,
                    RelatedReportId = action.ReportId,
                    RedirectUrl = $"http://192.168.0.245:4200/reports/{action.ReportId}#action-{actionId}",
                    CreatedAt = DateTime.UtcNow
                };

                _context.Notifications.Add(notification);
                await _context.SaveChangesAsync();

                // Send email notification
                if (!string.IsNullOrEmpty(action.Report.AssignedHSE.Email))
                {
                    var subject = "Action Cancelled by Admin";
                    var emailBody = GenerateAdminActionEmailBody(action.Report.AssignedHSE, cancelledBy, action.Title, action.Report.Title, "Cancelled", notification.RedirectUrl);
                    
                    try
                    {
                        await _emailService.SendGenericEmail(action.Report.AssignedHSE.Email, subject, emailBody);
                        
                        notification.IsEmailSent = true;
                        notification.EmailSentAt = DateTime.UtcNow;
                        await _context.SaveChangesAsync();
                        
                        _logger.LogInformation($"Sent email notification to {action.Report.AssignedHSE.Email} for action cancellation by admin");
                    }
                    catch (Exception emailEx)
                    {
                        _logger.LogError(emailEx, "Failed to send email notification for action cancellation by admin");
                    }
                }

                _logger.LogInformation($"Created action cancellation notification for HSE user {action.Report.AssignedHSE.Id}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating action cancellation notification for action {ActionId}", actionId);
            }
        }

        /// <summary>
        /// 2. Admin aborts an action - Notify assigned HSE user
        /// </summary>
        public async Task NotifyHSEOnAdminActionAbortedAsync(int actionId, string abortedByAdminId)
        {
            try
            {
                var action = await _context.Actions
                    .Include(a => a.Report)
                    .ThenInclude(r => r.AssignedHSE)
                    .FirstOrDefaultAsync(a => a.Id == actionId);

                var abortedBy = await _userManager.FindByIdAsync(abortedByAdminId);
                if (action?.Report?.AssignedHSE == null || abortedBy == null) return;

                // Generate redirect URL to the report with action focus
                string redirectUrl = action.ReportId != null 
                    ? $"http://192.168.0.245:4200/reports/{action.ReportId}#action-{actionId}"
                    : "http://192.168.0.245:4200/actions";

                var notification = new Notification
                {
                    Title = "Action Aborted by Admin",
                    Message = $"Admin {abortedBy.FullName} aborted action '{action.Title}' in report '{action.Report.Title}'. Reason: {action.AbortReason ?? "Not specified"}",
                    Type = "ActionAbortedByAdmin",
                    UserId = action.Report.AssignedHSE.Id,
                    TriggeredByUserId = abortedByAdminId,
                    RelatedActionId = actionId,
                    RelatedReportId = action.ReportId,
                    RedirectUrl = redirectUrl,
                    CreatedAt = DateTime.UtcNow
                };

                _context.Notifications.Add(notification);
                await _context.SaveChangesAsync();

                // Send email notification
                if (!string.IsNullOrEmpty(action.Report.AssignedHSE.Email))
                {
                    var subject = "Action Aborted by Admin";
                    var emailBody = GenerateAdminActionEmailBody(action.Report.AssignedHSE, abortedBy, action.Title, action.Report.Title, "Aborted", redirectUrl, action.AbortReason);
                    
                    try
                    {
                        await _emailService.SendGenericEmail(action.Report.AssignedHSE.Email, subject, emailBody);
                        
                        notification.IsEmailSent = true;
                        notification.EmailSentAt = DateTime.UtcNow;
                        await _context.SaveChangesAsync();
                        
                        _logger.LogInformation($"Sent email notification to {action.Report.AssignedHSE.Email} for action abort by admin");
                    }
                    catch (Exception emailEx)
                    {
                        _logger.LogError(emailEx, "Failed to send email notification for action abort by admin");
                    }
                }

                _logger.LogInformation($"Created action abort notification for HSE user {action.Report.AssignedHSE.Id}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating action abort notification for action {ActionId}", actionId);
            }
        }

        /// <summary>
        /// 2. Admin creates an action - Notify assigned HSE user
        /// </summary>
        public async Task NotifyHSEOnAdminActionCreatedAsync(int actionId, string createdByAdminId, string hseUserId)
        {
            try
            {
                var action = await _context.Actions
                    .Include(a => a.Report)
                    .FirstOrDefaultAsync(a => a.Id == actionId);

                var createdBy = await _userManager.FindByIdAsync(createdByAdminId);
                if (action == null || createdBy == null) return;

                // Generate redirect URL to the report with action focus
                string redirectUrl = action.ReportId != null 
                    ? $"http://192.168.0.245:4200/reports/{action.ReportId}#action-{actionId}"
                    : "http://192.168.0.245:4200/actions";

                var notification = new Notification
                {
                    Title = "New Action Created by Admin",
                    Message = $"Admin {createdBy.FullName} created a new action '{action.Title}' related to report '{action.Report?.Title}' and assigned it to you.",
                    Type = "ActionCreatedByAdmin",
                    UserId = hseUserId,
                    TriggeredByUserId = createdByAdminId,
                    RelatedActionId = actionId,
                    RelatedReportId = action.ReportId,
                    RedirectUrl = redirectUrl,
                    CreatedAt = DateTime.UtcNow
                };

                _context.Notifications.Add(notification);
                await _context.SaveChangesAsync();

                // Send email notification
                var hseUser = await _userManager.FindByIdAsync(hseUserId);
                if (hseUser != null && !string.IsNullOrEmpty(hseUser.Email))
                {
                    var subject = "New Action Created by Admin";
                    var emailBody = GenerateAdminActionEmailBody(hseUser, createdBy, action.Title, action.Report?.Title ?? "Unknown Report", "Created", redirectUrl);
                    
                    try
                    {
                        await _emailService.SendGenericEmail(hseUser.Email, subject, emailBody);
                        
                        notification.IsEmailSent = true;
                        notification.EmailSentAt = DateTime.UtcNow;
                        await _context.SaveChangesAsync();
                        
                        _logger.LogInformation($"Sent email notification to {hseUser.Email} for action creation by admin");
                    }
                    catch (Exception emailEx)
                    {
                        _logger.LogError(emailEx, "Failed to send email notification for action creation by admin");
                    }
                }

                _logger.LogInformation($"Created admin action creation notification for HSE user {hseUserId}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating admin action creation notification for action {ActionId}", actionId);
            }
        }

        /// <summary>
        /// 3. Overdue corrective actions - Notify author HSE
        /// </summary>
        public async Task NotifyHSEOnOverdueCorrectiveActionsAsync()
        {
            try
            {
                var today = DateTime.UtcNow.Date;
                var overdueCorrectiveActions = await _context.CorrectiveActions
                    .Include(ca => ca.CreatedByHSE)
                    .Include(ca => ca.AssignedToProfile) // Include assigned profile user
                    .Include(ca => ca.Report)
                    .Where(ca => ca.DueDate.Date < today && 
                                !ca.IsCompleted && 
                                ca.Status != "Completed" && 
                                ca.Status != "Cancelled")
                    .ToListAsync();

                foreach (var correctiveAction in overdueCorrectiveActions)
                {
                    var daysOverdue = (today - correctiveAction.DueDate.Date).Days;
                    // Generate redirect URL to the report with corrective action focus
                    string redirectUrl = correctiveAction.ReportId != null 
                        ? $"http://192.168.0.245:4200/reports/{correctiveAction.ReportId}#corrective-action-{correctiveAction.Id}"
                        : "http://192.168.0.245:4200/actions";

                    // FIRST: Notify assigned profile user (most important)
                    if (!string.IsNullOrEmpty(correctiveAction.AssignedToProfileId) && correctiveAction.AssignedToProfile != null)
                    {
                        var assignedUserNotification = new Notification
                        {
                            Title = "Corrective Action Overdue - Action Required",
                            Message = $"Your assigned corrective action '{correctiveAction.Title}' is {daysOverdue} day(s) overdue (due: {correctiveAction.DueDate:MM/dd/yyyy}). Please complete it urgently.",
                            Type = "CorrectiveActionOverdue",
                            UserId = correctiveAction.AssignedToProfileId,
                            RelatedCorrectiveActionId = correctiveAction.Id,
                            RelatedReportId = correctiveAction.ReportId,
                            RedirectUrl = redirectUrl,
                            CreatedAt = DateTime.UtcNow
                        };

                        _context.Notifications.Add(assignedUserNotification);
                        
                        // Send email to assigned profile user
                        if (!string.IsNullOrEmpty(correctiveAction.AssignedToProfile.Email))
                        {
                            var subject = "Urgent: Corrective Action Overdue";
                            var emailBody = GenerateOverdueNotificationEmailBody(correctiveAction.AssignedToProfile, "Corrective Action", correctiveAction.Title, daysOverdue, correctiveAction.DueDate, redirectUrl, correctiveAction.Report?.Title);
                            
                            try
                            {
                                await _emailService.SendGenericEmail(correctiveAction.AssignedToProfile.Email, subject, emailBody);
                                
                                assignedUserNotification.IsEmailSent = true;
                                assignedUserNotification.EmailSentAt = DateTime.UtcNow;
                                
                                _logger.LogInformation($"Sent overdue corrective action email to assigned user {correctiveAction.AssignedToProfile.Email}");
                            }
                            catch (Exception emailEx)
                            {
                                _logger.LogError(emailEx, "Failed to send overdue corrective action email to assigned user");
                            }
                        }
                    }

                    // SECOND: Notify HSE author (for monitoring purposes - NO EMAIL for HSE)
                    if (correctiveAction.CreatedByHSE != null)
                    {
                        var hseNotification = new Notification
                        {
                            Title = "Corrective Action Overdue (Monitoring)",
                            Message = $"Corrective action '{correctiveAction.Title}' assigned to {correctiveAction.AssignedToProfile?.FullName ?? "Unknown"} is {daysOverdue} day(s) overdue in report '{correctiveAction.Report?.Title}'.",
                            Type = "CorrectiveActionOverdueMonitoring",
                            UserId = correctiveAction.CreatedByHSE.Id,
                            RelatedCorrectiveActionId = correctiveAction.Id,
                            RelatedReportId = correctiveAction.ReportId,
                            RedirectUrl = redirectUrl,
                            CreatedAt = DateTime.UtcNow
                        };

                        _context.Notifications.Add(hseNotification);
                        // Note: NO email sent to HSE author - only in-system notification for monitoring
                    }

                    // THIRD: Notify admins about overdue HSE corrective actions
                    var adminUsers = await GetAdminUsersAsync();
                    foreach (var admin in adminUsers)
                    {
                        var adminNotification = new Notification
                        {
                            Title = "HSE Corrective Action Overdue",
                            Message = $"HSE corrective action '{correctiveAction.Title}' by {correctiveAction.CreatedByHSE?.FullName ?? "Unknown HSE"} is {daysOverdue} day(s) overdue in report '{correctiveAction.Report?.Title}'.",
                            Type = "AdminOverdueAlert",
                            UserId = admin.Id,
                            RelatedCorrectiveActionId = correctiveAction.Id,
                            RelatedReportId = correctiveAction.ReportId,
                            RedirectUrl = redirectUrl,
                            CreatedAt = DateTime.UtcNow
                        };

                        _context.Notifications.Add(adminNotification);
                    }
                }

                await _context.SaveChangesAsync();
                _logger.LogInformation($"Created {overdueCorrectiveActions.Count} overdue corrective action notifications");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating overdue corrective action notifications");
            }
        }

        /// <summary>
        /// 3. Overdue sub-actions - Notify assigned profile users and author HSE
        /// </summary>
        public async Task NotifyHSEOnOverdueSubActionsAsync()
        {
            try
            {
                var today = DateTime.UtcNow.Date;
                var overdueSubActions = await _context.SubActions
                    .Include(sa => sa.Action)
                    .ThenInclude(a => a.CreatedBy)
                    .Include(sa => sa.CorrectiveAction)
                    .ThenInclude(ca => ca.CreatedByHSE)
                    .Include(sa => sa.Action.Report)
                    .Include(sa => sa.CorrectiveAction.Report)
                    .Include(sa => sa.AssignedTo) // Include assigned user
                    .Where(sa => sa.DueDate.HasValue && 
                                sa.DueDate.Value.Date < today && 
                                sa.Status != "Completed" && 
                                sa.Status != "Cancelled")
                    .ToListAsync();

                foreach (var subAction in overdueSubActions)
                {
                    string? authorId = null;
                    string? reportTitle = null;
                    int? reportId = null;

                    if (subAction.Action?.CreatedBy != null)
                    {
                        authorId = subAction.Action.CreatedBy.Id;
                        reportTitle = subAction.Action.Report?.Title;
                        reportId = subAction.Action.ReportId;
                    }
                    else if (subAction.CorrectiveAction?.CreatedByHSE != null)
                    {
                        authorId = subAction.CorrectiveAction.CreatedByHSE.Id;
                        reportTitle = subAction.CorrectiveAction.Report?.Title;
                        reportId = subAction.CorrectiveAction.ReportId;
                    }

                    var daysOverdue = (today - subAction.DueDate.Value.Date).Days;
                    
                    // Generate redirect URL based on parent type
                    string redirectUrl = "http://192.168.0.245:4200/actions"; // Default
                    if (reportId != null)
                    {
                        if (subAction.ActionId != null)
                        {
                            redirectUrl = $"http://192.168.0.245:4200/reports/{reportId}#action-{subAction.ActionId}";
                        }
                        else if (subAction.CorrectiveActionId != null)
                        {
                            redirectUrl = $"http://192.168.0.245:4200/reports/{reportId}#corrective-action-{subAction.CorrectiveActionId}";
                        }
                    }

                    // FIRST: Notify assigned profile user (most important)
                    if (!string.IsNullOrEmpty(subAction.AssignedToId))
                    {
                        var assignedUserNotification = new Notification
                        {
                            Title = "Sub-Action Overdue - Action Required",
                            Message = $"Your assigned sub-action '{subAction.Title}' is {daysOverdue} day(s) overdue (due: {subAction.DueDate.Value:MM/dd/yyyy}). Please complete it urgently.",
                            Type = "SubActionOverdue",
                            UserId = subAction.AssignedToId,
                            RelatedCorrectiveActionId = subAction.CorrectiveActionId,
                            RelatedActionId = subAction.ActionId,
                            RelatedReportId = reportId,
                            RedirectUrl = redirectUrl,
                            CreatedAt = DateTime.UtcNow
                        };

                        _context.Notifications.Add(assignedUserNotification);
                        
                        // Send email to assigned profile user
                        if (subAction.AssignedTo != null && !string.IsNullOrEmpty(subAction.AssignedTo.Email))
                        {
                            var subject = "Urgent: Sub-Action Overdue";
                            var emailBody = GenerateOverdueNotificationEmailBody(subAction.AssignedTo, "Sub-Action", subAction.Title, daysOverdue, subAction.DueDate.Value, redirectUrl, reportTitle);
                            
                            try
                            {
                                await _emailService.SendGenericEmail(subAction.AssignedTo.Email, subject, emailBody);
                                
                                assignedUserNotification.IsEmailSent = true;
                                assignedUserNotification.EmailSentAt = DateTime.UtcNow;
                                
                                _logger.LogInformation($"Sent overdue sub-action email to assigned user {subAction.AssignedTo.Email}");
                            }
                            catch (Exception emailEx)
                            {
                                _logger.LogError(emailEx, "Failed to send overdue sub-action email to assigned user");
                            }
                        }
                    }

                    // SECOND: Notify HSE author (for monitoring purposes - NO EMAIL for HSE)
                    if (authorId != null)
                    {
                        var hseNotification = new Notification
                        {
                            Title = "Sub-Action Overdue (Monitoring)",
                            Message = $"Sub-action '{subAction.Title}' assigned to {subAction.AssignedTo?.FullName ?? "Unknown"} is {daysOverdue} day(s) overdue in report '{reportTitle}'.",
                            Type = "SubActionOverdueMonitoring",
                            UserId = authorId,
                            RelatedCorrectiveActionId = subAction.CorrectiveActionId,
                            RelatedActionId = subAction.ActionId,
                            RelatedReportId = reportId,
                            RedirectUrl = redirectUrl,
                            CreatedAt = DateTime.UtcNow
                        };

                        _context.Notifications.Add(hseNotification);
                        // Note: NO email sent to HSE author - only in-system notification for monitoring
                    }

                    // THIRD: Notify admins about overdue HSE sub-actions
                    var adminUsers = await GetAdminUsersAsync();
                    foreach (var admin in adminUsers)
                    {
                        var parentInfo = subAction.CorrectiveAction?.Title ?? subAction.Action?.Title ?? "Unknown";
                        var hseAuthor = subAction.CorrectiveAction?.CreatedByHSE?.FullName ?? subAction.Action?.CreatedBy?.FullName ?? "Unknown HSE";
                        
                        var adminNotification = new Notification
                        {
                            Title = "HSE Sub-Action Overdue",
                            Message = $"HSE sub-action '{subAction.Title}' in corrective action '{parentInfo}' by {hseAuthor} is {daysOverdue} day(s) overdue in report '{reportTitle}'.",
                            Type = "AdminOverdueAlert",
                            UserId = admin.Id,
                            RelatedCorrectiveActionId = subAction.CorrectiveActionId,
                            RelatedActionId = subAction.ActionId,
                            RelatedReportId = reportId,
                            RedirectUrl = redirectUrl,
                            CreatedAt = DateTime.UtcNow
                        };

                        _context.Notifications.Add(adminNotification);
                    }
                }

                await _context.SaveChangesAsync();
                _logger.LogInformation($"Created overdue notifications for {overdueSubActions.Count} sub-actions (emails sent to assigned users only)");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating overdue sub-action notifications");
            }
        }

        // Helper methods
        private async Task<List<ApplicationUser>> GetHSEUsersForZoneAsync(string zoneName)
        {
            var currentTime = DateTime.UtcNow;
            
            // Get the zone ID from the zone name
            var zone = await _context.Zones.FirstOrDefaultAsync(z => z.Name == zoneName);
            if (zone == null)
            {
                _logger.LogWarning("Zone '{ZoneName}' not found", zoneName);
                return new List<ApplicationUser>();
            }

            // Get original HSE users assigned to this zone
            var originalHSEUsers = await _context.Users
                .Include(u => u.ResponsibleZones)
                .ThenInclude(rz => rz.Zone)
                .Where(u => u.ResponsibleZones.Any(rz => rz.Zone.Name == zoneName && rz.IsActive))
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
                .ToListAsync();

            // Combine both lists and remove duplicates
            var allHSEUsers = originalHSEUsers.Union(delegatedHSEUsers).ToList();

            _logger.LogInformation("Found {OriginalCount} original HSE users and {DelegatedCount} delegated HSE users for zone '{ZoneName}'. Total: {TotalCount}", 
                originalHSEUsers.Count, delegatedHSEUsers.Count, zoneName, allHSEUsers.Count);

            return allHSEUsers;
        }

        private async Task<List<ApplicationUser>> GetAdminUsersAsync()
        {
            var adminRoleUsers = await _userManager.GetUsersInRoleAsync("Admin");
            return adminRoleUsers.Where(u => u.IsActive).ToList();
        }

        private async Task<List<ApplicationUser>> GetHSEUsersAsync()
        {
            var hseRoleUsers = await _userManager.GetUsersInRoleAsync("HSE");
            return hseRoleUsers.Where(u => u.IsActive).ToList();
        }

        // Email template methods
        private string GenerateSubActionUpdateEmailBody(ApplicationUser author, ApplicationUser updatedBy, SubAction subAction, string parentType, string parentTitle, string redirectUrl)
        {
            var sb = new StringBuilder();
            sb.AppendLine($"<h2>📋 Sub-Action Updated</h2>");
            sb.AppendLine($"<p>Dear {author.FullName},</p>");
            sb.AppendLine($"<p>A profile user has updated a sub-action in your {parentType}:</p>");
            
            sb.AppendLine($"<div style='background-color: #e3f2fd; padding: 20px; border-left: 4px solid #2196F3; margin: 20px 0; border-radius: 5px;'>");
            sb.AppendLine($"<h3 style='color: #1976D2; margin-top: 0;'>{parentType.ToUpper()}: {parentTitle}</h3>");
            sb.AppendLine($"<p><strong>Sub-Action:</strong> {subAction.Title}</p>");
            sb.AppendLine($"<p><strong>Updated by:</strong> {updatedBy.FullName} (ID: {updatedBy.CompanyId})</p>");
            sb.AppendLine($"<p><strong>Current Status:</strong> <span style='background-color: #e8f5e8; padding: 3px 8px; border-radius: 3px; color: #2e7d32;'>{subAction.Status}</span></p>");
            
            if (subAction.DueDate.HasValue)
            {
                var daysUntilDue = (subAction.DueDate.Value.Date - DateTime.UtcNow.Date).Days;
                var dueDateColor = daysUntilDue <= 3 ? "#d32f2f" : daysUntilDue <= 7 ? "#ff9800" : "#388e3c";
                sb.AppendLine($"<p><strong>Due Date:</strong> <span style='color: {dueDateColor}; font-weight: bold;'>{subAction.DueDate.Value:MM/dd/yyyy}</span></p>");
            }
            sb.AppendLine($"</div>");
            
            sb.AppendLine($"<div style='text-align: center; margin: 30px 0;'>");
            sb.AppendLine($"<a href='{redirectUrl}' style='background-color: #2196F3; color: white; padding: 12px 24px; text-decoration: none; border-radius: 5px; display: inline-block;'>View Details</a>");
            sb.AppendLine($"</div>");
            
            sb.AppendLine($"<p>Please review the updated sub-action and follow up if necessary.</p>");
            sb.AppendLine($"<p>Best regards,<br><strong>HSE Management System</strong></p>");

            return sb.ToString();
        }

        private string GenerateAdminActionEmailBody(ApplicationUser recipient, ApplicationUser admin, string actionTitle, string reportTitle, string actionType, string redirectUrl, string? reason = null)
        {
            var sb = new StringBuilder();
            sb.AppendLine($"<h2>⚠️ Admin Action: {actionType}</h2>");
            sb.AppendLine($"<p>Dear {recipient.FullName},</p>");
            sb.AppendLine($"<p>An administrator has performed an action that affects your work:</p>");
            
            sb.AppendLine($"<div style='background-color: #fff3e0; padding: 20px; border-left: 4px solid #ff9800; margin: 20px 0; border-radius: 5px;'>");
            sb.AppendLine($"<h3 style='color: #f57c00; margin-top: 0;'>Action Details</h3>");
            sb.AppendLine($"<p><strong>Action:</strong> {actionTitle}</p>");
            sb.AppendLine($"<p><strong>Report:</strong> {reportTitle}</p>");
            sb.AppendLine($"<p><strong>Admin:</strong> {admin.FullName}</p>");
            sb.AppendLine($"<p><strong>Action Type:</strong> {actionType}</p>");
            
            if (!string.IsNullOrEmpty(reason))
            {
                sb.AppendLine($"<p><strong>Reason:</strong> {reason}</p>");
            }
            sb.AppendLine($"</div>");
            
            sb.AppendLine($"<div style='text-align: center; margin: 30px 0;'>");
            sb.AppendLine($"<a href='{redirectUrl}' style='background-color: #ff9800; color: white; padding: 12px 24px; text-decoration: none; border-radius: 5px; display: inline-block;'>View Details</a>");
            sb.AppendLine($"</div>");
            
            sb.AppendLine($"<p>Please review this change and adjust your workflow accordingly.</p>");
            sb.AppendLine($"<p>Best regards,<br><strong>HSE Management System</strong></p>");

            return sb.ToString();
        }

        private string GenerateOverdueNotificationEmailBody(ApplicationUser recipient, string itemType, string itemTitle, int daysOverdue, DateTime dueDate, string redirectUrl, string? reportTitle = null)
        {
            var sb = new StringBuilder();
            sb.AppendLine($"<h2>🚨 Overdue {itemType}</h2>");
            sb.AppendLine($"<p>Dear {recipient.FullName},</p>");
            sb.AppendLine($"<p>You have an overdue {itemType.ToLower()} that requires immediate attention:</p>");
            
            sb.AppendLine($"<div style='background-color: #ffebee; padding: 20px; border-left: 4px solid #f44336; margin: 20px 0; border-radius: 5px;'>");
            sb.AppendLine($"<h3 style='color: #d32f2f; margin-top: 0;'>{itemType}: {itemTitle}</h3>");
            
            if (!string.IsNullOrEmpty(reportTitle))
            {
                sb.AppendLine($"<p><strong>Report:</strong> {reportTitle}</p>");
            }
            
            sb.AppendLine($"<p><strong>Was Due:</strong> {dueDate:MM/dd/yyyy}</p>");
            sb.AppendLine($"<p style='color: #d32f2f; font-weight: bold;'>⚠️ Overdue by {daysOverdue} day(s)</p>");
            sb.AppendLine($"</div>");
            
            sb.AppendLine($"<div style='text-align: center; margin: 30px 0;'>");
            sb.AppendLine($"<a href='{redirectUrl}' style='background-color: #f44336; color: white; padding: 12px 24px; text-decoration: none; border-radius: 5px; display: inline-block;'>Take Action Now</a>");
            sb.AppendLine($"</div>");
            
            sb.AppendLine($"<p><strong>Please complete this {itemType.ToLower()} as soon as possible and update its status.</strong></p>");
            sb.AppendLine($"<p>Best regards,<br><strong>HSE Management System</strong></p>");

            return sb.ToString();
        }

        private string GenerateCorrectiveActionAbortEmailBody(ApplicationUser recipient, ApplicationUser admin, string correctiveActionTitle, string reportTitle, string reason, string redirectUrl)
        {
            var sb = new StringBuilder();
            sb.AppendLine($"<h2>🚫 Corrective Action Aborted by Admin</h2>");
            sb.AppendLine($"<p>Dear {recipient.FullName},</p>");
            sb.AppendLine($"<p>An administrator has aborted your corrective action:</p>");
            
            sb.AppendLine("<div style='background-color: #ffe6e6; padding: 15px; border-left: 4px solid #e74c3c; margin: 20px 0;'>");
            sb.AppendLine($"<h3 style='margin-top: 0; color: #c0392b;'>Corrective Action Aborted</h3>");
            sb.AppendLine($"<p><strong>Corrective Action:</strong> {correctiveActionTitle}</p>");
            sb.AppendLine($"<p><strong>Report:</strong> {reportTitle}</p>");
            sb.AppendLine($"<p><strong>Aborted by:</strong> {admin.FullName}</p>");
            if (!string.IsNullOrEmpty(reason))
            {
                sb.AppendLine($"<p><strong>Reason:</strong> {reason}</p>");
            }
            sb.AppendLine("</div>");
            
            sb.AppendLine("<p>Your corrective action has been permanently aborted and cannot be continued. If you believe this was done in error, please contact the administrator.</p>");
            
            sb.AppendLine("<div style='text-align: center; margin: 30px 0;'>");
            sb.AppendLine($"<a href='{redirectUrl}' style='background-color: #e74c3c; color: white; padding: 12px 24px; text-decoration: none; border-radius: 5px; display: inline-block;'>View Report</a>");
            sb.AppendLine("</div>");
            
            sb.AppendLine("<p>Best regards,<br>HSE Management System</p>");
            
            return sb.ToString();
        }

        // ===== ZONE DELEGATION NOTIFICATIONS =====
        
        /// <summary>
        /// Notify both parties when a zone delegation is created
        /// </summary>
        public async Task NotifyOnZoneDelegationCreatedAsync(int delegationId)
        {
            try
            {
                var delegation = await _context.HSEZoneDelegations
                    .Include(hzd => hzd.FromHSEUser)
                    .Include(hzd => hzd.ToHSEUser)
                    .Include(hzd => hzd.Zone)
                    .Include(hzd => hzd.CreatedByAdmin)
                    .FirstOrDefaultAsync(hzd => hzd.Id == delegationId);

                if (delegation == null)
                {
                    _logger.LogWarning("Zone delegation {DelegationId} not found for notification", delegationId);
                    return;
                }

                var startDate = delegation.StartDate.ToString("MM/dd/yyyy");
                var endDate = delegation.EndDate.ToString("MM/dd/yyyy");
                var duration = (delegation.EndDate - delegation.StartDate).Days;
                var zoneName = delegation.Zone.Name;
                var adminName = delegation.CreatedByAdmin.FullName;

                // Notify the original HSE user (FROM user - who is delegating away)
                var fromNotification = new Notification
                {
                    Title = "Zone Delegation Created - You are Temporarily Relieved",
                    Message = $"Admin {adminName} has delegated your responsibility for zone '{zoneName}' to {delegation.ToHSEUser.FullName} from {startDate} to {endDate} ({duration} days). Reason: {delegation.Reason ?? "Not specified"}.",
                    Type = "ZoneDelegationFrom",
                    UserId = delegation.FromHSEUserId,
                    TriggeredByUserId = delegation.CreatedByAdminId,
                    CreatedAt = DateTime.UtcNow
                };

                // Notify the delegated HSE user (TO user - who is taking over)
                var toNotification = new Notification
                {
                    Title = "Zone Delegation Assigned - You are Temporarily Responsible",
                    Message = $"Admin {adminName} has assigned you temporary responsibility for zone '{zoneName}' (delegated from {delegation.FromHSEUser.FullName}) from {startDate} to {endDate} ({duration} days). Reason: {delegation.Reason ?? "Not specified"}.",
                    Type = "ZoneDelegationTo",
                    UserId = delegation.ToHSEUserId,
                    TriggeredByUserId = delegation.CreatedByAdminId,
                    CreatedAt = DateTime.UtcNow
                };

                _context.Notifications.Add(fromNotification);
                _context.Notifications.Add(toNotification);
                await _context.SaveChangesAsync();

                // Send emails to both parties
                await SendDelegationCreatedEmailAsync(delegation, isDelegatingUser: true);
                await SendDelegationCreatedEmailAsync(delegation, isDelegatingUser: false);

                _logger.LogInformation("Created zone delegation notifications for delegation {DelegationId} - notified both FROM user {FromUserId} and TO user {ToUserId}", 
                    delegationId, delegation.FromHSEUserId, delegation.ToHSEUserId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating zone delegation notifications for delegation {DelegationId}", delegationId);
            }
        }

        /// <summary>
        /// Notify both parties when a zone delegation ends (automatically or manually)
        /// </summary>
        public async Task NotifyOnZoneDelegationEndedAsync(int delegationId)
        {
            try
            {
                var delegation = await _context.HSEZoneDelegations
                    .Include(hzd => hzd.FromHSEUser)
                    .Include(hzd => hzd.ToHSEUser)
                    .Include(hzd => hzd.Zone)
                    .Include(hzd => hzd.CreatedByAdmin)
                    .FirstOrDefaultAsync(hzd => hzd.Id == delegationId);

                if (delegation == null) return;

                var endDate = delegation.EndDate.ToString("MM/dd/yyyy");
                var zoneName = delegation.Zone.Name;
                var isExpired = DateTime.UtcNow > delegation.EndDate;
                var endReason = isExpired ? "expired as scheduled" : "ended early by admin";

                // Notify the original HSE user (FROM user - responsibility restored)
                var fromNotification = new Notification
                {
                    Title = "Zone Delegation Ended - Responsibility Restored",
                    Message = $"Your delegation for zone '{zoneName}' to {delegation.ToHSEUser.FullName} has {endReason}. You are now fully responsible for this zone again.",
                    Type = "ZoneDelegationEnded",
                    UserId = delegation.FromHSEUserId,
                    CreatedAt = DateTime.UtcNow
                };

                // Notify the delegated HSE user (TO user - responsibility ended)
                var toNotification = new Notification
                {
                    Title = "Zone Delegation Ended - Temporary Responsibility Completed",
                    Message = $"Your temporary responsibility for zone '{zoneName}' (delegated from {delegation.FromHSEUser.FullName}) has {endReason}. Thank you for covering this zone.",
                    Type = "ZoneDelegationEnded",
                    UserId = delegation.ToHSEUserId,
                    CreatedAt = DateTime.UtcNow
                };

                _context.Notifications.Add(fromNotification);
                _context.Notifications.Add(toNotification);
                await _context.SaveChangesAsync();

                // Send emails to both parties
                await SendDelegationEndedEmailAsync(delegation, isDelegatingUser: true, isExpired);
                await SendDelegationEndedEmailAsync(delegation, isDelegatingUser: false, isExpired);

                _logger.LogInformation("Created zone delegation end notifications for delegation {DelegationId}", delegationId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating zone delegation end notifications for delegation {DelegationId}", delegationId);
            }
        }

        private async Task SendDelegationCreatedEmailAsync(HSEZoneDelegation delegation, bool isDelegatingUser)
        {
            try
            {
                var recipient = isDelegatingUser ? delegation.FromHSEUser : delegation.ToHSEUser;
                var otherParty = isDelegatingUser ? delegation.ToHSEUser : delegation.FromHSEUser;
                
                if (string.IsNullOrEmpty(recipient.Email)) return;

                var subject = isDelegatingUser 
                    ? "Zone Delegation: Temporary Relief from Zone Responsibility"
                    : "Zone Delegation: Temporary Zone Assignment";

                var body = GenerateDelegationCreatedEmailBody(delegation, recipient, otherParty, isDelegatingUser);

                await _emailService.SendGenericEmail(recipient.Email, subject, body);
                _logger.LogInformation("Sent delegation creation email to {Email} (isDelegatingUser: {IsDelegating})", 
                    recipient.Email, isDelegatingUser);
            }
            catch (Exception ex)
            {
                var userType = isDelegatingUser ? "delegating" : "delegate";
                _logger.LogError(ex, "Failed to send delegation creation email to {UserType} user", userType);
            }
        }

        private async Task SendDelegationEndedEmailAsync(HSEZoneDelegation delegation, bool isDelegatingUser, bool isExpired)
        {
            try
            {
                var recipient = isDelegatingUser ? delegation.FromHSEUser : delegation.ToHSEUser;
                
                if (string.IsNullOrEmpty(recipient.Email)) return;

                var subject = isDelegatingUser 
                    ? "Zone Delegation Ended: Responsibility Restored"
                    : "Zone Delegation Ended: Temporary Assignment Completed";

                var body = GenerateDelegationEndedEmailBody(delegation, recipient, isDelegatingUser, isExpired);

                await _emailService.SendGenericEmail(recipient.Email, subject, body);
                _logger.LogInformation("Sent delegation end email to {Email} (isDelegatingUser: {IsDelegating})", 
                    recipient.Email, isDelegatingUser);
            }
            catch (Exception ex)
            {
                var userType = isDelegatingUser ? "delegating" : "delegate";
                _logger.LogError(ex, "Failed to send delegation end email to {UserType} user", userType);
            }
        }

        private string GenerateDelegationCreatedEmailBody(HSEZoneDelegation delegation, ApplicationUser recipient, ApplicationUser otherParty, bool isDelegatingUser)
        {
            var sb = new StringBuilder();
            var startDate = delegation.StartDate.ToString("MM/dd/yyyy");
            var endDate = delegation.EndDate.ToString("MM/dd/yyyy");
            var duration = (delegation.EndDate - delegation.StartDate).Days;
            
            if (isDelegatingUser)
            {
                sb.AppendLine($"<h2>🔄 Zone Delegation Created - Temporary Relief</h2>");
                sb.AppendLine($"<p>Dear {recipient.FullName},</p>");
                sb.AppendLine($"<p>You have been temporarily relieved of your zone responsibility as follows:</p>");
            }
            else
            {
                sb.AppendLine($"<h2>📋 Zone Delegation Assigned - Temporary Responsibility</h2>");
                sb.AppendLine($"<p>Dear {recipient.FullName},</p>");
                sb.AppendLine($"<p>You have been assigned temporary responsibility for a zone as follows:</p>");
            }

            sb.AppendLine($"<div style='background-color: #e3f2fd; padding: 20px; border-left: 4px solid #2196F3; margin: 20px 0; border-radius: 5px;'>");
            sb.AppendLine($"<h3 style='color: #1976D2; margin-top: 0;'>📍 Zone Delegation Details</h3>");
            sb.AppendLine($"<p><strong>Zone:</strong> {delegation.Zone.Name}</p>");
            
            if (isDelegatingUser)
            {
                sb.AppendLine($"<p><strong>Delegated to:</strong> {otherParty.FullName} (ID: {otherParty.CompanyId})</p>");
            }
            else
            {
                sb.AppendLine($"<p><strong>Delegated from:</strong> {otherParty.FullName} (ID: {otherParty.CompanyId})</p>");
            }
            
            sb.AppendLine($"<p><strong>Start Date:</strong> {startDate}</p>");
            sb.AppendLine($"<p><strong>End Date:</strong> {endDate}</p>");
            sb.AppendLine($"<p><strong>Duration:</strong> {duration} days</p>");
            
            if (!string.IsNullOrEmpty(delegation.Reason))
            {
                sb.AppendLine($"<p><strong>Reason:</strong> {delegation.Reason}</p>");
            }
            
            sb.AppendLine($"<p><strong>Created by:</strong> {delegation.CreatedByAdmin.FullName}</p>");
            sb.AppendLine($"</div>");

            if (isDelegatingUser)
            {
                sb.AppendLine($"<div style='background-color: #fff3e0; padding: 15px; border-radius: 5px; margin: 20px 0;'>");
                sb.AppendLine($"<p style='margin: 0;'><strong>📢 Important Notes:</strong></p>");
                sb.AppendLine($"<ul style='margin: 10px 0; padding-left: 20px;'>");
                sb.AppendLine($"<li>You will continue to receive notifications during this period for monitoring purposes</li>");
                sb.AppendLine($"<li>{otherParty.FullName} will also receive all notifications and emails for this zone</li>");
                sb.AppendLine($"<li>Your responsibility will automatically resume on {endDate}</li>");
                sb.AppendLine($"</ul>");
                sb.AppendLine($"</div>");
            }
            else
            {
                sb.AppendLine($"<div style='background-color: #f3e5f5; padding: 15px; border-radius: 5px; margin: 20px 0;'>");
                sb.AppendLine($"<p style='margin: 0;'><strong>📋 Your Responsibilities:</strong></p>");
                sb.AppendLine($"<ul style='margin: 10px 0; padding-left: 20px;'>");
                sb.AppendLine($"<li>You will receive all notifications and emails for this zone during the delegation period</li>");
                sb.AppendLine($"<li>Handle all reports, actions, and HSE activities for this zone</li>");
                sb.AppendLine($"<li>Coordinate with the original HSE user if needed</li>");
                sb.AppendLine($"<li>This temporary assignment will end automatically on {endDate}</li>");
                sb.AppendLine($"</ul>");
                sb.AppendLine($"</div>");
            }

            sb.AppendLine($"<div style='text-align: center; margin: 30px 0;'>");
            var frontendUrl = GetFrontendUrl();
            sb.AppendLine($"<a href='{frontendUrl}/hse-dashboard' style='background-color: #2196F3; color: white; padding: 12px 24px; text-decoration: none; border-radius: 5px; display: inline-block;'>Access HSE Dashboard</a>");
            sb.AppendLine($"</div>");

            sb.AppendLine($"<p>If you have any questions about this delegation, please contact the administrator.</p>");
            sb.AppendLine($"<p>Best regards,<br><strong>HSE Management System</strong></p>");

            return sb.ToString();
        }

        private string GenerateDelegationEndedEmailBody(HSEZoneDelegation delegation, ApplicationUser recipient, bool isDelegatingUser, bool isExpired)
        {
            var sb = new StringBuilder();
            var endReason = isExpired ? "expired as scheduled" : "ended early by admin";
            
            if (isDelegatingUser)
            {
                sb.AppendLine($"<h2>✅ Zone Delegation Ended - Responsibility Restored</h2>");
                sb.AppendLine($"<p>Dear {recipient.FullName},</p>");
                sb.AppendLine($"<p>Your zone delegation has {endReason} and your full responsibility has been restored:</p>");
            }
            else
            {
                sb.AppendLine($"<h2>🎯 Zone Delegation Completed - Thank You</h2>");
                sb.AppendLine($"<p>Dear {recipient.FullName},</p>");
                sb.AppendLine($"<p>Your temporary zone assignment has {endReason}. Thank you for your service:</p>");
            }

            sb.AppendLine($"<div style='background-color: #e8f5e8; padding: 20px; border-left: 4px solid #4caf50; margin: 20px 0; border-radius: 5px;'>");
            sb.AppendLine($"<h3 style='color: #2e7d32; margin-top: 0;'>📍 Delegation Summary</h3>");
            sb.AppendLine($"<p><strong>Zone:</strong> {delegation.Zone.Name}</p>");
            sb.AppendLine($"<p><strong>Period:</strong> {delegation.StartDate:MM/dd/yyyy} to {delegation.EndDate:MM/dd/yyyy}</p>");
            sb.AppendLine($"<p><strong>Duration:</strong> {(delegation.EndDate - delegation.StartDate).Days} days</p>");
            
            if (!string.IsNullOrEmpty(delegation.Reason))
            {
                sb.AppendLine($"<p><strong>Original Reason:</strong> {delegation.Reason}</p>");
            }
            
            sb.AppendLine($"</div>");

            if (isDelegatingUser)
            {
                sb.AppendLine($"<p><strong>Your zone responsibility is now fully restored.</strong> You will continue to receive all notifications and manage all HSE activities for this zone.</p>");
            }
            else
            {
                sb.AppendLine($"<p><strong>Thank you for your dedicated service</strong> in managing this zone temporarily. You will no longer receive notifications for this zone unless you have your own assigned zones.</p>");
            }

            sb.AppendLine($"<div style='text-align: center; margin: 30px 0;'>");
            var frontendUrl = GetFrontendUrl();
            sb.AppendLine($"<a href='{frontendUrl}/hse-dashboard' style='background-color: #4caf50; color: white; padding: 12px 24px; text-decoration: none; border-radius: 5px; display: inline-block;'>View HSE Dashboard</a>");
            sb.AppendLine($"</div>");

            sb.AppendLine($"<p>Best regards,<br><strong>HSE Management System</strong></p>");

            return sb.ToString();
        }

        /// <summary>
        /// Get the frontend URL automatically detecting local IP or using configured value
        /// </summary>
        private string GetFrontendUrl()
        {
            try
            {
                // First try to get from configuration
                var configuredUrl = _configuration["ApplicationSettings:FrontendBaseUrl"];
                if (!string.IsNullOrEmpty(configuredUrl))
                {
                    return configuredUrl;
                }

                // Auto-detect local IP address
                var localIP = GetLocalIPAddress();
                if (!string.IsNullOrEmpty(localIP))
                {
                    return $"http://{localIP}:4200";
                }

                // Fallback to localhost
                return "http://localhost:4200";
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to determine frontend URL, using localhost fallback");
                return "http://localhost:4200";
            }
        }

        /// <summary>
        /// Automatically detect the local network IP address
        /// </summary>
        private string GetLocalIPAddress()
        {
            try
            {
                using (var socket = new System.Net.Sockets.Socket(System.Net.Sockets.AddressFamily.InterNetwork, System.Net.Sockets.SocketType.Dgram, 0))
                {
                    socket.Connect("8.8.8.8", 65530);
                    var endPoint = socket.LocalEndPoint as System.Net.IPEndPoint;
                    return endPoint?.Address.ToString() ?? string.Empty;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to auto-detect local IP address");
                return string.Empty;
            }
        }
    }
}