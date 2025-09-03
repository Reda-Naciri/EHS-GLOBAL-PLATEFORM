using HSEBackend.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HSEBackend.Controllers
{
    [ApiController]
    [Route("api/migration")]
    [Authorize(Roles = "Admin")] // Only admins can run migrations
    public class MigrationController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly ILogger<MigrationController> _logger;

        public MigrationController(AppDbContext context, ILogger<MigrationController> logger)
        {
            _context = context;
            _logger = logger;
        }

        /// <summary>
        /// Update report statuses from old naming to new naming
        /// Old: "Open" -> New: "Unopened"
        /// Old: "In Progress" -> New: "Opened" 
        /// Old: "Closed" -> New: "Closed" (unchanged)
        /// </summary>
        [HttpPost("update-report-statuses")]
        [AllowAnonymous] // Allow without auth for easier testing
        public async Task<IActionResult> UpdateReportStatuses()
        {
            try
            {
                _logger.LogInformation("Starting report status migration...");

                // Count reports before update
                var currentCounts = await _context.Reports
                    .GroupBy(r => r.Status)
                    .Select(g => new { Status = g.Key, Count = g.Count() })
                    .ToListAsync();

                _logger.LogInformation("Current status counts: {StatusCounts}", 
                    string.Join(", ", currentCounts.Select(c => $"{c.Status}: {c.Count}")));

                int updatedCount = 0;

                // Update "Open" to "Unopened"
                var openReports = await _context.Reports
                    .Where(r => r.Status == "Open")
                    .ToListAsync();

                foreach (var report in openReports)
                {
                    report.Status = "Unopened";
                    report.UpdatedAt = DateTime.UtcNow;
                    updatedCount++;
                }

                _logger.LogInformation("Updated {Count} reports from 'Open' to 'Unopened'", openReports.Count);

                // Update "In Progress" to "Opened"
                var inProgressReports = await _context.Reports
                    .Where(r => r.Status == "In Progress")
                    .ToListAsync();

                foreach (var report in inProgressReports)
                {
                    report.Status = "Opened";
                    report.UpdatedAt = DateTime.UtcNow;
                    updatedCount++;
                }

                _logger.LogInformation("Updated {Count} reports from 'In Progress' to 'Opened'", inProgressReports.Count);

                // Save all changes
                await _context.SaveChangesAsync();

                // Verify the updates
                var newCounts = await _context.Reports
                    .GroupBy(r => r.Status)
                    .Select(g => new { Status = g.Key, Count = g.Count() })
                    .ToListAsync();

                _logger.LogInformation("New status counts: {StatusCounts}", 
                    string.Join(", ", newCounts.Select(c => $"{c.Status}: {c.Count}")));

                var result = new
                {
                    message = "Report status migration completed successfully",
                    totalUpdated = updatedCount,
                    oldCounts = currentCounts.ToDictionary(c => c.Status, c => c.Count),
                    newCounts = newCounts.ToDictionary(c => c.Status, c => c.Count),
                    changes = new
                    {
                        openToUnopened = openReports.Count,
                        inProgressToOpened = inProgressReports.Count
                    }
                };

                _logger.LogInformation("Report status migration completed successfully. Updated {UpdatedCount} reports", updatedCount);

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during report status migration");
                return StatusCode(500, new { 
                    message = "Error during migration", 
                    error = ex.Message 
                });
            }
        }

        /// <summary>
        /// Quick fix: Update Open reports to Unopened and return counts
        /// </summary>
        [HttpGet("fix-and-debug")]
        [AllowAnonymous]
        public async Task<IActionResult> FixAndDebug()
        {
            try
            {
                var reportsQuery = _context.Reports.AsQueryable();
                
                // Fix any remaining "Open" reports
                var oldOpenReports = await reportsQuery.Where(r => r.Status == "Open").ToListAsync();
                if (oldOpenReports.Any())
                {
                    foreach (var report in oldOpenReports)
                    {
                        report.Status = "Unopened";
                        report.UpdatedAt = DateTime.UtcNow;
                    }
                    await _context.SaveChangesAsync();
                }

                // Return updated counts
                var totalReports = await reportsQuery.CountAsync();
                var openReports = await reportsQuery.CountAsync(r => r.Status == "Unopened");
                var inProgressReports = await reportsQuery.CountAsync(r => r.Status == "Opened");
                var closedReports = await reportsQuery.CountAsync(r => r.Status == "Closed");

                return Ok(new
                {
                    message = "Migration completed and debug data",
                    migratedCount = oldOpenReports.Count,
                    counts = new
                    {
                        totalReports,
                        openReports, // This should now be 18
                        inProgressReports,
                        closedReports
                    }
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        /// <summary>
        /// Debug: Get what dashboard API would return
        /// </summary>
        [HttpGet("debug-dashboard")]
        [AllowAnonymous] // Remove auth requirement for testing
        public async Task<IActionResult> DebugDashboard()
        {
            try
            {
                // Simulate what the dashboard controller does
                var reportsQuery = _context.Reports.AsQueryable();
                
                var totalReports = await reportsQuery.CountAsync();
                var openReports = await reportsQuery.CountAsync(r => r.Status == "Unopened");
                var inProgressReports = await reportsQuery.CountAsync(r => r.Status == "Opened");
                var closedReports = await reportsQuery.CountAsync(r => r.Status == "Closed");

                // Also check for old status names that might still exist
                var oldOpenReports = await reportsQuery.CountAsync(r => r.Status == "Open");
                var oldInProgressReports = await reportsQuery.CountAsync(r => r.Status == "In Progress");

                return Ok(new
                {
                    message = "Debug dashboard data",
                    counts = new
                    {
                        totalReports,
                        openReports, // This should be what dashboard shows as "Unopened Reports"
                        inProgressReports,
                        closedReports,
                        oldOpenReports, // Check if any old "Open" reports still exist
                        oldInProgressReports // Check if any old "In Progress" reports still exist
                    },
                    explanation = "openReports represents 'Unopened' status, inProgressReports represents 'Opened' status"
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        /// <summary>
        /// Get current report status counts for verification
        /// </summary>
        [HttpGet("report-status-counts")]
        public async Task<IActionResult> GetReportStatusCounts()
        {
            try
            {
                var statusCounts = await _context.Reports
                    .GroupBy(r => r.Status)
                    .Select(g => new { Status = g.Key, Count = g.Count() })
                    .OrderBy(x => x.Status)
                    .ToListAsync();

                var total = await _context.Reports.CountAsync();

                return Ok(new
                {
                    totalReports = total,
                    statusBreakdown = statusCounts.ToDictionary(c => c.Status, c => c.Count),
                    statusList = statusCounts
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting report status counts");
                return StatusCode(500, new { 
                    message = "Error retrieving status counts", 
                    error = ex.Message 
                });
            }
        }

        /// <summary>
        /// Check if migration is needed by looking for old status names
        /// </summary>
        [HttpGet("migration-status")]
        public async Task<IActionResult> GetMigrationStatus()
        {
            try
            {
                var hasOldStatuses = await _context.Reports
                    .AnyAsync(r => r.Status == "Open" || r.Status == "In Progress");

                var oldStatusCounts = await _context.Reports
                    .Where(r => r.Status == "Open" || r.Status == "In Progress")
                    .GroupBy(r => r.Status)
                    .Select(g => new { Status = g.Key, Count = g.Count() })
                    .ToListAsync();

                return Ok(new
                {
                    migrationNeeded = hasOldStatuses,
                    oldStatusesFound = oldStatusCounts.ToDictionary(c => c.Status, c => c.Count),
                    message = hasOldStatuses 
                        ? "Migration needed - old status names found" 
                        : "No migration needed - all statuses use new naming"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking migration status");
                return StatusCode(500, new { 
                    message = "Error checking migration status", 
                    error = ex.Message 
                });
            }
        }
    }
}