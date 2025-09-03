using HSEBackend.Data;
using HSEBackend.DTOs;
using HSEBackend.Models;
using HSEBackend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;

namespace HSEBackend.Controllers
{
    [ApiController]
    [Route("api/corrective-actions")]
    [Authorize(Roles = "HSE,Admin")]
    public class CorrectiveActionsController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IHSEAccessControlService _hseAccessControl;
        private readonly ILogger<CorrectiveActionsController> _logger;
        private readonly ProgressCalculationService _progressService;
        private readonly IEnhancedEmailService _emailService;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly INotificationService _notificationService;

        public CorrectiveActionsController(
            AppDbContext context, 
            IHSEAccessControlService hseAccessControl, 
            ILogger<CorrectiveActionsController> logger, 
            ProgressCalculationService progressService,
            IEnhancedEmailService emailService,
            UserManager<ApplicationUser> userManager,
            INotificationService notificationService)
        {
            _context = context;
            _hseAccessControl = hseAccessControl;
            _logger = logger;
            _progressService = progressService;
            _emailService = emailService;
            _userManager = userManager;
            _notificationService = notificationService;
        }

        /// <summary>
        /// Get all corrective actions with filtering
        /// Admin: Can see all corrective actions
        /// Non-Admin: Can only see corrective actions they created
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetCorrectiveActions(
            [FromQuery] string? status = null,
            [FromQuery] string? priority = null,
            [FromQuery] string? assignedTo = null,
            [FromQuery] int? reportId = null)
        {
            try
            {
                var userId = User.FindFirst("UserId")?.Value;
                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized(new { message = "Invalid user context" });
                }

                var query = _context.CorrectiveActions
                    .Include(ca => ca.Report)
                    .Include(ca => ca.SubActions)
                        .ThenInclude(sa => sa.AssignedTo)
                    .Include(ca => ca.Attachments)
                    .Include(ca => ca.CreatedByHSE)
                    .Include(ca => ca.AssignedToProfile)
                    .AsQueryable();

                // Apply access control: Non-admin users can only see their own actions
                if (!await _hseAccessControl.IsAdminAsync(userId))
                {
                    query = query.Where(ca => ca.CreatedByHSEId == userId);
                }

                if (!string.IsNullOrEmpty(status))
                    query = query.Where(ca => ca.Status == status);

                if (!string.IsNullOrEmpty(priority))
                    query = query.Where(ca => ca.Priority == priority);

                if (!string.IsNullOrEmpty(assignedTo))
                    query = query.Where(ca => ca.AssignedToProfileId == assignedTo);

                if (reportId.HasValue)
                    query = query.Where(ca => ca.ReportId == reportId.Value);

                var correctiveActions = await query
                    .OrderByDescending(ca => ca.CreatedAt)
                    .ToListAsync();

                var correctiveActionDtos = correctiveActions.Select(ca => new CorrectiveActionDetailDto
                {
                    Id = ca.Id,
                    Title = ca.Title,
                    Description = ca.Description,
                    Status = ca.Status,
                    DueDate = ca.DueDate,
                    Priority = ca.Priority,
                    Hierarchy = ca.Hierarchy,
                    AssignedTo = ca.AssignedToProfile?.FullName,
                    CreatedByHSEId = ca.CreatedByHSEId,
                    CreatedByName = ca.CreatedByHSE?.FullName,
                    CreatedAt = ca.CreatedAt,
                    UpdatedAt = ca.UpdatedAt,
                    IsCompleted = ca.IsCompleted,
                    Overdue = ca.Overdue,
                    ReportId = ca.ReportId,
                    ReportTitle = ca.Report?.Title,
                    ReportTrackingNumber = ca.Report?.TrackingNumber,
                    SubActions = ca.SubActions.Select(sa => new SubActionDetailDto
                    {
                        Id = sa.Id,
                        Title = sa.Title,
                        Description = sa.Description,
                        Status = sa.Status,
                        DueDate = sa.DueDate,
                        AssignedToId = sa.AssignedToId,
                        AssignedToName = sa.AssignedTo?.FullName,
                        CreatedAt = sa.CreatedAt,
                        UpdatedAt = sa.UpdatedAt,
                        Overdue = sa.Overdue
                    }).ToList(),
                    Attachments = ca.Attachments.Select(a => new AttachmentDto
                    {
                        Id = a.Id,
                        FileName = a.FileName,
                        FileSize = a.FileSize,
                        UploadedAt = a.UploadedAt,
                        DownloadUrl = $"/api/corrective-actions/{ca.Id}/attachments/{a.Id}"
                    }).ToList()
                }).ToList();

                return Ok(correctiveActionDtos);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting corrective actions");
                return StatusCode(500, new { message = "Error retrieving corrective actions" });
            }
        }

        /// <summary>
        /// Get corrective action by ID
        /// </summary>
        [HttpGet("{id}")]
        public async Task<IActionResult> GetCorrectiveAction(int id)
        {
            try
            {
                // HSE and Admin can access all corrective actions for reading
                var correctiveAction = await _context.CorrectiveActions
                    .Include(ca => ca.Report)
                    .Include(ca => ca.SubActions)
                        .ThenInclude(sa => sa.Action)
                    .Include(ca => ca.SubActions)
                        .ThenInclude(sa => sa.CorrectiveAction)
                    .Include(ca => ca.Attachments)
                    .Include(ca => ca.CreatedByHSE)
                    .Include(ca => ca.AbortedBy)
                    .FirstOrDefaultAsync(ca => ca.Id == id);

                if (correctiveAction == null)
                    return NotFound(new { message = "Corrective action not found" });

                var correctiveActionDto = new CorrectiveActionDetailDto
                {
                    Id = correctiveAction.Id,
                    Title = correctiveAction.Title,
                    Description = correctiveAction.Description,
                    Status = correctiveAction.Status,
                    DueDate = correctiveAction.DueDate,
                    Priority = correctiveAction.Priority,
                    Hierarchy = correctiveAction.Hierarchy,
                    AssignedTo = correctiveAction.AssignedToProfile?.FullName,
                    CreatedByHSEId = correctiveAction.CreatedByHSEId,
                    CreatedByName = correctiveAction.CreatedByHSE?.FullName,
                    CreatedAt = correctiveAction.CreatedAt,
                    UpdatedAt = correctiveAction.UpdatedAt,
                    IsCompleted = correctiveAction.IsCompleted,
                    Overdue = correctiveAction.Overdue,
                    ReportId = correctiveAction.ReportId,
                    ReportTitle = correctiveAction.Report?.Title,
                    ReportTrackingNumber = correctiveAction.Report?.TrackingNumber,
                    
                    // Abort tracking fields
                    AbortedById = correctiveAction.AbortedById,
                    AbortedByName = correctiveAction.AbortedBy?.FullName,
                    AbortedAt = correctiveAction.AbortedAt,
                    AbortReason = correctiveAction.AbortReason,
                    SubActions = correctiveAction.SubActions.Select(sa => new SubActionDetailDto
                    {
                        Id = sa.Id,
                        Title = sa.Title,
                        Description = sa.Description,
                        Status = sa.Status,
                        DueDate = sa.DueDate,
                        AssignedToId = sa.AssignedToId,
                        AssignedToName = sa.AssignedTo?.FullName,
                        CreatedAt = sa.CreatedAt,
                        UpdatedAt = sa.UpdatedAt,
                        Overdue = sa.Overdue
                    }).ToList(),
                    Attachments = correctiveAction.Attachments.Select(a => new AttachmentDto
                    {
                        Id = a.Id,
                        FileName = a.FileName,
                        FileSize = a.FileSize,
                        UploadedAt = a.UploadedAt,
                        DownloadUrl = $"/api/corrective-actions/{id}/attachments/{a.Id}"
                    }).ToList()
                };

                return Ok(correctiveActionDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting corrective action {Id}", id);
                return StatusCode(500, new { message = "Error retrieving corrective action" });
            }
        }

        /// <summary>
        /// Create new corrective action
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> CreateCorrectiveAction([FromBody] CreateCorrectiveActionDto dto)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(ModelState);

                var userId = User.FindFirst("UserId")?.Value;
                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized(new { message = "Invalid user context" });
                }

                // Check if user can create corrective actions for this report's zone (only if reportId is provided)
                if (dto.ReportId.HasValue && !await _hseAccessControl.CanCreateCorrectiveActionAsync(userId, dto.ReportId.Value))
                {
                    return StatusCode(403, new { message = "You can only create corrective actions for reports in your assigned responsibility zones" });
                }

                var correctiveAction = new CorrectiveAction
                {
                    Title = dto.Title,
                    Description = dto.Description,
                    DueDate = dto.DueDate,
                    Priority = dto.Priority,
                    Hierarchy = dto.Hierarchy,
                    AssignedToProfileId = null, // Corrective actions are not assigned to specific users initially
                    CreatedByHSEId = dto.CreatedByHSEId ?? userId, // Use provided value or fallback to current user
                    ReportId = dto.ReportId,
                    Status = "Not Started", // Start with "Not Started" - will be updated based on sub-actions
                    CreatedAt = DateTime.UtcNow
                };

                _context.CorrectiveActions.Add(correctiveAction);
                await _context.SaveChangesAsync();

                // Send notification to HSE user assigned to the report
                if (correctiveAction.ReportId.HasValue)
                {
                    try
                    {
                        var report = await _context.Reports.FindAsync(correctiveAction.ReportId.Value);
                        if (report != null && !string.IsNullOrEmpty(report.AssignedHSEId))
                        {
                            await _notificationService.NotifyHSEOnCorrectiveActionAddedAsync(correctiveAction.Id, userId, report.AssignedHSEId);
                            _logger.LogInformation($"✅ Sent corrective action added notification to HSE user {report.AssignedHSEId} for corrective action {correctiveAction.Id}");
                        }
                    }
                    catch (Exception notificationEx)
                    {
                        _logger.LogError(notificationEx, $"❌ Failed to send corrective action added notification for corrective action {correctiveAction.Id}");
                    }
                }

                // Send notification to all admins about new corrective action creation
                try
                {
                    await _notificationService.NotifyAdminOnNewCorrectiveActionCreatedAsync(correctiveAction.Id, userId);
                    _logger.LogInformation($"✅ Sent admin notifications for new corrective action {correctiveAction.Id}");
                }
                catch (Exception notificationEx)
                {
                    _logger.LogError(notificationEx, $"❌ Failed to send admin notifications for corrective action {correctiveAction.Id}");
                }

                return CreatedAtAction(nameof(GetCorrectiveAction), new { id = correctiveAction.Id }, 
                    new { id = correctiveAction.Id, message = "Corrective action created successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating corrective action");
                return StatusCode(500, new { message = "Error creating corrective action" });
            }
        }

        /// <summary>
        /// Update corrective action
        /// </summary>
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateCorrectiveAction(int id, [FromBody] UpdateCorrectiveActionDto dto)
        {
            try
            {
                var userId = User.FindFirst("UserId")?.Value;
                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized(new { message = "Invalid user context" });
                }

                var correctiveAction = await _context.CorrectiveActions
                    .Include(ca => ca.Report)
                    .FirstOrDefaultAsync(ca => ca.Id == id);
                    
                if (correctiveAction == null)
                    return NotFound(new { message = "Corrective action not found" });

                // Check if user can manage this corrective action (assignment-based control)
                if (!await _hseAccessControl.CanManageCorrectiveActionAsync(userId, id))
                {
                    return StatusCode(403, new { message = "You can only manage corrective actions for reports currently assigned to you" });
                }

                if (!string.IsNullOrEmpty(dto.Title))
                    correctiveAction.Title = dto.Title;

                if (!string.IsNullOrEmpty(dto.Description))
                    correctiveAction.Description = dto.Description;

                if (dto.DueDate.HasValue)
                    correctiveAction.DueDate = dto.DueDate.Value;

                if (!string.IsNullOrEmpty(dto.Priority))
                    correctiveAction.Priority = dto.Priority;

                if (!string.IsNullOrEmpty(dto.Hierarchy))
                    correctiveAction.Hierarchy = dto.Hierarchy;

                if (!string.IsNullOrEmpty(dto.AssignedTo))
                    correctiveAction.AssignedToProfileId = dto.AssignedTo;

                if (!string.IsNullOrEmpty(dto.Status))
                    correctiveAction.Status = dto.Status;

                if (dto.IsCompleted.HasValue)
                    correctiveAction.IsCompleted = dto.IsCompleted.Value;

                correctiveAction.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                return Ok(new { message = "Corrective action updated successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating corrective action {Id}", id);
                return StatusCode(500, new { message = "Error updating corrective action" });
            }
        }

        /// <summary>
        /// Delete corrective action
        /// </summary>
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteCorrectiveAction(int id)
        {
            try
            {
                var userId = User.FindFirst("UserId")?.Value;
                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized(new { message = "Invalid user context" });
                }

                var correctiveAction = await _context.CorrectiveActions
                    .Include(ca => ca.Report)
                    .FirstOrDefaultAsync(ca => ca.Id == id);
                    
                if (correctiveAction == null)
                    return NotFound(new { message = "Corrective action not found" });

                // Check if user can manage this corrective action (assignment-based control)
                if (!await _hseAccessControl.CanManageCorrectiveActionAsync(userId, id))
                {
                    return StatusCode(403, new { message = "You can only manage corrective actions for reports currently assigned to you" });
                }

                _context.CorrectiveActions.Remove(correctiveAction);
                await _context.SaveChangesAsync();

                return Ok(new { message = "Corrective action deleted successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting corrective action {Id}", id);
                return StatusCode(500, new { message = "Error deleting corrective action" });
            }
        }

        /// <summary>
        /// Update corrective action status
        /// </summary>
        [HttpPut("{id}/status")]
        public async Task<IActionResult> UpdateCorrectiveActionStatus(int id, [FromBody] UpdateCorrectiveActionStatusDto dto)
        {
            try
            {
                var userId = User.FindFirst("UserId")?.Value;
                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized(new { message = "Invalid user context" });
                }

                var correctiveAction = await _context.CorrectiveActions
                    .Include(ca => ca.Report)
                    .Include(ca => ca.SubActions)
                    .FirstOrDefaultAsync(ca => ca.Id == id);
                    
                if (correctiveAction == null)
                    return NotFound(new { message = "Corrective action not found" });

                // Check if user can manage this corrective action (assignment-based control)
                if (!await _hseAccessControl.CanManageCorrectiveActionAsync(userId, id))
                {
                    return StatusCode(403, new { message = "You can only manage corrective actions for reports currently assigned to you" });
                }

                var oldStatus = correctiveAction.Status;
                correctiveAction.Status = dto.Status;
                correctiveAction.IsCompleted = dto.Status == "Completed";
                correctiveAction.UpdatedAt = DateTime.UtcNow;

                // If corrective action is being aborted, cancel all its sub-actions
                if (dto.Status == "Aborted")
                {
                    foreach (var subAction in correctiveAction.SubActions)
                    {
                        if (subAction.Status != "Completed")
                        {
                            subAction.Status = "Canceled";
                            subAction.UpdatedAt = DateTime.UtcNow;
                        }
                    }
                }

                await _context.SaveChangesAsync();

                // Send notification to HSE user about corrective action status change
                if (correctiveAction.Report != null && !string.IsNullOrEmpty(correctiveAction.Report.AssignedHSEId) && oldStatus != dto.Status)
                {
                    try
                    {
                        await _notificationService.NotifyHSEOnCorrectiveActionStatusUpdateAsync(id, userId, oldStatus, dto.Status);
                        _logger.LogInformation($"✅ Sent corrective action status update notification for corrective action {id}");
                    }
                    catch (Exception notificationEx)
                    {
                        _logger.LogError(notificationEx, $"❌ Failed to send corrective action status update notification for corrective action {id}");
                    }
                }

                return Ok(new { message = "Corrective action status updated successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating corrective action status for {Id}", id);
                return StatusCode(500, new { message = "Error updating corrective action status" });
            }
        }

        /// <summary>
        /// Get sub-actions for a corrective action
        /// </summary>
        [HttpGet("{id}/sub-actions")]
        public async Task<IActionResult> GetSubActions(int id)
        {
            try
            {
                var subActions = await _context.SubActions
                    .Include(sa => sa.AssignedTo)
                    .Where(sa => sa.CorrectiveActionId == id)
                    .ToListAsync();

                var subActionDtos = subActions.Select(sa => new SubActionDetailDto
                {
                    Id = sa.Id,
                    Title = sa.Title,
                    Description = sa.Description,
                    Status = sa.Status,
                    DueDate = sa.DueDate,
                    AssignedToId = sa.AssignedToId,
                    AssignedToName = sa.AssignedTo?.FullName,
                    CreatedAt = sa.CreatedAt,
                    UpdatedAt = sa.UpdatedAt
                }).ToList();

                return Ok(subActionDtos);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting sub-actions for corrective action {Id}", id);
                return StatusCode(500, new { message = "Error retrieving sub-actions" });
            }
        }

        /// <summary>
        /// Create sub-action for corrective action
        /// </summary>
        [HttpPost("{id}/sub-actions")]
        public async Task<IActionResult> CreateSubAction(int id, [FromBody] CreateSubActionDto dto)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(ModelState);

                var userId = User.FindFirst("UserId")?.Value;
                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized(new { message = "Invalid user context" });
                }

                var correctiveAction = await _context.CorrectiveActions
                    .Include(ca => ca.Report)
                    .FirstOrDefaultAsync(ca => ca.Id == id);
                    
                if (correctiveAction == null)
                    return NotFound(new { message = "Corrective action not found" });

                // Check if user can create sub-action for this corrective action (only author can create sub-actions)
                if (!await _hseAccessControl.CanCreateSubActionForCorrectiveActionAsync(userId, id))
                {
                    return StatusCode(403, new { message = "You can only create sub-actions for corrective actions you created" });
                }

                // Validate sub-action due date doesn't exceed parent corrective action due date
                if (dto.DueDate.HasValue)
                {
                    if (dto.DueDate.Value > correctiveAction.DueDate)
                    {
                        return BadRequest(new { message = "Sub-action due date cannot exceed the parent corrective action deadline" });
                    }
                }

                // Create a placeholder Action for this SubAction since SubAction model requires ActionId
                var placeholderAction = new Models.Action
                {
                    Title = $"Placeholder for CorrectiveAction {id} SubAction",
                    Description = "System-generated placeholder action for corrective action sub-actions",
                    Status = "In Progress",
                    Hierarchy = "Administrative Controls",
                    CreatedById = userId,
                    ReportId = correctiveAction.ReportId,
                    CreatedAt = DateTime.UtcNow
                };

                _context.Actions.Add(placeholderAction);
                await _context.SaveChangesAsync(); // Save to get the ID

                var subAction = new SubAction
                {
                    Title = dto.Title,
                    Description = dto.Description,
                    Status = "Not Started", // Automatically set status to "Not Started"
                    DueDate = dto.DueDate,
                    AssignedToId = dto.AssignedToId,
                    CorrectiveActionId = id,
                    ActionId = placeholderAction.Id, // Use the placeholder Action ID
                    CreatedAt = DateTime.UtcNow
                };

                _context.SubActions.Add(subAction);
                await _context.SaveChangesAsync();

                // Send profile assignment email if assigned to a profile user
                if (!string.IsNullOrEmpty(dto.AssignedToId))
                {
                    try
                    {
                        _logger.LogInformation($"📧 Sending profile assignment email for corrective sub-action {subAction.Id} to user {dto.AssignedToId}");
                        await _emailService.SendProfileAssignmentEmailAsync(dto.AssignedToId, subAction.Id);
                    }
                    catch (Exception emailEx)
                    {
                        _logger.LogError(emailEx, $"❌ Failed to send profile assignment email for corrective sub-action {subAction.Id}");
                        // Don't fail the sub-action creation if email fails
                    }
                }

                // Note: Do NOT update parent action status when creating sub-actions
                // Status is only updated when sub-action status changes (not on creation)

                return CreatedAtAction(nameof(GetSubActions), new { id = id }, 
                    new { id = subAction.Id, message = "Sub-action created successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating sub-action for corrective action {Id}", id);
                return StatusCode(500, new { message = "Error creating sub-action" });
            }
        }

        /// <summary>
        /// Get corrective actions by report
        /// </summary>
        [HttpGet("by-report/{reportId}")]
        public async Task<IActionResult> GetCorrectiveActionsByReport(int reportId)
        {
            try
            {
                var correctiveActions = await _context.CorrectiveActions
                    .Include(ca => ca.SubActions)
                        .ThenInclude(sa => sa.Action)
                    .Include(ca => ca.SubActions)
                        .ThenInclude(sa => sa.CorrectiveAction)
                    .Include(ca => ca.CreatedByHSE)
                    .Where(ca => ca.ReportId == reportId)
                    .ToListAsync();

                var correctiveActionDtos = correctiveActions.Select(ca => new CorrectiveActionSummaryDto
                {
                    Id = ca.Id,
                    Title = ca.Title,
                    Description = ca.Description,
                    Status = ca.Status,
                    DueDate = ca.DueDate,
                    Priority = ca.Priority,
                    Hierarchy = ca.Hierarchy,
                    AssignedTo = ca.AssignedToProfile?.FullName,
                    CreatedByHSEId = ca.CreatedByHSEId,
                    CreatedByName = ca.CreatedByHSE?.FullName,
                    SubActionsCount = ca.SubActions.Count(sa => 
                        (sa.Action == null || sa.Action.Status != "Aborted") && 
                        (sa.CorrectiveAction == null || sa.CorrectiveAction.Status != "Aborted")),
                    ProgressPercentage = ca.Status == "Aborted" ? 0 : _progressService.CalculateSubActionsProgressPercentage(ca.SubActions.ToList()),
                    CreatedAt = ca.CreatedAt
                }).ToList();

                return Ok(correctiveActionDtos);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting corrective actions for report {ReportId}", reportId);
                return StatusCode(500, new { message = "Error retrieving corrective actions" });
            }
        }

        /// <summary>
        /// Abort corrective action with reason tracking
        /// </summary>
        [HttpPost("{id}/abort")]
        [Authorize(Roles = "HSE,Admin")]
        public async Task<IActionResult> AbortCorrectiveAction(int id, [FromBody] AbortCorrectiveActionDto dto)
        {
            try
            {
                _logger.LogInformation("🔍 AbortCorrectiveAction: Starting abort for corrective action ID {Id}", id);
                
                var userId = User.FindFirst("UserId")?.Value;
                if (string.IsNullOrEmpty(userId))
                {
                    _logger.LogWarning("❌ AbortCorrectiveAction: No user ID found in token for corrective action {Id}", id);
                    return Unauthorized(new { message = "Invalid user context" });
                }

                _logger.LogInformation("👤 AbortCorrectiveAction: User {UserId} attempting to abort corrective action {Id}", userId, id);

                var correctiveAction = await _context.CorrectiveActions
                    .Include(ca => ca.SubActions)
                    .Include(ca => ca.Report)
                    .FirstOrDefaultAsync(ca => ca.Id == id);
                    
                if (correctiveAction == null)
                {
                    _logger.LogWarning("❌ AbortCorrectiveAction: Corrective action {Id} not found", id);
                    return NotFound(new { message = "Corrective action not found" });
                }

                _logger.LogInformation("✅ AbortCorrectiveAction: Found corrective action {Id}, checking permissions", id);

                // Check if user has permission using HSE access control service
                if (!await _hseAccessControl.CanAbortCorrectiveActionAsync(userId, id))
                {
                    _logger.LogWarning("❌ AbortCorrectiveAction: User {UserId} cannot abort corrective action {Id} - insufficient permissions", userId, id);
                    return StatusCode(403, new { message = "You can only abort corrective actions in reports assigned to you, or you must be an administrator" });
                }

                _logger.LogInformation("✅ AbortCorrectiveAction: User {UserId} has permission to abort corrective action {Id}", userId, id);

                // Set abort details
                correctiveAction.Status = "Aborted";
                correctiveAction.IsCompleted = false;
                correctiveAction.AbortedById = userId;
                correctiveAction.AbortedAt = DateTime.UtcNow;
                correctiveAction.AbortReason = dto.Reason;
                correctiveAction.UpdatedAt = DateTime.UtcNow;
                _logger.LogInformation("🔄 AbortCorrectiveAction: Set corrective action {Id} status to Aborted with reason: {Reason}", id, dto.Reason);

                // Check if user is admin for notifications
                var currentUser = await _userManager.FindByIdAsync(userId);
                var isAdmin = currentUser != null && await _userManager.IsInRoleAsync(currentUser, "Admin");
                
                // Cancel all sub-actions and notify if admin is doing the cancellation
                foreach (var subAction in correctiveAction.SubActions)
                {
                    _logger.LogInformation("🔄 AbortCorrectiveAction: Canceling sub-action {SubActionId} of corrective action {Id}", subAction.Id, id);
                    subAction.Status = "Canceled";
                    subAction.UpdatedAt = DateTime.UtcNow;
                    
                    // Send notification if admin is cancelling sub-action and it has an assigned user
                    if (isAdmin && !string.IsNullOrEmpty(subAction.AssignedToId))
                    {
                        try
                        {
                            await _notificationService.NotifyHSEOnAdminSubActionCancelledAsync(subAction.Id, userId);
                            _logger.LogInformation($"✅ Sent admin sub-action cancel notification for sub-action {subAction.Id}");
                        }
                        catch (Exception notificationEx)
                        {
                            _logger.LogError(notificationEx, $"❌ Failed to send admin sub-action cancel notification for sub-action {subAction.Id}");
                        }
                    }
                }

                _logger.LogInformation("💾 AbortCorrectiveAction: Saving changes to database for corrective action {Id}", id);
                await _context.SaveChangesAsync();

                // Send notification to HSE author about corrective action being aborted
                try
                {
                    await _notificationService.NotifyHSEOnCorrectiveActionAbortedAsync(id, userId, dto.Reason ?? "No reason provided");
                    _logger.LogInformation($"✅ Sent corrective action abort notification for corrective action {id}");
                }
                catch (Exception notificationEx)
                {
                    _logger.LogError(notificationEx, $"❌ Failed to send corrective action abort notification for corrective action {id}");
                }

                _logger.LogInformation("✅ AbortCorrectiveAction: Successfully aborted corrective action {Id}", id);
                return Ok(new { message = "Corrective action aborted successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ AbortCorrectiveAction: Error aborting corrective action {Id}. Exception: {Exception}", id, ex.Message);
                _logger.LogError("❌ AbortCorrectiveAction: Stack trace: {StackTrace}", ex.StackTrace);
                return StatusCode(500, new { message = "Error aborting corrective action", details = ex.Message });
            }
        }

        /// <summary>
        /// Update parent CorrectiveAction status based on its sub-actions
        /// </summary>
        private async Task UpdateParentCorrectiveActionStatus(SubAction subAction)
        {
            if (subAction.CorrectiveActionId == null) return;

            var correctiveAction = await _context.CorrectiveActions
                .FirstOrDefaultAsync(ca => ca.Id == subAction.CorrectiveActionId);

            if (correctiveAction == null) return;

            var allSubActions = await _context.SubActions
                .Where(sa => sa.CorrectiveActionId == subAction.CorrectiveActionId)
                .ToListAsync();

            if (!allSubActions.Any()) return;

            var newStatus = _progressService.CalculateParentStatus(allSubActions);
            
            if (correctiveAction.Status != newStatus)
            {
                correctiveAction.Status = newStatus;
                correctiveAction.IsCompleted = newStatus == "Completed";
                correctiveAction.UpdatedAt = DateTime.UtcNow;
                
                await _context.SaveChangesAsync();
                
                _logger.LogInformation($"CorrectiveAction {subAction.CorrectiveActionId} status updated to '{newStatus}' based on sub-actions");
            }
        }
    }
}