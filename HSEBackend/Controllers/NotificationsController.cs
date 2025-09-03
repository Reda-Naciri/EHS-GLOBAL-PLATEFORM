using HSEBackend.Models;
using HSEBackend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace HSEBackend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class NotificationsController : ControllerBase
    {
        private readonly INotificationService _notificationService;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger<NotificationsController> _logger;

        public NotificationsController(
            INotificationService notificationService,
            UserManager<ApplicationUser> userManager,
            ILogger<NotificationsController> logger)
        {
            _notificationService = notificationService;
            _userManager = userManager;
            _logger = logger;
        }

        /// <summary>
        /// Get current user's notifications with pagination
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetUserNotifications(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20,
            [FromQuery] bool unreadOnly = false)
        {
            try
            {
                var userId = _userManager.GetUserId(User);
                if (userId == null)
                {
                    return Unauthorized("User not found");
                }

                if (page < 1) page = 1;
                if (pageSize < 1 || pageSize > 100) pageSize = 20;

                var notifications = await _notificationService.GetUserNotificationsAsync(userId, page, pageSize);
                var unreadCount = await _notificationService.GetUnreadNotificationCountAsync(userId);

                // Filter for unread only if requested
                if (unreadOnly)
                {
                    notifications = notifications.Where(n => !n.IsRead).ToList();
                }

                return Ok(new
                {
                    totalCount = notifications.Count,
                    unreadCount = unreadCount,
                    notifications = notifications.Select(n => new
                    {
                        id = n.Id.ToString(), // Convert to string for frontend compatibility
                        title = n.Title,
                        message = n.Message,
                        type = MapNotificationType(n.Type),
                        isRead = n.IsRead,
                        createdAt = n.CreatedAt,
                        readAt = n.ReadAt,
                        priority = "medium", // Default priority
                        relatedEntityId = GetRelatedEntityId(n),
                        relatedEntityType = GetRelatedEntityType(n),
                        actionUrl = GenerateActionUrl(n),
                        triggeredByUser = n.TriggeredByUser != null ? new
                        {
                            id = n.TriggeredByUser.Id,
                            fullName = n.TriggeredByUser.FullName,
                            email = n.TriggeredByUser.Email
                        } : null,
                        relatedReport = n.RelatedReport != null ? new
                        {
                            id = n.RelatedReport.Id,
                            title = n.RelatedReport.Title,
                            trackingNumber = n.RelatedReport.TrackingNumber
                        } : null,
                        relatedAction = n.RelatedAction != null ? new
                        {
                            id = n.RelatedAction.Id,
                            title = n.RelatedAction.Title
                        } : null
                    })
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving notifications for user");
                return StatusCode(500, new { error = "Failed to retrieve notifications" });
            }
        }

        /// <summary>
        /// Get count of unread notifications for current user
        /// </summary>
        [HttpGet("unread-count")]
        public async Task<IActionResult> GetUnreadNotificationCount()
        {
            try
            {
                var userId = _userManager.GetUserId(User);
                if (userId == null)
                {
                    return Unauthorized("User not found");
                }

                var count = await _notificationService.GetUnreadNotificationCountAsync(userId);
                return Ok(new { unreadCount = count });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving unread notification count");
                return StatusCode(500, new { error = "Failed to retrieve unread count" });
            }
        }

        /// <summary>
        /// Mark a specific notification as read
        /// </summary>
        [HttpPost("{id}/mark-read")]
        public async Task<IActionResult> MarkNotificationAsRead(string id)
        {
            try
            {
                var userId = _userManager.GetUserId(User);
                if (userId == null)
                {
                    return Unauthorized("User not found");
                }

                if (!int.TryParse(id, out int notificationId))
                {
                    return BadRequest("Invalid notification ID");
                }

                await _notificationService.MarkNotificationAsReadAsync(notificationId, userId);
                return Ok(new { message = "Notification marked as read" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error marking notification {NotificationId} as read", id);
                return StatusCode(500, new { error = "Failed to mark notification as read" });
            }
        }

        /// <summary>
        /// Mark all notifications as read for current user
        /// </summary>
        [HttpPost("mark-all-read")]
        public async Task<IActionResult> MarkAllNotificationsAsRead()
        {
            try
            {
                var userId = _userManager.GetUserId(User);
                if (userId == null)
                {
                    return Unauthorized("User not found");
                }

                await _notificationService.MarkAllNotificationsAsReadAsync(userId);
                return Ok(new { message = "All notifications marked as read" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error marking all notifications as read");
                return StatusCode(500, new { error = "Failed to mark all notifications as read" });
            }
        }

        /// <summary>
        /// Trigger daily admin updates (Admin only)
        /// </summary>
        [HttpPost("admin/trigger-daily-updates")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> TriggerDailyAdminUpdates()
        {
            try
            {
                await _notificationService.NotifyAdminOnDailyUpdatesAsync();
                return Ok(new { message = "Daily admin updates triggered successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error triggering daily admin updates");
                return StatusCode(500, new { error = "Failed to trigger daily updates" });
            }
        }

        /// <summary>
        /// Trigger overdue items check (Admin only)
        /// </summary>
        [HttpPost("admin/trigger-overdue-check")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> TriggerOverdueItemsCheck()
        {
            try
            {
                await _notificationService.NotifyAdminOnOverdueItemsAsync();
                return Ok(new { message = "Overdue items check triggered successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error triggering overdue items check");
                return StatusCode(500, new { error = "Failed to trigger overdue check" });
            }
        }

        /// <summary>
        /// Trigger deadline approaching notifications (Admin only)
        /// </summary>
        [HttpPost("admin/trigger-deadline-check")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> TriggerDeadlineCheck()
        {
            try
            {
                await _notificationService.NotifyOnDeadlineApproachingAsync();
                return Ok(new { message = "Deadline approaching notifications triggered successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error triggering deadline check");
                return StatusCode(500, new { error = "Failed to trigger deadline check" });
            }
        }

        // Helper methods for notification mapping
        private string MapNotificationType(string backendType)
        {
            return backendType switch
            {
                "ReportSubmitted" => "info",
                "ReportAssigned" => "info",
                "CommentAdded" => "info",
                "ActionStatusChanged" => "warning",
                "SubActionStatusChanged" => "warning",
                "ActionAdded" => "success",
                "SubActionAdded" => "success",
                "ActionAborted" => "error",
                "DeadlineApproaching" => "warning",
                "OverdueAlert" => "error",
                _ => "info"
            };
        }

        private string? GetRelatedEntityId(Notification notification)
        {
            if (notification.RelatedReportId.HasValue)
                return notification.RelatedReportId.Value.ToString();
            if (notification.RelatedActionId.HasValue)
                return notification.RelatedActionId.Value.ToString();
            if (notification.RelatedCorrectiveActionId.HasValue)
                return notification.RelatedCorrectiveActionId.Value.ToString();
            return null;
        }

        private string? GetRelatedEntityType(Notification notification)
        {
            if (notification.RelatedReportId.HasValue) return "report";
            if (notification.RelatedActionId.HasValue) return "action";
            if (notification.RelatedCorrectiveActionId.HasValue) return "correctiveAction";
            return null;
        }

        private string? GenerateActionUrl(Notification notification)
        {
            // Use the stored RedirectUrl if available, otherwise fall back to simple URLs
            if (!string.IsNullOrEmpty(notification.RedirectUrl))
            {
                // Remove the base URL if it exists and return only the path part for frontend
                if (notification.RedirectUrl.StartsWith("http://192.168.0.245:4200"))
                {
                    return notification.RedirectUrl.Replace("http://192.168.0.245:4200", "");
                }
                return notification.RedirectUrl;
            }

            // Fallback to simple URLs if no RedirectUrl is stored
            if (notification.RelatedReportId.HasValue)
                return $"/reports/{notification.RelatedReportId}";
            if (notification.RelatedActionId.HasValue)
                return $"/actions/{notification.RelatedActionId}";
            if (notification.RelatedCorrectiveActionId.HasValue)
                return $"/corrective-actions/{notification.RelatedCorrectiveActionId}";
            return null;
        }
    }
}