using HSEBackend.Data;
using HSEBackend.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;

namespace HSEBackend.Services
{
    public class DeadlineNotificationBackgroundService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<DeadlineNotificationBackgroundService> _logger;
        private readonly TimeSpan _checkInterval = TimeSpan.FromHours(6); // Check every 6 hours

        public DeadlineNotificationBackgroundService(IServiceProvider serviceProvider, ILogger<DeadlineNotificationBackgroundService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("⏰ Deadline Notification Background Service started.");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using (var scope = _serviceProvider.CreateScope())
                    {
                        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                        var notificationService = scope.ServiceProvider.GetRequiredService<INotificationService>();
                        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
                        
                        await ProcessDeadlineNotifications(context, notificationService, userManager);
                        await ProcessOverdueNotifications(context, notificationService, userManager);
                        await ProcessCrossCommentNotifications(context, notificationService, userManager);
                        await ProcessAdminActivityNotifications(context, notificationService, userManager);
                    }
                    
                    _logger.LogInformation("⏰ Deadline notification check completed at {Time}", DateTime.UtcNow);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "❌ Error occurred during deadline notification check");
                }

                try
                {
                    await Task.Delay(_checkInterval, stoppingToken);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
            }

            _logger.LogInformation("⏰ Deadline Notification Background Service stopped.");
        }

        private async Task ProcessDeadlineNotifications(AppDbContext context, INotificationService notificationService, UserManager<ApplicationUser> userManager)
        {
            var now = DateTime.UtcNow;
            var threeDaysFromNow = now.AddDays(3);
            var oneDayFromNow = now.AddDays(1);

            try
            {
                // Check Actions approaching deadline
                var actionsApproaching = await context.Actions
                    .Include(a => a.Report)
                    .Include(a => a.CreatedBy)
                    .Include(a => a.AssignedTo)
                    .Where(a => a.DueDate.HasValue && 
                               a.Status != "Completed" && 
                               a.Status != "Aborted" &&
                               ((a.DueDate.Value.Date == threeDaysFromNow.Date) || (a.DueDate.Value.Date == oneDayFromNow.Date)))
                    .ToListAsync();

                foreach (var action in actionsApproaching)
                {
                    var daysUntilDue = (action.DueDate.Value.Date - now.Date).Days;
                    
                    // Notify action creator
                    if (!string.IsNullOrEmpty(action.CreatedById))
                    {
                        await CreateDeadlineNotification(context, action.CreatedById, 
                            $"Action Deadline Approaching", 
                            $"Action '{action.Title}' is due in {daysUntilDue} day(s) on {action.DueDate.Value.ToString("MMM dd, yyyy")}.",
                            action.Id, action.ReportId);
                    }

                    // Notify assigned user
                    if (!string.IsNullOrEmpty(action.AssignedToId) && action.AssignedToId != action.CreatedById)
                    {
                        await CreateDeadlineNotification(context, action.AssignedToId, 
                            $"Action Deadline Approaching", 
                            $"Action '{action.Title}' assigned to you is due in {daysUntilDue} day(s) on {action.DueDate.Value.ToString("MMM dd, yyyy")}.",
                            action.Id, action.ReportId);
                    }

                    // Notify HSE user assigned to the report
                    if (action.Report != null && !string.IsNullOrEmpty(action.Report.AssignedHSEId) && 
                        action.Report.AssignedHSEId != action.CreatedById && action.Report.AssignedHSEId != action.AssignedToId)
                    {
                        await CreateDeadlineNotification(context, action.Report.AssignedHSEId, 
                            $"Action Deadline Approaching", 
                            $"Action '{action.Title}' in your assigned report '{action.Report.Title}' is due in {daysUntilDue} day(s).",
                            action.Id, action.ReportId);
                    }
                }

                // Check SubActions approaching deadline
                var subActionsApproaching = await context.SubActions
                    .Include(sa => sa.Action)
                        .ThenInclude(a => a.Report)
                    .Include(sa => sa.CorrectiveAction)
                        .ThenInclude(ca => ca.Report)
                    .Include(sa => sa.AssignedTo)
                    .Where(sa => sa.DueDate.HasValue && 
                                sa.Status != "Completed" && 
                                sa.Status != "Canceled" &&
                                ((sa.DueDate.Value.Date == threeDaysFromNow.Date) || (sa.DueDate.Value.Date == oneDayFromNow.Date)))
                    .ToListAsync();

                foreach (var subAction in subActionsApproaching)
                {
                    var daysUntilDue = (subAction.DueDate.Value.Date - now.Date).Days;
                    var parentTitle = subAction.CorrectiveAction?.Title ?? subAction.Action?.Title ?? "Unknown";
                    var reportTitle = subAction.CorrectiveAction?.Report?.Title ?? subAction.Action?.Report?.Title ?? "Unknown";
                    var assignedHSEId = subAction.CorrectiveAction?.Report?.AssignedHSEId ?? subAction.Action?.Report?.AssignedHSEId;

                    // Notify assigned user
                    if (!string.IsNullOrEmpty(subAction.AssignedToId))
                    {
                        await CreateDeadlineNotification(context, subAction.AssignedToId, 
                            $"Sub-Action Deadline Approaching", 
                            $"Sub-action '{subAction.Title}' for corrective action '{parentTitle}' is due in {daysUntilDue} day(s) on {subAction.DueDate.Value.ToString("MMM dd, yyyy")}.",
                            subAction.ActionId, subAction.CorrectiveAction?.ReportId ?? subAction.Action?.ReportId);
                    }

                    // Notify HSE user assigned to the report
                    if (!string.IsNullOrEmpty(assignedHSEId) && assignedHSEId != subAction.AssignedToId)
                    {
                        await CreateDeadlineNotification(context, assignedHSEId, 
                            $"Sub-Action Deadline Approaching", 
                            $"Sub-action '{subAction.Title}' in corrective action within your assigned report '{reportTitle}' is due in {daysUntilDue} day(s).",
                            subAction.ActionId, subAction.CorrectiveAction?.ReportId ?? subAction.Action?.ReportId);
                    }
                }

                // Check CorrectiveActions approaching deadline
                var correctiveActionsApproaching = await context.CorrectiveActions
                    .Include(ca => ca.Report)
                    .Include(ca => ca.CreatedByHSE)
                    .Include(ca => ca.AssignedToProfile)
                    .Where(ca => ca.Status != "Completed" && 
                                ca.Status != "Aborted" &&
                                ((ca.DueDate.Date == threeDaysFromNow.Date) || (ca.DueDate.Date == oneDayFromNow.Date)))
                    .ToListAsync();

                foreach (var correctiveAction in correctiveActionsApproaching)
                {
                    var daysUntilDue = (correctiveAction.DueDate.Date - now.Date).Days;
                    
                    // Notify corrective action creator (HSE)
                    if (!string.IsNullOrEmpty(correctiveAction.CreatedByHSEId))
                    {
                        await CreateDeadlineNotification(context, correctiveAction.CreatedByHSEId, 
                            $"Corrective Action Deadline Approaching", 
                            $"Corrective action '{correctiveAction.Title}' is due in {daysUntilDue} day(s) on {correctiveAction.DueDate.ToString("MMM dd, yyyy")}.",
                            null, correctiveAction.ReportId, correctiveAction.Id);
                    }

                    // Notify assigned profile user
                    if (!string.IsNullOrEmpty(correctiveAction.AssignedToProfileId) && correctiveAction.AssignedToProfileId != correctiveAction.CreatedByHSEId)
                    {
                        await CreateDeadlineNotification(context, correctiveAction.AssignedToProfileId, 
                            $"Corrective Action Deadline Approaching", 
                            $"Corrective action '{correctiveAction.Title}' assigned to you is due in {daysUntilDue} day(s) on {correctiveAction.DueDate.ToString("MMM dd, yyyy")}.",
                            null, correctiveAction.ReportId, correctiveAction.Id);
                    }

                    // Notify HSE user assigned to the report
                    if (correctiveAction.Report != null && !string.IsNullOrEmpty(correctiveAction.Report.AssignedHSEId) && 
                        correctiveAction.Report.AssignedHSEId != correctiveAction.CreatedByHSEId)
                    {
                        await CreateDeadlineNotification(context, correctiveAction.Report.AssignedHSEId, 
                            $"Corrective Action Deadline Approaching", 
                            $"Corrective action '{correctiveAction.Title}' in your assigned report '{correctiveAction.Report.Title}' is due in {daysUntilDue} day(s).",
                            null, correctiveAction.ReportId, correctiveAction.Id);
                    }
                }

                _logger.LogInformation($"✅ Processed {actionsApproaching.Count} actions, {subActionsApproaching.Count} sub-actions, {correctiveActionsApproaching.Count} corrective actions for deadline notifications");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error processing deadline notifications");
            }
        }

        private async Task ProcessOverdueNotifications(AppDbContext context, INotificationService notificationService, UserManager<ApplicationUser> userManager)
        {
            var today = DateTime.UtcNow.Date;

            try
            {
                // Check overdue Actions
                var overdueActions = await context.Actions
                    .Include(a => a.Report)
                    .Include(a => a.CreatedBy)
                    .Include(a => a.AssignedTo)
                    .Where(a => a.DueDate.HasValue && 
                               a.DueDate.Value.Date < today && 
                               a.Status != "Completed" && 
                               a.Status != "Aborted")
                    .ToListAsync();

                foreach (var action in overdueActions)
                {
                    var daysOverdue = (today - action.DueDate.Value.Date).Days;
                    
                    // Notify action creator
                    if (!string.IsNullOrEmpty(action.CreatedById))
                    {
                        await CreateOverdueNotification(context, action.CreatedById, 
                            $"Action Overdue", 
                            $"Action '{action.Title}' is {daysOverdue} day(s) overdue (was due {action.DueDate.Value.ToString("MMM dd, yyyy")}).",
                            action.Id, action.ReportId);
                    }

                    // Notify assigned user
                    if (!string.IsNullOrEmpty(action.AssignedToId) && action.AssignedToId != action.CreatedById)
                    {
                        await CreateOverdueNotification(context, action.AssignedToId, 
                            $"Action Overdue", 
                            $"Action '{action.Title}' assigned to you is {daysOverdue} day(s) overdue (was due {action.DueDate.Value.ToString("MMM dd, yyyy")}).",
                            action.Id, action.ReportId);
                    }

                    // Notify HSE user assigned to the report
                    if (action.Report != null && !string.IsNullOrEmpty(action.Report.AssignedHSEId))
                    {
                        await CreateOverdueNotification(context, action.Report.AssignedHSEId, 
                            $"Action Overdue", 
                            $"Action '{action.Title}' in your assigned report '{action.Report.Title}' is {daysOverdue} day(s) overdue.",
                            action.Id, action.ReportId);
                    }
                }

                // Check overdue SubActions
                var overdueSubActions = await context.SubActions
                    .Include(sa => sa.Action)
                        .ThenInclude(a => a.Report)
                    .Include(sa => sa.CorrectiveAction)
                        .ThenInclude(ca => ca.Report)
                    .Include(sa => sa.AssignedTo)
                    .Where(sa => sa.DueDate.HasValue && 
                                sa.DueDate.Value.Date < today && 
                                sa.Status != "Completed" && 
                                sa.Status != "Canceled")
                    .ToListAsync();

                foreach (var subAction in overdueSubActions)
                {
                    var daysOverdue = (today - subAction.DueDate.Value.Date).Days;
                    var parentTitle = subAction.CorrectiveAction?.Title ?? subAction.Action?.Title ?? "Unknown";
                    var assignedHSEId = subAction.CorrectiveAction?.Report?.AssignedHSEId ?? subAction.Action?.Report?.AssignedHSEId;

                    // Notify assigned user
                    if (!string.IsNullOrEmpty(subAction.AssignedToId))
                    {
                        await CreateOverdueNotification(context, subAction.AssignedToId, 
                            $"Sub-Action Overdue", 
                            $"Sub-action '{subAction.Title}' for corrective action '{parentTitle}' is {daysOverdue} day(s) overdue (was due {subAction.DueDate.Value.ToString("MMM dd, yyyy")}).",
                            subAction.ActionId, subAction.CorrectiveAction?.ReportId ?? subAction.Action?.ReportId);
                    }

                    // Notify HSE user assigned to the report
                    if (!string.IsNullOrEmpty(assignedHSEId))
                    {
                        await CreateOverdueNotification(context, assignedHSEId, 
                            $"Sub-Action Overdue", 
                            $"Sub-action '{subAction.Title}' in corrective action '{parentTitle}' is {daysOverdue} day(s) overdue in your assigned report.",
                            subAction.ActionId, subAction.CorrectiveAction?.ReportId ?? subAction.Action?.ReportId);
                    }
                }

                // Use the new overdue notification methods for corrective actions and sub-actions
                try
                {
                    await notificationService.NotifyHSEOnOverdueCorrectiveActionsAsync();
                    _logger.LogInformation("✅ Sent overdue corrective action notifications");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "❌ Error sending overdue corrective action notifications");
                }
                
                try
                {
                    await notificationService.NotifyHSEOnOverdueSubActionsAsync();
                    _logger.LogInformation("✅ Sent overdue sub-action notifications");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "❌ Error sending overdue sub-action notifications");
                }
                
                // Keep original logic for actions (not moved to new notification methods yet)
                // Check overdue CorrectiveActions - keeping for backward compatibility
                var overdueCorrectiveActions = await context.CorrectiveActions
                    .Include(ca => ca.Report)
                    .Include(ca => ca.CreatedByHSE)
                    .Where(ca => ca.DueDate.Date < today && 
                                ca.Status != "Completed" && 
                                ca.Status != "Aborted")
                    .ToListAsync();

                _logger.LogInformation($"Found {overdueCorrectiveActions.Count} overdue corrective actions (handled by new notification service)");

                _logger.LogInformation($"✅ Processed {overdueActions.Count} overdue actions, {overdueSubActions.Count} overdue sub-actions, {overdueCorrectiveActions.Count} overdue corrective actions");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error processing overdue notifications");
            }
        }

        private async Task ProcessCrossCommentNotifications(AppDbContext context, INotificationService notificationService, UserManager<ApplicationUser> userManager)
        {
            try
            {
                // Get recent comments (last 6 hours) to find cross-commenting scenarios
                var recentComments = await context.Comments
                    .Include(c => c.User)
                    .Include(c => c.Report)
                    .Where(c => c.CreatedAt >= DateTime.UtcNow.AddHours(-6) && c.IsInternal)
                    .ToListAsync();

                // Group by report to find reports with multiple commenters
                var reportComments = recentComments.GroupBy(c => c.ReportId).Where(g => g.Count() > 0);

                foreach (var reportGroup in reportComments)
                {
                    var reportId = reportGroup.Key;
                    var comments = reportGroup.ToList();
                    var report = comments.First().Report;

                    if (report == null) continue;

                    // Get all HSE users who have commented on this report (historically)
                    var allCommenters = await context.Comments
                        .Include(c => c.User)
                        .Where(c => c.ReportId == reportId && c.IsInternal)
                        .Select(c => c.User)
                        .Distinct()
                        .ToListAsync();

                    // For each recent comment, notify other HSE users who have commented on this report
                    foreach (var comment in comments.OrderBy(c => c.CreatedAt))
                    {
                        var commenter = comment.User;
                        if (commenter == null) continue;

                        var otherCommenters = allCommenters.Where(u => u.Id != commenter.Id).ToList();

                        foreach (var otherCommenter in otherCommenters)
                        {
                            // Check if we haven't already sent this notification
                            var existingNotification = await context.Notifications
                                .Where(n => n.UserId == otherCommenter.Id && 
                                           n.Type == "CrossComment" && 
                                           n.RelatedReportId == reportId &&
                                           n.CreatedAt >= DateTime.UtcNow.AddHours(-6))
                                .FirstOrDefaultAsync();

                            if (existingNotification == null)
                            {
                                await CreateCrossCommentNotification(context, otherCommenter.Id, 
                                    $"New Comment on Report You Commented", 
                                    $"{commenter.FullName} commented on report '{report.Title}' that you also commented on.",
                                    reportId, commenter.Id);
                            }
                        }
                    }
                }

                _logger.LogInformation($"✅ Processed cross-comment notifications for {reportComments.Count()} reports");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error processing cross-comment notifications");
            }
        }

        private async Task ProcessAdminActivityNotifications(AppDbContext context, INotificationService notificationService, UserManager<ApplicationUser> userManager)
        {
            try
            {
                // Get recent activities (last 6 hours) that admins should be notified about
                var cutoffTime = DateTime.UtcNow.AddHours(-6);

                // Count recent reports
                var recentReportsCount = await context.Reports
                    .Where(r => r.CreatedAt >= cutoffTime)
                    .CountAsync();

                // Count recent actions
                var recentActionsCount = await context.Actions
                    .Where(a => a.CreatedAt >= cutoffTime)
                    .CountAsync();

                // Count recent corrective actions
                var recentCorrectiveActionsCount = await context.CorrectiveActions
                    .Where(ca => ca.CreatedAt >= cutoffTime)
                    .CountAsync();

                // Get admin users
                var adminUsers = await userManager.GetUsersInRoleAsync("Admin");
                adminUsers = adminUsers.Where(u => u.IsActive).ToList();

                if (recentReportsCount > 0 || recentActionsCount > 0 || recentCorrectiveActionsCount > 0)
                {
                    foreach (var admin in adminUsers)
                    {
                        var activities = new List<string>();
                        if (recentReportsCount > 0) activities.Add($"{recentReportsCount} new report(s)");
                        if (recentActionsCount > 0) activities.Add($"{recentActionsCount} new action(s)");
                        if (recentCorrectiveActionsCount > 0) activities.Add($"{recentCorrectiveActionsCount} new corrective action(s)");

                        await CreateAdminActivityNotification(context, admin.Id, 
                            $"Recent HSE System Activity", 
                            $"In the last 6 hours: {string.Join(", ", activities)}.");
                    }
                }

                _logger.LogInformation($"✅ Sent admin activity notifications to {adminUsers.Count} admins for {recentReportsCount + recentActionsCount + recentCorrectiveActionsCount} recent activities");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error processing admin activity notifications");
            }
        }

        private async Task CreateDeadlineNotification(AppDbContext context, string userId, string title, string message, int? actionId, int? reportId, int? correctiveActionId = null)
        {
            var notification = new Notification
            {
                Title = title,
                Message = message,
                Type = "DeadlineApproaching",
                UserId = userId,
                RelatedActionId = actionId,
                RelatedReportId = reportId,
                RelatedCorrectiveActionId = correctiveActionId,
                CreatedAt = DateTime.UtcNow
            };

            context.Notifications.Add(notification);
            await context.SaveChangesAsync();
        }

        private async Task CreateOverdueNotification(AppDbContext context, string userId, string title, string message, int? actionId, int? reportId, int? correctiveActionId = null)
        {
            var notification = new Notification
            {
                Title = title,
                Message = message,
                Type = "OverdueAlert",
                UserId = userId,
                RelatedActionId = actionId,
                RelatedReportId = reportId,
                RelatedCorrectiveActionId = correctiveActionId,
                CreatedAt = DateTime.UtcNow
            };

            context.Notifications.Add(notification);
            await context.SaveChangesAsync();
        }

        private async Task CreateCrossCommentNotification(AppDbContext context, string userId, string title, string message, int reportId, string triggeredByUserId)
        {
            var notification = new Notification
            {
                Title = title,
                Message = message,
                Type = "CrossComment",
                UserId = userId,
                TriggeredByUserId = triggeredByUserId,
                RelatedReportId = reportId,
                CreatedAt = DateTime.UtcNow
            };

            context.Notifications.Add(notification);
            await context.SaveChangesAsync();
        }

        private async Task CreateAdminActivityNotification(AppDbContext context, string userId, string title, string message)
        {
            var notification = new Notification
            {
                Title = title,
                Message = message,
                Type = "AdminActivity",
                UserId = userId,
                CreatedAt = DateTime.UtcNow
            };

            context.Notifications.Add(notification);
            await context.SaveChangesAsync();
        }
    }
}