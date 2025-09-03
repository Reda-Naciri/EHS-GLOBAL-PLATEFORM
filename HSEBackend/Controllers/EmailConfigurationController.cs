using HSEBackend.Models;
using HSEBackend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;

namespace HSEBackend.Controllers
{
    [ApiController]
    [Route("api/emailconfiguration")]
    [Authorize(Roles = "Admin")]
    public class EmailConfigurationController : ControllerBase
    {
        private readonly IEnhancedEmailService _emailService;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger<EmailConfigurationController> _logger;
        private readonly EmailSchedulingBackgroundService _emailSchedulingService;

        public EmailConfigurationController(
            IEnhancedEmailService emailService,
            UserManager<ApplicationUser> userManager,
            ILogger<EmailConfigurationController> logger,
            EmailSchedulingBackgroundService emailSchedulingService)
        {
            _emailService = emailService;
            _userManager = userManager;
            _logger = logger;
            _emailSchedulingService = emailSchedulingService;
        }

        /// <summary>
        /// Get current email configuration
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetEmailConfiguration()
        {
            try
            {
                _logger.LogInformation("EmailConfiguration GET endpoint called");
                
                var config = await _emailService.GetEmailConfigurationAsync();
                
                if (config == null)
                {
                    _logger.LogError("Email configuration is null after service call");
                    return StatusCode(500, new { error = "Email configuration not found" });
                }
                
                _logger.LogInformation("📧 Email configuration retrieved: HSE={HSEMinutes}min, Admin={AdminMinutes}min", 
                    config.HSEUpdateIntervalMinutes, config.AdminOverviewIntervalMinutes);
                
                return Ok(new
                {
                    id = config.Id,
                    isEmailingEnabled = config.IsEmailingEnabled,
                    sendProfileAssignmentEmails = config.SendProfileAssignmentEmails,
                    sendHSEUpdateEmails = config.SendHSEUpdateEmails,
                    hseUpdateIntervalMinutes = config.HSEUpdateIntervalMinutes,
                    sendHSEInstantReportEmails = config.SendHSEInstantReportEmails,
                    sendAdminOverviewEmails = config.SendAdminOverviewEmails,
                    adminOverviewIntervalMinutes = config.AdminOverviewIntervalMinutes,
                    superAdminUserIds = config.SuperAdminUserIds,
                    createdAt = config.CreatedAt,
                    updatedAt = config.UpdatedAt,
                    updatedByUser = config.UpdatedByUser != null ? new
                    {
                        id = config.UpdatedByUser.Id,
                        fullName = config.UpdatedByUser.FullName,
                        email = config.UpdatedByUser.Email
                    } : null
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving email configuration: {ErrorMessage}", ex.Message);
                _logger.LogError("Stack trace: {StackTrace}", ex.StackTrace);
                return StatusCode(500, new { error = "Failed to retrieve email configuration", details = ex.Message });
            }
        }

        /// <summary>
        /// Update email configuration
        /// </summary>
        [HttpPut]
        public async Task<IActionResult> UpdateEmailConfiguration([FromBody] UpdateEmailConfigurationDto dto)
        {
            try
            {
                _logger.LogInformation("📧 Received email configuration update: HSE={HSEMinutes}min, Admin={AdminMinutes}min", 
                    dto.HSEUpdateIntervalMinutes, dto.AdminOverviewIntervalMinutes);
                
                var userId = _userManager.GetUserId(User);
                if (userId == null)
                {
                    return Unauthorized("User not found");
                }

                var config = new EmailConfiguration
                {
                    IsEmailingEnabled = dto.IsEmailingEnabled,
                    SendProfileAssignmentEmails = dto.SendProfileAssignmentEmails,
                    SendHSEUpdateEmails = dto.SendHSEUpdateEmails,
                    HSEUpdateIntervalMinutes = dto.HSEUpdateIntervalMinutes,
                    SendHSEInstantReportEmails = dto.SendHSEInstantReportEmails,
                    SendAdminOverviewEmails = dto.SendAdminOverviewEmails,
                    AdminOverviewIntervalMinutes = dto.AdminOverviewIntervalMinutes,
                    SuperAdminUserIds = dto.SuperAdminUserIds
                };

                await _emailService.UpdateEmailConfigurationAsync(config, userId);

                _logger.LogInformation("Email configuration updated by user {UserId}", userId);
                return Ok(new { message = "Email configuration updated successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating email configuration");
                return StatusCode(500, new { error = "Failed to update email configuration" });
            }
        }

        /// <summary>
        /// Get email templates
        /// </summary>
        [HttpGet("templates")]
        public async Task<IActionResult> GetEmailTemplates()
        {
            try
            {
                var templates = await _emailService.GetEmailTemplatesAsync();
                return Ok(templates.Select(t => new
                {
                    id = t.Id,
                    templateName = t.TemplateName,
                    subject = t.Subject,
                    htmlContent = t.HtmlContent,
                    plainTextContent = t.PlainTextContent,
                    isActive = t.IsActive,
                    createdAt = t.CreatedAt,
                    updatedAt = t.UpdatedAt,
                    updatedByUser = t.UpdatedByUser != null ? new
                    {
                        id = t.UpdatedByUser.Id,
                        fullName = t.UpdatedByUser.FullName,
                        email = t.UpdatedByUser.Email
                    } : null
                }));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving email templates");
                return StatusCode(500, new { error = "Failed to retrieve email templates" });
            }
        }

        /// <summary>
        /// Update email template
        /// </summary>
        [HttpPut("templates/{templateName}")]
        public async Task<IActionResult> UpdateEmailTemplate(string templateName, [FromBody] UpdateEmailTemplateDto dto)
        {
            try
            {
                var userId = _userManager.GetUserId(User);
                if (userId == null)
                {
                    return Unauthorized("User not found");
                }

                var template = new EmailTemplate
                {
                    TemplateName = templateName,
                    Subject = dto.Subject,
                    HtmlContent = dto.HtmlContent,
                    PlainTextContent = dto.PlainTextContent,
                    IsActive = dto.IsActive
                };

                await _emailService.UpdateEmailTemplateAsync(template, userId);

                _logger.LogInformation("Email template {TemplateName} updated by user {UserId}", templateName, userId);
                return Ok(new { message = "Email template updated successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating email template {TemplateName}", templateName);
                return StatusCode(500, new { error = "Failed to update email template" });
            }
        }

        /// <summary>
        /// Test HSE update emails (Admin only)
        /// </summary>
        [HttpPost("test/hse-updates")]
        public async Task<IActionResult> TestHSEUpdateEmails()
        {
            try
            {
                await _emailService.SendTestHSEUpdateEmailsAsync();
                return Ok(new { message = "HSE test emails sent successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending test HSE update emails");
                return StatusCode(500, new { error = "Failed to send HSE test emails" });
            }
        }

        /// <summary>
        /// Test admin overview emails (Admin only)
        /// </summary>
        [HttpPost("test/admin-overview")]
        public async Task<IActionResult> TestAdminOverviewEmails()
        {
            try
            {
                await _emailService.SendTestAdminOverviewEmailsAsync();
                return Ok(new { message = "Admin test emails sent successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending test admin overview emails");
                return StatusCode(500, new { error = "Failed to send admin test emails" });
            }
        }

        /// <summary>
        /// Send deadline notifications (Admin only)
        /// </summary>
        [HttpPost("send-deadline-notifications")]
        public async Task<IActionResult> SendDeadlineNotifications()
        {
            try
            {
                await _emailService.SendDeadlineNotificationsAsync();
                return Ok(new { message = "Deadline notifications sent successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending deadline notifications");
                return StatusCode(500, new { error = "Failed to send deadline notifications" });
            }
        }

        /// <summary>
        /// Reset email scheduling timers (Admin only)
        /// </summary>
        [HttpPost("reset-timers")]
        public IActionResult ResetEmailTimers()
        {
            try
            {
                _emailSchedulingService.ResetTimers();
                _emailSchedulingService.LogNextScheduledTimes();
                _logger.LogInformation("Email timers reset successfully");
                return Ok(new { message = "Email timers reset successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error resetting email timers");
                return StatusCode(500, new { error = "Failed to reset email timers" });
            }
        }

        /// <summary>
        /// Get next scheduled email times
        /// </summary>
        [HttpGet("next-scheduled")]
        [AllowAnonymous] // Allow access without authentication for status display
        public async Task<IActionResult> GetNextScheduledEmails()
        {
            try
            {
                var nextEmails = await _emailSchedulingService.GetNextScheduledEmailsAsync();
                return Ok(nextEmails);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting next scheduled emails");
                return StatusCode(500, new { error = "Failed to get next scheduled emails" });
            }
        }
    }

    // DTOs for email configuration
    public class UpdateEmailConfigurationDto
    {
        [Required]
        public bool IsEmailingEnabled { get; set; }

        [Required]
        public bool SendProfileAssignmentEmails { get; set; }

        [Required]
        public bool SendHSEUpdateEmails { get; set; }

        [Required]
        [Range(2, 10080)] // 2 minutes to 1 week (7 * 24 * 60)
        public int HSEUpdateIntervalMinutes { get; set; }

        [Required]
        public bool SendHSEInstantReportEmails { get; set; }

        [Required]
        public bool SendAdminOverviewEmails { get; set; }

        [Required]
        [Range(2, 10080)] // 2 minutes to 1 week (7 * 24 * 60)
        public int AdminOverviewIntervalMinutes { get; set; }

        [StringLength(2000)]
        public string? SuperAdminUserIds { get; set; }
    }

    public class UpdateEmailTemplateDto
    {
        [Required]
        [StringLength(200)]
        public string Subject { get; set; } = "";

        [Required]
        public string HtmlContent { get; set; } = "";

        [Required]
        public string PlainTextContent { get; set; } = "";

        public bool IsActive { get; set; } = true;
    }
}