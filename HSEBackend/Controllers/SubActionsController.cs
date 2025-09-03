using HSEBackend.Data;
using HSEBackend.DTOs;
using HSEBackend.Models;
using HSEBackend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace HSEBackend.Controllers
{
    [ApiController]
    [Route("api/sub-actions")]
    [Authorize]
    public class SubActionsController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly ILogger<SubActionsController> _logger;
        private readonly ProgressCalculationService _progressService;
        private readonly INotificationService _notificationService;

        public SubActionsController(AppDbContext context, ILogger<SubActionsController> logger, ProgressCalculationService progressService, INotificationService notificationService)
        {
            _context = context;
            _logger = logger;
            _progressService = progressService;
            _notificationService = notificationService;
        }

        /// <summary>
        /// Update sub-action
        /// </summary>
        [HttpPut("{id}")]
        [Authorize(Roles = "HSE,Admin,Profil")]
        public async Task<IActionResult> UpdateSubAction(int id, [FromBody] UpdateSubActionDto dto)
        {
            try
            {
                var subAction = await _context.SubActions
                    .Include(sa => sa.Action)
                    .Include(sa => sa.CorrectiveAction)
                    .FirstOrDefaultAsync(sa => sa.Id == id);

                if (subAction == null)
                    return NotFound(new { message = "Sub-action not found" });

                // Track changes for notifications
                var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                var userRoles = User.FindAll(ClaimTypes.Role).Select(c => c.Value).ToList();
                var isProfile = userRoles.Contains("Profil");

                // Profile users can only update sub-actions assigned to them
                if (isProfile && subAction.AssignedToId != currentUserId)
                {
                    return Forbid("You can only update sub-actions assigned to you");
                }
                bool assignmentChanged = false;
                string? oldAssignedToId = subAction.AssignedToId;

                // Update sub-action fields
                if (!string.IsNullOrEmpty(dto.Title))
                    subAction.Title = dto.Title;

                if (!string.IsNullOrEmpty(dto.Description))
                    subAction.Description = dto.Description;

                if (dto.DueDate.HasValue)
                {
                    // Validate sub-action due date doesn't exceed parent action due date
                    DateTime? parentDueDate = null;
                    if (subAction.Action?.DueDate.HasValue == true)
                        parentDueDate = subAction.Action.DueDate.Value;
                    else if (subAction.CorrectiveAction != null)
                        parentDueDate = subAction.CorrectiveAction.DueDate;
                    
                    if (parentDueDate.HasValue && dto.DueDate.Value > parentDueDate.Value)
                    {
                        return BadRequest(new { message = "Sub-action due date cannot exceed the parent action deadline" });
                    }
                    
                    subAction.DueDate = dto.DueDate.Value;
                }

                if (!string.IsNullOrEmpty(dto.AssignedToId) && dto.AssignedToId != subAction.AssignedToId)
                {
                    subAction.AssignedToId = dto.AssignedToId;
                    assignmentChanged = true;
                }

                bool statusChanged = false;
                if (!string.IsNullOrEmpty(dto.Status) && dto.Status != subAction.Status)
                {
                    subAction.Status = dto.Status;
                    statusChanged = true;
                }

                subAction.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                // If status changed, update parent action/corrective action status
                if (statusChanged)
                {
                    await UpdateParentActionStatus(subAction);
                    await UpdateParentCorrectiveActionStatus(subAction);
                }

                // Debug logging for notification conditions
                _logger.LogInformation("Sub-action update debug - UserId: {UserId}, AssignedToId: {AssignedToId}, UserRoles: {Roles}, IsProfile: {IsProfile}, AssignmentChanged: {AssignmentChanged}, StatusChanged: {StatusChanged}", 
                    currentUserId, subAction.AssignedToId, string.Join(",", userRoles), isProfile, assignmentChanged, statusChanged);

                // Send notification if a profile user updates their assigned sub-action (either assignment or status change)
                if (!string.IsNullOrEmpty(currentUserId) && 
                    currentUserId == subAction.AssignedToId && 
                    isProfile && 
                    (assignmentChanged || statusChanged))
                {
                    try
                    {
                        await _notificationService.NotifyActionAuthorOnSubActionUpdateAsync(subAction.Id, currentUserId);
                        _logger.LogInformation("✅ Email notification sent for sub-action update by profile user. SubActionId: {SubActionId}, UpdatedBy: {UserId}", subAction.Id, currentUserId);
                    }
                    catch (Exception notificationEx)
                    {
                        _logger.LogError(notificationEx, "❌ Failed to send notification for sub-action update by profile user. SubActionId: {SubActionId}", subAction.Id);
                        // Don't fail the main operation if notification fails
                    }
                }
                else
                {
                    _logger.LogInformation("❌ Notification NOT sent - conditions not met. UserId: {UserId}, AssignedTo: {AssignedTo}, IsProfile: {IsProfile}, Changes: Assignment={AssignmentChanged}, Status={StatusChanged}", 
                        currentUserId, subAction.AssignedToId, isProfile, assignmentChanged, statusChanged);
                }

                return Ok(new { message = "Sub-action updated successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating sub-action {Id}", id);
                return StatusCode(500, new { message = "Error updating sub-action" });
            }
        }

        /// <summary>
        /// Update sub-action status only
        /// </summary>
        [HttpPut("{id}/status")]
        [AllowAnonymous] // Allow both authenticated admins and anonymous profile users
        public async Task<IActionResult> UpdateSubActionStatus(int id, [FromBody] UpdateSubActionStatusDto dto)
        {
            try
            {
                // Debug authentication information
                var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                var userRoles = User.FindAll(ClaimTypes.Role).Select(c => c.Value).ToList();
                var userName = User.FindFirst(ClaimTypes.Name)?.Value;
                var isAuthenticated = User.Identity?.IsAuthenticated ?? false;
                
                // Use provided userId for profile users (from frontend), fallback to authenticated user
                var effectiveUserId = !string.IsNullOrEmpty(dto.UserId) ? dto.UserId : currentUserId;
                var allClaims = User.Claims.Select(c => new { c.Type, c.Value }).ToList();
                
                _logger.LogInformation("🔍 SubAction Status Update - ID: {SubActionId}, User: {UserId} ({UserName}), Auth: {IsAuthenticated}, Roles: [{Roles}]", 
                    id, currentUserId, userName, isAuthenticated, string.Join(", ", userRoles));
                
                _logger.LogInformation("🔍 All User Claims: {Claims}", string.Join("; ", allClaims.Select(c => $"{c.Type}={c.Value}")));
                var subAction = await _context.SubActions
                    .Include(sa => sa.Action)
                    .Include(sa => sa.CorrectiveAction)
                    .FirstOrDefaultAsync(sa => sa.Id == id);

                if (subAction == null)
                    return NotFound(new { message = "Sub-action not found" });

                var oldStatus = subAction.Status;
                bool isProfile = userRoles.Contains("Profil");

                // Profile users can only update sub-actions assigned to them
                if (isProfile && subAction.AssignedToId != currentUserId)
                {
                    return Forbid("You can only update sub-actions assigned to you");
                }
                
                subAction.Status = dto.Status;
                subAction.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                // Update parent action/corrective action status
                await UpdateParentActionStatus(subAction);
                await UpdateParentCorrectiveActionStatus(subAction);

                // Debug logging for status update notification conditions  
                _logger.LogInformation("Sub-action status update debug - UserId: {UserId}, AssignedToId: {AssignedToId}, UserRoles: {Roles}, IsProfile: {IsProfile}, OldStatus: {OldStatus}, NewStatus: {NewStatus}", 
                    currentUserId, subAction.AssignedToId, string.Join(",", userRoles), isProfile, oldStatus, dto.Status);

                // Check if the assigned user is a profile user (starts with "profile-user-" or has CompanyId starting with "TE")
                bool isAssignedToProfileUser = false;
                string? assignedUserId = subAction.AssignedToId;
                
                _logger.LogInformation("🔍 DEBUG: SubAction {SubActionId} - AssignedToId: '{AssignedToId}'", id, assignedUserId);
                
                if (!string.IsNullOrEmpty(assignedUserId))
                {
                    var assignedUser = await _context.Users.FirstOrDefaultAsync(u => u.Id == assignedUserId);
                    _logger.LogInformation("🔍 DEBUG: Found assigned user: {UserExists}, UserId: '{UserId}'", assignedUser != null, assignedUserId);
                    
                    if (assignedUser != null)
                    {
                        _logger.LogInformation("🔍 DEBUG: User details - CompanyId: '{CompanyId}', UserId starts with profile-user: {StartsWithProfile}", 
                            assignedUser.CompanyId, assignedUserId.StartsWith("profile-user-"));
                        
                        // Check if this is a profile user (they typically have company IDs starting with "TE")
                        // Profile users can be identified by having a CompanyId starting with "TE" OR userId starting with "profile-user-"
                        isAssignedToProfileUser = (!string.IsNullOrEmpty(assignedUser.CompanyId) && assignedUser.CompanyId.StartsWith("TE")) ||
                                                  assignedUserId.StartsWith("profile-user-");
                        
                        _logger.LogInformation("🔍 Assigned user check - UserId: {UserId}, CompanyId: {CompanyId}, IsProfileUser: {IsProfileUser}", 
                            assignedUserId, assignedUser.CompanyId, isAssignedToProfileUser);
                    }
                    else
                    {
                        _logger.LogWarning("⚠️ DEBUG: Assigned user '{UserId}' not found in database!", assignedUserId);
                    }
                }
                else
                {
                    _logger.LogInformation("ℹ️ DEBUG: No assigned user for sub-action {SubActionId}", id);
                }

                // Determine who is making the change and send appropriate notifications
                if (oldStatus != dto.Status)
                {
                    bool isAdmin = userRoles.Contains("Admin");
                    bool isHSE = userRoles.Contains("HSE");
                    
                    _logger.LogInformation("🔍 Status change detected - User: {UserId} (Effective: {EffectiveUserId}), Auth: {IsAuth}, Roles: [{Roles}], IsAdmin: {IsAdmin}, OldStatus: {OldStatus}, NewStatus: {NewStatus}", 
                        currentUserId, effectiveUserId, isAuthenticated, string.Join(",", userRoles), isAdmin, oldStatus, dto.Status);
                    
                    _logger.LogInformation("🔍 NOTIFICATION CONDITIONS - IsAssignedToProfile: {IsProfile}, EffectiveUser==Assigned: {UserMatch} ({EffectiveUserId} == {AssignedUserId}), IsAuth: {IsAuth}, IsAdmin: {IsAdmin}", 
                        isAssignedToProfileUser, effectiveUserId == assignedUserId, effectiveUserId, assignedUserId, isAuthenticated, isAdmin);
                    
                    if (isAuthenticated && isAdmin && (dto.Status == "Cancelled" || dto.Status == "Canceled"))
                    {
                        _logger.LogInformation("🎯 Admin cancellation detected - calling NotifyHSEOnAdminSubActionCancelledAsync");
                        // Admin cancelled the sub-action - use admin cancellation notification
                        try
                        {
                            await _notificationService.NotifyHSEOnAdminSubActionCancelledAsync(subAction.Id, currentUserId);
                            _logger.LogInformation("✅ Admin cancellation notification sent. SubActionId: {SubActionId}, CancelledBy: {AdminId}, OldStatus: {OldStatus}", subAction.Id, currentUserId, oldStatus);
                        }
                        catch (Exception notificationEx)
                        {
                            _logger.LogError(notificationEx, "❌ Failed to send admin cancellation notification. SubActionId: {SubActionId}", subAction.Id);
                        }
                    }
                    else if (isAssignedToProfileUser && !string.IsNullOrEmpty(effectiveUserId) && effectiveUserId == assignedUserId)
                    {
                        // Profile user updating their own sub-action - use effective user ID
                        try
                        {
                            await _notificationService.NotifyActionAuthorOnSubActionUpdateAsync(subAction.Id, effectiveUserId);
                            _logger.LogInformation("✅ Email notification sent for sub-action status update by profile user. SubActionId: {SubActionId}, UpdatedBy: {EffectiveUserId}, Auth: {IsAuth}, OldStatus: {OldStatus}, NewStatus: {NewStatus}", 
                                subAction.Id, effectiveUserId, isAuthenticated, oldStatus, dto.Status);
                        }
                        catch (Exception notificationEx)
                        {
                            _logger.LogError(notificationEx, "❌ Failed to send notification for sub-action status update by profile user. SubActionId: {SubActionId}", subAction.Id);
                        }
                    }
                    else
                    {
                        _logger.LogInformation("❌ No notification sent - CurrentUser: {UserId}, EffectiveUser: {EffectiveUserId}, Auth: {IsAuth}, AssignedTo: {AssignedTo}, IsProfile: {IsProfile}, IsAdmin: {IsAdmin}, UserMatch: {UserMatch}, Status: {OldStatus} -> {NewStatus}", 
                            currentUserId, effectiveUserId, isAuthenticated, assignedUserId, isAssignedToProfileUser, isAdmin, effectiveUserId == assignedUserId, oldStatus, dto.Status);
                    }
                }

                _logger.LogInformation($"Sub-action {id} status updated from '{oldStatus}' to '{dto.Status}'");

                return Ok(new { message = "Sub-action status updated successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating sub-action status for {Id}", id);
                return StatusCode(500, new { message = "Error updating sub-action status" });
            }
        }

        /// <summary>
        /// Update parent Action status based on its sub-actions
        /// </summary>
        private async Task UpdateParentActionStatus(SubAction subAction)
        {
            if (subAction.Action == null) return;

            var allSubActions = await _context.SubActions
                .Where(sa => sa.ActionId == subAction.ActionId)
                .ToListAsync();

            if (!allSubActions.Any()) return;

            var newStatus = _progressService.CalculateParentStatus(allSubActions);
            
            if (subAction.Action.Status != newStatus)
            {
                subAction.Action.Status = newStatus;
                subAction.Action.UpdatedAt = DateTime.UtcNow;
                
                await _context.SaveChangesAsync();
                
                _logger.LogInformation($"Action {subAction.ActionId} status updated to '{newStatus}' based on sub-actions");
            }
        }

        /// <summary>
        /// Update parent CorrectiveAction status based on its sub-actions
        /// </summary>
        private async Task UpdateParentCorrectiveActionStatus(SubAction subAction)
        {
            if (subAction.CorrectiveAction == null) return;

            var allSubActions = await _context.SubActions
                .Where(sa => sa.CorrectiveActionId == subAction.CorrectiveActionId)
                .ToListAsync();

            if (!allSubActions.Any()) return;

            var newStatus = _progressService.CalculateParentStatus(allSubActions);
            
            if (subAction.CorrectiveAction.Status != newStatus)
            {
                subAction.CorrectiveAction.Status = newStatus;
                subAction.CorrectiveAction.IsCompleted = newStatus == "Completed";
                subAction.CorrectiveAction.UpdatedAt = DateTime.UtcNow;
                
                await _context.SaveChangesAsync();
                
                _logger.LogInformation($"CorrectiveAction {subAction.CorrectiveActionId} status updated to '{newStatus}' based on sub-actions");
            }
        }

        /// <summary>
        /// Get sub-actions assigned to a specific user
        /// </summary>
        [HttpGet("assigned-to/{userId}")]
        [AllowAnonymous] // Temporarily allow for testing assignments loading
        public async Task<IActionResult> GetSubActionsByAssignedUser(string userId)
        {
            try
            {
                _logger.LogInformation("🔍 GetSubActionsByAssignedUser called with userId: '{UserId}'", userId);
                
                // First, let's check if the user exists
                var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);
                _logger.LogInformation("👤 User found: {UserExists}, Name: {UserName}, CompanyId: {CompanyId}", 
                    user != null, user?.FullName, user?.CompanyId);
                
                // Let's also check all sub-actions in the system for debugging
                var allSubActions = await _context.SubActions
                    .Include(sa => sa.AssignedTo)
                    .ToListAsync();
                
                _logger.LogInformation("📊 Total sub-actions in system: {Count}", allSubActions.Count);
                foreach (var sa in allSubActions)
                {
                    _logger.LogInformation("  SubAction {Id}: '{Title}' assigned to '{AssignedToId}' ({AssignedToName})", 
                        sa.Id, sa.Title, sa.AssignedToId, sa.AssignedTo?.FullName);
                }
                
                // Load sub-actions with all related data, ordered by corrective action creation date (newest first)
                var subActions = await _context.SubActions
                    .Include(sa => sa.AssignedTo)
                    .Include(sa => sa.CorrectiveAction) // Include parent corrective action
                    .Include(sa => sa.Action) // Include placeholder action
                    .Where(sa => sa.AssignedToId == userId)
                    .OrderByDescending(sa => sa.CorrectiveAction != null ? sa.CorrectiveAction.CreatedAt : sa.CreatedAt)
                    .ToListAsync();

                // Explicitly load CreatedByHSE for each corrective action
                foreach (var sa in subActions)
                {
                    if (sa.CorrectiveActionId.HasValue && sa.CorrectiveAction != null)
                    {
                        await _context.Entry(sa.CorrectiveAction)
                            .Reference(ca => ca.CreatedByHSE)
                            .LoadAsync();
                    }
                }

                _logger.LogInformation("✅ Found {Count} sub-actions assigned to user '{UserId}'", subActions.Count, userId);
                
                // Debug corrective action author data
                foreach (var sa in subActions)
                {
                    _logger.LogInformation("📝 SubAction {Id} corrective action details:", sa.Id);
                    _logger.LogInformation("  - CorrectiveActionId: {CAId}", sa.CorrectiveActionId);
                    _logger.LogInformation("  - CorrectiveAction exists: {CAExists}", sa.CorrectiveAction != null);
                    if (sa.CorrectiveAction != null)
                    {
                        _logger.LogInformation("  - CA Title: '{Title}'", sa.CorrectiveAction.Title);
                        _logger.LogInformation("  - CA CreatedAt: '{CreatedAt}'", sa.CorrectiveAction.CreatedAt);
                        _logger.LogInformation("  - CA CreatedByHSEId: '{CreatedByHSEId}'", sa.CorrectiveAction.CreatedByHSEId);
                        _logger.LogInformation("  - CA CreatedByHSE exists: {CreatedByHSEExists}", sa.CorrectiveAction.CreatedByHSE != null);
                        if (sa.CorrectiveAction.CreatedByHSE != null)
                        {
                            _logger.LogInformation("  - CA Author FullName: '{AuthorName}'", sa.CorrectiveAction.CreatedByHSE.FullName);
                            _logger.LogInformation("  - CA Author FirstName: '{FirstName}', LastName: '{LastName}'", 
                                sa.CorrectiveAction.CreatedByHSE.FirstName, sa.CorrectiveAction.CreatedByHSE.LastName);
                        }
                        else
                        {
                            _logger.LogWarning("  - ⚠️ CA CreatedByHSE is NULL! CreatedByHSEId='{Id}' but user not loaded", sa.CorrectiveAction.CreatedByHSEId);
                        }
                    }
                }
                
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
                    Overdue = sa.Overdue,
                    // Include corrective action details
                    CorrectiveActionId = sa.CorrectiveActionId,
                    CorrectiveActionTitle = sa.CorrectiveAction?.Title,
                    CorrectiveActionDescription = sa.CorrectiveAction?.Description,
                    CorrectiveActionDueDate = sa.CorrectiveAction?.DueDate,
                    CorrectiveActionPriority = sa.CorrectiveAction?.Priority,
                    CorrectiveActionHierarchy = sa.CorrectiveAction?.Hierarchy,
                    CorrectiveActionStatus = sa.CorrectiveAction?.Status,
                    CorrectiveActionAuthor = sa.CorrectiveAction?.CreatedByHSE?.FullName,
                    CorrectiveActionCreatedAt = sa.CorrectiveAction?.CreatedAt
                }).ToList();

                return Ok(subActionDtos);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting sub-actions for user {UserId}", userId);
                return StatusCode(500, new { message = "Error retrieving sub-actions" });
            }
        }

        /// <summary>
        /// Debug endpoint to check raw corrective action data
        /// </summary>
        [HttpGet("debug/corrective-actions")]
        [Authorize(Roles = "HSE,Admin")]
        public async Task<IActionResult> DebugCorrectiveActions()
        {
            try
            {
                var correctiveActions = await _context.CorrectiveActions
                    .Include(ca => ca.CreatedByHSE)
                    .Take(5) // Limit to first 5
                    .Select(ca => new
                    {
                        Id = ca.Id,
                        Title = ca.Title,
                        CreatedAt = ca.CreatedAt,
                        CreatedAtUtc = ca.CreatedAt.ToString("yyyy-MM-dd HH:mm:ss UTC"),
                        CreatedAtLocal = ca.CreatedAt.ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss Local"),
                        CreatedByHSEId = ca.CreatedByHSEId,
                        CreatedByName = ca.CreatedByHSE != null ? ca.CreatedByHSE.FullName : "NULL",
                        CreatedByFirstName = ca.CreatedByHSE != null ? ca.CreatedByHSE.FirstName : "NULL",
                        CreatedByLastName = ca.CreatedByHSE != null ? ca.CreatedByHSE.LastName : "NULL"
                    })
                    .ToListAsync();

                return Ok(new { 
                    message = "Debug corrective actions data",
                    serverTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                    serverTimeUtc = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss UTC"),
                    correctiveActions 
                });
            }
            catch (Exception ex)
            {
                return Ok(new { error = ex.Message, stackTrace = ex.StackTrace });
            }
        }

    }

    public class UpdateSubActionStatusDto
    {
        public string Status { get; set; } = "";
        public string? UserId { get; set; } // For profile user identification
    }
}