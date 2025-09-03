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
    [Route("api/actions")]
    [Authorize]
    public class ActionsController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly ILogger<ActionsController> _logger;
        private readonly ProgressCalculationService _progressService;
        private readonly IHSEAccessControlService _hseAccessControl;
        private readonly IEnhancedEmailService _emailService;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly INotificationService _notificationService;

        public ActionsController(
            AppDbContext context, 
            ILogger<ActionsController> logger, 
            ProgressCalculationService progressService, 
            IHSEAccessControlService hseAccessControl,
            IEnhancedEmailService emailService,
            UserManager<ApplicationUser> userManager,
            INotificationService notificationService)
        {
            _context = context;
            _logger = logger;
            _progressService = progressService;
            _hseAccessControl = hseAccessControl;
            _emailService = emailService;
            _userManager = userManager;
            _notificationService = notificationService;
        }

        /// <summary>
        /// Get all actions with optional filtering
        /// </summary>
        [HttpGet]
        [Authorize(Roles = "HSE,Admin")]
        public async Task<IActionResult> GetActions(
            [FromQuery] string? status = null,
            [FromQuery] string? assignedTo = null,
            [FromQuery] string? hierarchy = null,
            [FromQuery] int? reportId = null)
        {
            try
            {
                var query = _context.Actions
                    .Include(a => a.AssignedTo)
                    .Include(a => a.CreatedBy)
                    .Include(a => a.Report)
                    .Include(a => a.SubActions)
                    .AsQueryable();

                if (!string.IsNullOrEmpty(status))
                    query = query.Where(a => a.Status == status);

                if (!string.IsNullOrEmpty(assignedTo))
                    query = query.Where(a => a.AssignedToId == assignedTo);

                if (!string.IsNullOrEmpty(hierarchy))
                    query = query.Where(a => a.Hierarchy == hierarchy);

                if (reportId.HasValue)
                    query = query.Where(a => a.ReportId == reportId.Value);

                var actions = await query
                    .OrderByDescending(a => a.CreatedAt)
                    .ToListAsync();

                var actionDtos = actions.Select(a => new ActionDetailDto
                {
                    Id = a.Id,
                    Title = a.Title,
                    Description = a.Description,
                    DueDate = a.DueDate,
                    Status = a.Status,
                    Hierarchy = a.Hierarchy,
                    AssignedToId = a.AssignedToId,
                    AssignedToName = a.AssignedTo?.FullName,
                    CreatedById = a.CreatedById,
                    CreatedByName = a.CreatedBy?.FullName,
                    CreatedAt = a.CreatedAt,
                    UpdatedAt = a.UpdatedAt,
                    ReportId = a.ReportId,
                    ReportTitle = a.Report?.Title,
                    ReportTrackingNumber = a.Report?.TrackingNumber,
                    Overdue = a.Overdue,
                    SubActions = a.SubActions.Select(sa => new SubActionDetailDto
                    {
                        Id = sa.Id,
                        Title = sa.Title,
                        Description = sa.Description,
                        DueDate = sa.DueDate,
                        Status = sa.Status,
                        AssignedToId = sa.AssignedToId,
                        AssignedToName = sa.AssignedTo?.FullName,
                        CreatedAt = sa.CreatedAt,
                        UpdatedAt = sa.UpdatedAt,
                        Overdue = sa.Overdue
                    }).ToList()
                }).ToList();

                return Ok(actionDtos);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting actions");
                return StatusCode(500, new { message = "Error retrieving actions" });
            }
        }

        /// <summary>
        /// Get action by ID
        /// </summary>
        [HttpGet("{id}")]
        [Authorize(Roles = "HSE,Admin")]
        public async Task<IActionResult> GetAction(int id)
        {
            try
            {
                var action = await _context.Actions
                    .Include(a => a.AssignedTo)
                    .Include(a => a.CreatedBy)
                    .Include(a => a.Report)
                    .Include(a => a.SubActions)
                        .ThenInclude(sa => sa.AssignedTo)
                    .Include(a => a.Attachments)
                    .FirstOrDefaultAsync(a => a.Id == id);

                if (action == null)
                    return NotFound(new { message = "Action not found" });

                var actionDto = new ActionDetailDto
                {
                    Id = action.Id,
                    Title = action.Title,
                    Description = action.Description,
                    DueDate = action.DueDate,
                    Status = action.Status,
                    Hierarchy = action.Hierarchy,
                    AssignedToId = action.AssignedToId,
                    AssignedToName = action.AssignedTo?.FullName,
                    CreatedById = action.CreatedById,
                    CreatedByName = action.CreatedBy?.FullName,
                    CreatedAt = action.CreatedAt,
                    UpdatedAt = action.UpdatedAt,
                    ReportId = action.ReportId,
                    ReportTitle = action.Report?.Title,
                    SubActions = action.SubActions.Select(sa => new SubActionDetailDto
                    {
                        Id = sa.Id,
                        Title = sa.Title,
                        Description = sa.Description,
                        DueDate = sa.DueDate,
                        Status = sa.Status,
                        AssignedToId = sa.AssignedToId,
                        AssignedToName = sa.AssignedTo?.FullName,
                        CreatedAt = sa.CreatedAt,
                        UpdatedAt = sa.UpdatedAt
                    }).ToList(),
                    Attachments = action.Attachments.Select(a => new AttachmentDto
                    {
                        Id = a.Id,
                        FileName = a.FileName,
                        FileSize = a.FileSize,
                        UploadedAt = a.UploadedAt,
                        DownloadUrl = $"/api/actions/{action.Id}/attachments/{a.Id}"
                    }).ToList()
                };

                return Ok(actionDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting action {Id}", id);
                return StatusCode(500, new { message = "Error retrieving action" });
            }
        }

        /// <summary>
        /// Create new action
        /// </summary>
        [HttpPost]
        [Authorize(Roles = "HSE,Admin")]
        public async Task<IActionResult> CreateAction([FromBody] CreateActionDto dto)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(ModelState);

                var action = new Models.Action
                {
                    Title = dto.Title,
                    Description = dto.Description,
                    DueDate = dto.DueDate,
                    Hierarchy = dto.Hierarchy,
                    AssignedToId = dto.AssignedToId,
                    CreatedById = dto.CreatedById,
                    ReportId = dto.ReportId,
                    Status = "Not Started",
                    CreatedAt = DateTime.UtcNow
                };

                _context.Actions.Add(action);
                await _context.SaveChangesAsync();

                // Send notification to HSE user assigned to the report
                if (action.ReportId.HasValue)
                {
                    try
                    {
                        var report = await _context.Reports.FindAsync(action.ReportId.Value);
                        if (report != null && !string.IsNullOrEmpty(report.AssignedHSEId))
                        {
                            var currentUserId = User.FindFirst("UserId")?.Value;
                            if (!string.IsNullOrEmpty(currentUserId))
                            {
                                var currentUser = await _userManager.FindByIdAsync(currentUserId);
                                var isAdmin = currentUser != null && await _userManager.IsInRoleAsync(currentUser, "Admin");
                                
                                if (isAdmin)
                                {
                                    // Use admin-specific notification for admin-created actions
                                    await _notificationService.NotifyHSEOnAdminActionCreatedAsync(action.Id, currentUserId, report.AssignedHSEId);
                                    _logger.LogInformation($"✅ Sent admin action creation notification to HSE user {report.AssignedHSEId} for action {action.Id}");
                                }
                                else
                                {
                                    // Use regular HSE notification for HSE-created actions
                                    await _notificationService.NotifyHSEOnActionAddedAsync(action.Id, currentUserId, report.AssignedHSEId);
                                    _logger.LogInformation($"✅ Sent action added notification to HSE user {report.AssignedHSEId} for action {action.Id}");
                                }
                            }
                        }
                    }
                    catch (Exception notificationEx)
                    {
                        _logger.LogError(notificationEx, $"❌ Failed to send action added notification for action {action.Id}");
                    }
                }

                // Send notification to all admins about new action creation
                try
                {
                    var currentUserId = User.FindFirst("UserId")?.Value;
                    await _notificationService.NotifyAdminOnNewActionCreatedAsync(action.Id, currentUserId ?? "system");
                    _logger.LogInformation($"✅ Sent admin notifications for new action {action.Id}");
                }
                catch (Exception notificationEx)
                {
                    _logger.LogError(notificationEx, $"❌ Failed to send admin notifications for action {action.Id}");
                }

                return CreatedAtAction(nameof(GetAction), new { id = action.Id }, new { id = action.Id, message = "Action created successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating action");
                return StatusCode(500, new { message = "Error creating action" });
            }
        }

        /// <summary>
        /// Update action
        /// </summary>
        [HttpPut("{id}")]
        [Authorize(Roles = "HSE,Admin")]
        public async Task<IActionResult> UpdateAction(int id, [FromBody] UpdateActionDto dto)
        {
            try
            {
                var userId = User.FindFirst("UserId")?.Value;
                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized(new { message = "Invalid user context" });
                }

                var action = await _context.Actions.FindAsync(id);
                if (action == null)
                    return NotFound(new { message = "Action not found" });

                // Check if user can manage this action (assignment-based control)
                if (!await _hseAccessControl.CanManageActionAsync(userId, id))
                {
                    return StatusCode(403, new { message = "You can only manage actions for reports currently assigned to you" });
                }

                if (!string.IsNullOrEmpty(dto.Title))
                    action.Title = dto.Title;

                if (!string.IsNullOrEmpty(dto.Description))
                    action.Description = dto.Description;

                if (dto.DueDate.HasValue)
                    action.DueDate = dto.DueDate.Value;

                if (!string.IsNullOrEmpty(dto.Hierarchy))
                    action.Hierarchy = dto.Hierarchy;

                if (!string.IsNullOrEmpty(dto.AssignedToId))
                    action.AssignedToId = dto.AssignedToId;

                if (!string.IsNullOrEmpty(dto.Status))
                    action.Status = dto.Status;

                action.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                return Ok(new { message = "Action updated successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating action {Id}", id);
                return StatusCode(500, new { message = "Error updating action" });
            }
        }

        /// <summary>
        /// Delete action
        /// </summary>
        [HttpDelete("{id}")]
        [Authorize(Roles = "HSE,Admin")]
        public async Task<IActionResult> DeleteAction(int id)
        {
            try
            {
                var userId = User.FindFirst("UserId")?.Value;
                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized(new { message = "Invalid user context" });
                }

                var action = await _context.Actions.FindAsync(id);
                if (action == null)
                    return NotFound(new { message = "Action not found" });

                // Check if user can manage this action (assignment-based control)
                if (!await _hseAccessControl.CanManageActionAsync(userId, id))
                {
                    return StatusCode(403, new { message = "You can only manage actions for reports currently assigned to you" });
                }

                _context.Actions.Remove(action);
                await _context.SaveChangesAsync();

                return Ok(new { message = "Action deleted successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting action {Id}", id);
                return StatusCode(500, new { message = "Error deleting action" });
            }
        }

        /// <summary>
        /// Update action status
        /// </summary>
        [HttpPut("{id}/status")]
        [AllowAnonymous]
        public async Task<IActionResult> UpdateActionStatus(int id, [FromBody] UpdateActionStatusDto dto)
        {
            try
            {
                var action = await _context.Actions
                    .Include(a => a.SubActions)
                    .Include(a => a.Report)
                    .FirstOrDefaultAsync(a => a.Id == id);
                if (action == null)
                    return NotFound(new { message = "Action not found" });

                var oldStatus = action.Status;
                action.Status = dto.Status;
                action.UpdatedAt = DateTime.UtcNow;

                // If action is being aborted, cancel all its sub-actions
                if (dto.Status == "Aborted")
                {
                    foreach (var subAction in action.SubActions)
                    {
                        if (subAction.Status != "Completed")
                        {
                            subAction.Status = "Canceled";
                            subAction.UpdatedAt = DateTime.UtcNow;
                        }
                    }
                }

                await _context.SaveChangesAsync();

                // Send notification to HSE user about action status change
                if (action.Report != null && !string.IsNullOrEmpty(action.Report.AssignedHSEId) && oldStatus != dto.Status)
                {
                    try
                    {
                        var currentUserId = User.FindFirst("UserId")?.Value;
                        await _notificationService.NotifyHSEOnActionStatusUpdateAsync(id, currentUserId ?? "system", oldStatus, dto.Status);
                        _logger.LogInformation($"✅ Sent action status update notification for action {id}");
                    }
                    catch (Exception notificationEx)
                    {
                        _logger.LogError(notificationEx, $"❌ Failed to send action status update notification for action {id}");
                    }
                }

                return Ok(new { message = "Action status updated successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating action status for {Id}", id);
                return StatusCode(500, new { message = "Error updating action status" });
            }
        }

        /// <summary>
        /// Get sub-actions for an action
        /// </summary>
        [HttpGet("{id}/sub-actions")]
        [Authorize(Roles = "HSE,Admin")]
        public async Task<IActionResult> GetSubActions(int id)
        {
            try
            {
                var subActions = await _context.SubActions
                    .Include(sa => sa.AssignedTo)
                    .Include(sa => sa.CorrectiveAction) // Include corrective action details
                    .Where(sa => sa.ActionId == id)
                    .ToListAsync();

                var subActionDtos = subActions.Select(sa => new SubActionDetailDto
                {
                    Id = sa.Id,
                    Title = sa.Title,
                    Description = sa.Description,
                    DueDate = sa.DueDate,
                    Status = sa.Status,
                    AssignedToId = sa.AssignedToId,
                    AssignedToName = sa.AssignedTo?.FullName,
                    CreatedAt = sa.CreatedAt,
                    UpdatedAt = sa.UpdatedAt,
                    // Include corrective action details if this sub-action belongs to a corrective action
                    CorrectiveActionId = sa.CorrectiveActionId,
                    CorrectiveActionTitle = sa.CorrectiveAction?.Title,
                    CorrectiveActionDescription = sa.CorrectiveAction?.Description,
                    CorrectiveActionDueDate = sa.CorrectiveAction?.DueDate,
                    CorrectiveActionPriority = sa.CorrectiveAction?.Priority,
                    CorrectiveActionHierarchy = sa.CorrectiveAction?.Hierarchy,
                    CorrectiveActionStatus = sa.CorrectiveAction?.Status
                }).ToList();

                return Ok(subActionDtos);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting sub-actions for action {Id}", id);
                return StatusCode(500, new { message = "Error retrieving sub-actions" });
            }
        }

        /// <summary>
        /// Create sub-action
        /// </summary>
        [HttpPost("{id}/sub-actions")]
        [Authorize(Roles = "HSE,Admin")]
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

                var action = await _context.Actions.FindAsync(id);
                if (action == null)
                    return NotFound(new { message = "Action not found" });

                // Check if user can manage this action (assignment-based control)
                if (!await _hseAccessControl.CanManageActionAsync(userId, id))
                {
                    return StatusCode(403, new { message = "You can only create sub-actions for actions on reports currently assigned to you" });
                }

                // Validate sub-action due date doesn't exceed parent action due date
                if (dto.DueDate.HasValue && action.DueDate.HasValue)
                {
                    if (dto.DueDate.Value > action.DueDate.Value)
                    {
                        return BadRequest(new { message = "Sub-action due date cannot exceed the parent action deadline" });
                    }
                }

                var subAction = new SubAction
                {
                    Title = dto.Title,
                    Description = dto.Description,
                    DueDate = dto.DueDate,
                    Status = "Not Started", // Automatically set status to "Not Started"
                    AssignedToId = dto.AssignedToId,
                    ActionId = id,
                    CreatedAt = DateTime.UtcNow
                };

                _context.SubActions.Add(subAction);
                await _context.SaveChangesAsync();

                // Send profile assignment email if assigned to a profile user
                if (!string.IsNullOrEmpty(dto.AssignedToId))
                {
                    try
                    {
                        _logger.LogInformation($"📧 Sending profile assignment email for sub-action {subAction.Id} to user {dto.AssignedToId}");
                        await _emailService.SendProfileAssignmentEmailAsync(dto.AssignedToId, subAction.Id);
                    }
                    catch (Exception emailEx)
                    {
                        _logger.LogError(emailEx, $"❌ Failed to send profile assignment email for sub-action {subAction.Id}");
                        // Don't fail the sub-action creation if email fails
                    }
                }

                // Note: Do NOT update parent action status when creating sub-actions
                // Status is only updated when sub-action status changes (not on creation)

                return CreatedAtAction(nameof(GetSubActions), new { id = id }, new { id = subAction.Id, message = "Sub-action created successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating sub-action for action {Id}", id);
                return StatusCode(500, new { message = "Error creating sub-action" });
            }
        }

        /// <summary>
        /// Abort action with reason tracking
        /// </summary>
        [HttpPost("{id}/abort")]
        [Authorize(Roles = "HSE,Admin")]
        public async Task<IActionResult> AbortAction(int id, [FromBody] AbortActionDto dto)
        {
            try
            {
                var userId = User.FindFirst("UserId")?.Value;
                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized(new { message = "Invalid user context" });
                }

                var action = await _context.Actions
                    .Include(a => a.SubActions)
                    .FirstOrDefaultAsync(a => a.Id == id);
                    
                if (action == null)
                    return NotFound(new { message = "Action not found" });

                // Check if user can abort this action (Admin can abort any action, HSE can only abort if assigned to report)
                if (!await _hseAccessControl.CanAbortActionAsync(userId, id))
                {
                    return StatusCode(403, new { message = "You don't have permission to abort this action" });
                }

                // Set abort details
                action.Status = "Aborted";
                action.AbortedById = userId;
                action.AbortedAt = DateTime.UtcNow;
                action.AbortReason = dto.Reason;
                action.UpdatedAt = DateTime.UtcNow;

                // Check if user is admin for notifications
                var currentUser = await _userManager.FindByIdAsync(userId);
                var isAdmin = currentUser != null && await _userManager.IsInRoleAsync(currentUser, "Admin");
                
                // Cancel all sub-actions and notify if admin is doing the cancellation
                foreach (var subAction in action.SubActions)
                {
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

                await _context.SaveChangesAsync();

                // Send notification to HSE user about action being aborted by admin
                
                if (isAdmin && !string.IsNullOrEmpty(action.AssignedToId))
                {
                    try
                    {
                        await _notificationService.NotifyHSEOnAdminActionAbortedAsync(id, userId);
                        _logger.LogInformation($"✅ Sent admin action abort notification for action {id} to HSE user {action.AssignedToId}");
                    }
                    catch (Exception notificationEx)
                    {
                        _logger.LogError(notificationEx, $"❌ Failed to send admin action abort notification for action {id}");
                    }
                }
                else
                {
                    // Fallback to general status update notification for HSE users
                    var report = await _context.Reports.FindAsync(action.ReportId);
                    if (report != null && !string.IsNullOrEmpty(report.AssignedHSEId))
                    {
                        try
                        {
                            await _notificationService.NotifyHSEOnActionStatusUpdateAsync(id, userId, "Previous Status", "Aborted");
                            _logger.LogInformation($"✅ Sent action abort notification for action {id}");
                        }
                        catch (Exception notificationEx)
                        {
                            _logger.LogError(notificationEx, $"❌ Failed to send action abort notification for action {id}");
                        }
                    }
                }

                return Ok(new { message = "Action aborted successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error aborting action {Id}", id);
                return StatusCode(500, new { message = "Error aborting action" });
            }
        }

        /// <summary>
        /// Update parent Action status based on its sub-actions
        /// </summary>
        private async Task UpdateParentActionStatus(SubAction subAction)
        {
            if (subAction.ActionId == null) return;

            var action = await _context.Actions
                .FirstOrDefaultAsync(a => a.Id == subAction.ActionId);

            if (action == null) return;

            var allSubActions = await _context.SubActions
                .Where(sa => sa.ActionId == subAction.ActionId)
                .ToListAsync();

            if (!allSubActions.Any()) return;

            var newStatus = _progressService.CalculateParentStatus(allSubActions);
            
            if (action.Status != newStatus)
            {
                action.Status = newStatus;
                action.UpdatedAt = DateTime.UtcNow;
                
                await _context.SaveChangesAsync();
                
                _logger.LogInformation($"Action {subAction.ActionId} status updated to '{newStatus}' based on sub-actions");
            }
        }
    }
}