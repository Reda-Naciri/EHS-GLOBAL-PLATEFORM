using HSEBackend.Data;
using HSEBackend.Models;
using HSEBackend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HSEBackend.Controllers
{
    [ApiController]
    [Route("api/testnotifications")]
    [Authorize(Roles = "Admin")]
    public class TestNotificationsController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger<TestNotificationsController> _logger;

        public TestNotificationsController(
            AppDbContext context,
            UserManager<ApplicationUser> userManager,
            ILogger<TestNotificationsController> logger)
        {
            _context = context;
            _userManager = userManager;
            _logger = logger;
        }

        /// <summary>
        /// Create sample notifications for testing the notification banner
        /// </summary>
        [HttpPost("create-sample-notifications")]
        public async Task<IActionResult> CreateSampleNotifications()
        {
            try
            {
                _logger.LogInformation("📧 Creating sample test notifications...");

                // Get admin and HSE users
                var adminUser = await _userManager.FindByEmailAsync("admin@te.com");
                var hseUser = await _userManager.FindByEmailAsync("hse@te.com");

                if (adminUser == null || hseUser == null)
                {
                    return BadRequest("Required test users not found in database");
                }

                var testNotifications = new List<Notification>();

                // Sample notifications for Admin user
                testNotifications.AddRange(new[]
                {
                    new Notification
                    {
                        Title = "Daily HSE System Update",
                        Message = "Last 24h: 3 reports completed, 2 new actions created, 1 action completed.",
                        Type = "DailyUpdate",
                        UserId = adminUser.Id,
                        IsRead = false,
                        CreatedAt = DateTime.UtcNow.AddMinutes(-30)
                    },
                    new Notification
                    {
                        Title = "New Registration Request",
                        Message = "New user registration request from John Smith (john.smith@te.com) for Production department.",
                        Type = "RegistrationRequest",
                        UserId = adminUser.Id,
                        IsRead = false,
                        CreatedAt = DateTime.UtcNow.AddHours(-2)
                    },
                    new Notification
                    {
                        Title = "Overdue Items Alert",
                        Message = "System has 2 overdue actions and 1 overdue sub-actions requiring attention.",
                        Type = "OverdueAlert",
                        UserId = adminUser.Id,
                        IsRead = false,
                        CreatedAt = DateTime.UtcNow.AddHours(-4)
                    },
                    new Notification
                    {
                        Title = "Action Cancelled by HSE",
                        Message = "HSE user HSE Manager cancelled action 'Safety Equipment Check' in report 'Equipment Maintenance'.",
                        Type = "ActionCancelled",
                        UserId = adminUser.Id,
                        IsRead = true,
                        ReadAt = DateTime.UtcNow.AddMinutes(-10),
                        CreatedAt = DateTime.UtcNow.AddDays(-1)
                    }
                });

                // Sample notifications for HSE user
                testNotifications.AddRange(new[]
                {
                    new Notification
                    {
                        Title = "New Report Submitted",
                        Message = "A new Incident report 'Slip and Fall in Production Area A' has been submitted in Production Area A by Reporter ID: TE001234.",
                        Type = "ReportSubmitted",
                        UserId = hseUser.Id,
                        IsRead = false,
                        CreatedAt = DateTime.UtcNow.AddMinutes(-15)
                    },
                    new Notification
                    {
                        Title = "Report Assigned to You",
                        Message = "Report 'Chemical Spill in Laboratory' has been assigned to you by an administrator.",
                        Type = "ReportAssigned",
                        UserId = hseUser.Id,
                        IsRead = false,
                        CreatedAt = DateTime.UtcNow.AddMinutes(-45)
                    },
                    new Notification
                    {
                        Title = "New Comment on Assigned Report",
                        Message = "John Doe added a comment to report 'Equipment Malfunction' assigned to you.",
                        Type = "CommentAdded",
                        UserId = hseUser.Id,
                        IsRead = true,
                        ReadAt = DateTime.UtcNow.AddMinutes(-5),
                        CreatedAt = DateTime.UtcNow.AddHours(-1)
                    },
                    new Notification
                    {
                        Title = "Action Deadline Approaching",
                        Message = "Action 'Replace Safety Guards' is due in 2 day(s) (08/05/2025).",
                        Type = "DeadlineApproaching",
                        UserId = hseUser.Id,
                        IsRead = false,
                        CreatedAt = DateTime.UtcNow.AddHours(-3)
                    },
                    new Notification
                    {
                        Title = "New Action Added to Your Report",
                        Message = "System Administrator added a new action 'Emergency Response Training' to report 'Fire Safety Inspection' assigned to you.",
                        Type = "ActionAdded",
                        UserId = hseUser.Id,
                        IsRead = false,
                        CreatedAt = DateTime.UtcNow.AddHours(-6)
                    }
                });

                // Clear existing test notifications first
                var existingTestNotifications = await _context.Notifications
                    .Where(n => n.Type.Contains("Test") || n.Message.Contains("test") || n.Message.Contains("sample"))
                    .ToListAsync();
                
                if (existingTestNotifications.Any())
                {
                    _context.Notifications.RemoveRange(existingTestNotifications);
                    await _context.SaveChangesAsync();
                    _logger.LogInformation($"🧹 Cleared {existingTestNotifications.Count} existing test notifications");
                }

                // Add new test notifications
                _context.Notifications.AddRange(testNotifications);
                await _context.SaveChangesAsync();

                _logger.LogInformation($"✅ Successfully created {testNotifications.Count} sample notifications");

                return Ok(new
                {
                    message = $"Successfully created {testNotifications.Count} sample notifications",
                    adminNotifications = testNotifications.Count(n => n.UserId == adminUser.Id),
                    hseNotifications = testNotifications.Count(n => n.UserId == hseUser.Id),
                    unreadCount = testNotifications.Count(n => !n.IsRead),
                    readCount = testNotifications.Count(n => n.IsRead)
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error creating sample notifications");
                return StatusCode(500, new { error = "Failed to create sample notifications", details = ex.Message });
            }
        }

        /// <summary>
        /// Get notification statistics
        /// </summary>
        [HttpGet("stats")]
        public async Task<IActionResult> GetNotificationStats()
        {
            try
            {
                var totalNotifications = await _context.Notifications.CountAsync();
                var unreadNotifications = await _context.Notifications.CountAsync(n => !n.IsRead);
                var adminNotifications = await _context.Notifications
                    .Join(_context.Users, n => n.UserId, u => u.Id, (n, u) => new { n, u })
                    .CountAsync(x => x.u.Email == "admin@te.com");
                var hseNotifications = await _context.Notifications
                    .Join(_context.Users, n => n.UserId, u => u.Id, (n, u) => new { n, u })
                    .CountAsync(x => x.u.Email == "hse@te.com");

                return Ok(new
                {
                    totalNotifications,
                    unreadNotifications,
                    readNotifications = totalNotifications - unreadNotifications,
                    adminNotifications,
                    hseNotifications,
                    recentNotifications = await _context.Notifications
                        .OrderByDescending(n => n.CreatedAt)
                        .Take(5)
                        .Select(n => new { n.Title, n.CreatedAt, n.IsRead })
                        .ToListAsync()
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting notification stats");
                return StatusCode(500, new { error = "Failed to get notification stats" });
            }
        }

        /// <summary>
        /// Clear all notifications (for testing purposes)
        /// </summary>
        [HttpDelete("clear-all-notifications")]
        public async Task<IActionResult> ClearAllNotifications()
        {
            try
            {
                var notifications = await _context.Notifications.ToListAsync();
                _context.Notifications.RemoveRange(notifications);
                await _context.SaveChangesAsync();

                _logger.LogInformation($"🧹 Cleared {notifications.Count} notifications");

                return Ok(new { message = $"Successfully cleared {notifications.Count} notifications" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error clearing notifications");
                return StatusCode(500, new { error = "Failed to clear notifications" });
            }
        }
    }
}