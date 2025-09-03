using HSEBackend.Data;
using HSEBackend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HSEBackend.Controllers
{
    [ApiController]
    [Route("api/maintenance")]
    [Authorize(Roles = "Admin")]
    public class MaintenanceController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly ProgressCalculationService _progressService;
        private readonly ILogger<MaintenanceController> _logger;

        public MaintenanceController(
            AppDbContext context,
            ProgressCalculationService progressService,
            ILogger<MaintenanceController> logger)
        {
            _context = context;
            _progressService = progressService;
            _logger = logger;
        }

        /// <summary>
        /// Update all corrective action statuses based on their sub-actions using the new calculation logic
        /// </summary>
        [HttpPost("update-corrective-action-statuses")]
        public async Task<IActionResult> UpdateCorrectiveActionStatuses()
        {
            try
            {
                _logger.LogInformation("🔄 Starting corrective action status update for all existing records");

                // Get all corrective actions with their sub-actions (excluding aborted ones)
                var correctiveActions = await _context.CorrectiveActions
                    .Include(ca => ca.SubActions)
                    .Where(ca => ca.Status != "Aborted")
                    .ToListAsync();

                var updateCount = 0;
                var totalCount = correctiveActions.Count;

                foreach (var correctiveAction in correctiveActions)
                {
                    var oldStatus = correctiveAction.Status;
                    var newStatus = _progressService.CalculateParentStatus(correctiveAction.SubActions.ToList());

                    if (oldStatus != newStatus)
                    {
                        correctiveAction.Status = newStatus;
                        correctiveAction.IsCompleted = newStatus == "Completed";
                        correctiveAction.UpdatedAt = DateTime.UtcNow;
                        updateCount++;

                        _logger.LogInformation($"📝 Updated CorrectiveAction {correctiveAction.Id} ({correctiveAction.Title}): '{oldStatus}' → '{newStatus}'");
                    }
                    else
                    {
                        _logger.LogDebug($"✅ CorrectiveAction {correctiveAction.Id} already has correct status: '{oldStatus}'");
                    }
                }

                await _context.SaveChangesAsync();

                _logger.LogInformation($"✅ Corrective action status update completed: {updateCount}/{totalCount} records updated");

                return Ok(new
                {
                    message = "Corrective action statuses updated successfully",
                    totalProcessed = totalCount,
                    updated = updateCount,
                    unchanged = totalCount - updateCount
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error updating corrective action statuses");
                return StatusCode(500, new { message = "Error updating corrective action statuses", details = ex.Message });
            }
        }

        /// <summary>
        /// Get current status summary for all corrective actions
        /// </summary>
        [HttpGet("corrective-action-status-summary")]
        public async Task<IActionResult> GetCorrectiveActionStatusSummary()
        {
            try
            {
                var summary = await _context.CorrectiveActions
                    .Include(ca => ca.SubActions)
                    .Where(ca => ca.Status != "Aborted")
                    .Select(ca => new
                    {
                        ca.Id,
                        ca.Title,
                        CurrentStatus = ca.Status,
                        SubActionCounts = new
                        {
                            Total = ca.SubActions.Count,
                            NotStarted = ca.SubActions.Count(sa => sa.Status == "Not Started"),
                            InProgress = ca.SubActions.Count(sa => sa.Status == "In Progress"),
                            Completed = ca.SubActions.Count(sa => sa.Status == "Completed"),
                            Cancelled = ca.SubActions.Count(sa => sa.Status == "Cancelled")
                        },
                        CalculatedStatus = _progressService.CalculateParentStatus(ca.SubActions.ToList()),
                        StatusMismatch = ca.Status != _progressService.CalculateParentStatus(ca.SubActions.ToList())
                    })
                    .ToListAsync();

                var mismatchCount = summary.Count(s => s.StatusMismatch);

                return Ok(new
                {
                    totalCorrectiveActions = summary.Count,
                    statusMismatches = mismatchCount,
                    correctiveActions = summary
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error getting corrective action status summary");
                return StatusCode(500, new { message = "Error getting status summary", details = ex.Message });
            }
        }
    }
}