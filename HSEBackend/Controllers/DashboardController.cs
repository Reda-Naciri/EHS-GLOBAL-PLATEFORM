using HSEBackend.Data;
using HSEBackend.DTOs;
using HSEBackend.Models;
using HSEBackend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HSEBackend.Controllers
{
    [ApiController]
    [Route("api/dashboard")]
    [Authorize(Roles = "HSE,Admin")]
    public class DashboardController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger<DashboardController> _logger;
        private readonly IHSEAccessControlService _hseAccessControl;

        public DashboardController(
            AppDbContext context,
            UserManager<ApplicationUser> userManager,
            ILogger<DashboardController> logger,
            IHSEAccessControlService hseAccessControl)
        {
            _context = context;
            _userManager = userManager;
            _logger = logger;
            _hseAccessControl = hseAccessControl;
        }

        /// <summary>
        /// Get dashboard statistics
        /// </summary>
        [HttpGet("stats")]
        public async Task<IActionResult> GetDashboardStats([FromQuery] string? zone = null)
        {
            try
            {
                var userId = User.FindFirst("UserId")?.Value;
                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized(new { message = "User ID not found in token" });
                }

                var now = DateTime.UtcNow;
                var thisMonth = new DateTime(now.Year, now.Month, 1);
                var lastMonth = thisMonth.AddMonths(-1);

                // Get all reports - SIMPLE approach
                var allReports = await _context.Reports
                    .Include(r => r.AssignedHSE)
                    .ToListAsync();

                // Apply basic filtering by zone if specified
                if (!string.IsNullOrEmpty(zone))
                {
                    allReports = allReports.Where(r => r.Zone == zone).ToList();
                }

                // For now, Admin sees all, HSE sees all (we'll fix access control later)
                var visibleReports = allReports;

                // One-time fix: Update any remaining "Open" reports to "Opened" (not "Unopened")
                var oldOpenReports = visibleReports.Where(r => r.Status == "Open").ToList();
                if (oldOpenReports.Any())
                {
                    foreach (var report in oldOpenReports)
                    {
                        report.Status = "Opened";
                        report.UpdatedAt = DateTime.UtcNow;
                    }
                    await _context.SaveChangesAsync();
                    _logger.LogInformation($"Auto-migrated {oldOpenReports.Count} reports from 'Open' to 'Opened'");
                }

                // Calculate SIMPLE statistics 
                var totalReports = visibleReports.Count;
                var openReports = visibleReports.Count(r => r.Status == "Unopened");
                var inProgressReports = visibleReports.Count(r => r.Status == "Opened");
                var closedReports = visibleReports.Count(r => r.Status == "Closed");
                var reportsThisMonth = visibleReports.Count(r => r.CreatedAt >= thisMonth);
                var reportsLastMonth = visibleReports.Count(r => r.CreatedAt >= lastMonth && r.CreatedAt < thisMonth);

                var monthlyGrowthRate = reportsLastMonth > 0 
                    ? ((double)(reportsThisMonth - reportsLastMonth) / reportsLastMonth) * 100 
                    : 0;

                var reportsByType = visibleReports
                    .GroupBy(r => r.Type)
                    .ToDictionary(g => g.Key, g => g.Count());

                var reportsByZone = visibleReports
                    .GroupBy(r => r.Zone)
                    .ToDictionary(g => g.Key, g => g.Count());

                var reportsByStatus = visibleReports
                    .GroupBy(r => r.Status)
                    .ToDictionary(g => g.Key, g => g.Count());

                // SIMPLE incident count
                var totalIncidents = visibleReports.Count(r => r.Type == "Incident-Management");
                var openIncidents = visibleReports.Count(r => r.Type == "Incident-Management" && r.Status == "Unopened");
                var inProgressIncidents = visibleReports.Count(r => r.Type == "Incident-Management" && r.Status == "Opened");
                var closedIncidents = visibleReports.Count(r => r.Type == "Incident-Management" && r.Status == "Closed");

                // Action Statistics
                var totalActions = await _context.Actions.CountAsync();
                var completedActions = await _context.Actions.CountAsync(a => a.Status == "Completed");
                var inProgressActions = await _context.Actions.CountAsync(a => a.Status == "In Progress");
                var notStartedActions = await _context.Actions.CountAsync(a => a.Status == "Not Started");
                var overdueActions = await _context.Actions.CountAsync(a => a.DueDate < now && a.Status != "Completed");

                var completionRate = totalActions > 0 ? (double)completedActions / totalActions * 100 : 0;

                var actionsByHierarchy = await _context.Actions
                    .GroupBy(a => a.Hierarchy)
                    .ToDictionaryAsync(g => g.Key, g => g.Count());

                var actionsByStatus = await _context.Actions
                    .GroupBy(a => a.Status)
                    .ToDictionaryAsync(g => g.Key, g => g.Count());

                // User Statistics
                var totalUsers = await _userManager.Users.CountAsync();
                var pendingUsers = await _context.PendingUsers.CountAsync();

                var usersByRole = new Dictionary<string, int>();
                var roles = new[] { "Admin", "HSE", "Profil" };
                foreach (var role in roles)
                {
                    var usersInRole = await _userManager.GetUsersInRoleAsync(role);
                    usersByRole[role] = usersInRole.Count;
                }

                var usersByDepartment = await _userManager.Users
                    .Where(u => !string.IsNullOrEmpty(u.Department))
                    .GroupBy(u => u.Department)
                    .ToDictionaryAsync(g => g.Key, g => g.Count());

                // Trend Data
                var reportTrends = await GetReportTrends();
                var actionTrends = await GetActionTrends();
                var recentActivity = await GetRecentActivity();

                var dashboardStats = new DashboardStatsDto
                {
                    Reports = new ReportStats
                    {
                        TotalReports = totalReports,
                        OpenReports = openReports,
                        InProgressReports = inProgressReports,
                        ClosedReports = closedReports,
                        ReportsThisMonth = reportsThisMonth,
                        ReportsLastMonth = reportsLastMonth,
                        MonthlyGrowthRate = monthlyGrowthRate,
                        ReportsByType = reportsByType,
                        ReportsByZone = reportsByZone,
                        ReportsByStatus = reportsByStatus
                    },
                    Incidents = new IncidentStats
                    {
                        TotalIncidents = totalIncidents,
                        OpenIncidents = openIncidents,
                        InProgressIncidents = inProgressIncidents,
                        ClosedIncidents = closedIncidents
                    },
                    Actions = new ActionStats
                    {
                        TotalActions = totalActions,
                        CompletedActions = completedActions,
                        InProgressActions = inProgressActions,
                        NotStartedActions = notStartedActions,
                        OverdueActions = overdueActions,
                        CompletionRate = completionRate,
                        ActionsByHierarchy = actionsByHierarchy,
                        ActionsByStatus = actionsByStatus
                    },
                    Users = new UserStats
                    {
                        TotalUsers = totalUsers,
                        ActiveUsers = totalUsers - pendingUsers,
                        PendingUsers = pendingUsers,
                        UsersByRole = usersByRole,
                        UsersByDepartment = usersByDepartment
                    },
                    Trends = new TrendData
                    {
                        ReportTrends = reportTrends,
                        ActionTrends = actionTrends,
                        RecentActivity = recentActivity
                    }
                };

                return Ok(dashboardStats);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting dashboard stats");
                return StatusCode(500, new { message = "Error retrieving dashboard statistics" });
            }
        }

        /// <summary>
        /// Get chart data for specific chart type
        /// </summary>
        [HttpGet("charts/{type}")]
        public async Task<IActionResult> GetChartData(string type)
        {
            try
            {
                var chartData = new ChartDataDto { Type = type };

                switch (type.ToLower())
                {
                    case "reports-by-type":
                        chartData.Title = "Reports by Type";
                        chartData.Data = await _context.Reports
                            .GroupBy(r => r.Type)
                            .ToDictionaryAsync(g => g.Key, g => (object)g.Count());
                        break;

                    case "reports-by-status":
                        chartData.Title = "Reports by Status";
                        chartData.Data = await _context.Reports
                            .GroupBy(r => r.Status)
                            .ToDictionaryAsync(g => g.Key, g => (object)g.Count());
                        break;

                    case "actions-by-hierarchy":
                        chartData.Title = "Actions by Hierarchy";
                        chartData.Data = await _context.Actions
                            .GroupBy(a => a.Hierarchy)
                            .ToDictionaryAsync(g => g.Key, g => (object)g.Count());
                        break;

                    case "monthly-trends":
                        chartData.Title = "Monthly Trends";
                        var trends = await GetReportTrends();
                        chartData.Data = trends.ToDictionary(t => t.Month, t => (object)t.Count);
                        break;

                    default:
                        return BadRequest(new { message = "Invalid chart type" });
                }

                return Ok(chartData);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting chart data for type: {Type}", type);
                return StatusCode(500, new { message = "Error retrieving chart data" });
            }
        }

        /// <summary>
        /// Get recent activity feed
        /// </summary>
        [HttpGet("recent-activity")]
        public async Task<IActionResult> GetRecentActivity([FromQuery] int limit = 20)
        {
            try
            {
                var activities = new List<RecentActivityDto>();

                // Recent Reports
                var recentReports = await _context.Reports
                    .OrderByDescending(r => r.CreatedAt)
                    .Take(limit / 2)
                    .ToListAsync();

                foreach (var report in recentReports)
                {
                    activities.Add(new RecentActivityDto
                    {
                        Type = "report",
                        Title = report.Title,
                        Description = $"New {report.Type} report submitted",
                        UserName = report.ReporterCompanyId,
                        Timestamp = report.CreatedAt,
                        Status = report.Status,
                        RelatedId = report.Id
                    });
                }

                // Recent Actions
                var recentActions = await _context.Actions
                    .Include(a => a.CreatedBy)
                    .OrderByDescending(a => a.CreatedAt)
                    .Take(limit / 2)
                    .ToListAsync();

                foreach (var action in recentActions)
                {
                    activities.Add(new RecentActivityDto
                    {
                        Type = "action",
                        Title = action.Title,
                        Description = $"New action created: {action.Hierarchy}",
                        UserName = action.CreatedBy?.UserName ?? "Unknown",
                        Timestamp = action.CreatedAt,
                        Status = action.Status,
                        RelatedId = action.Id
                    });
                }

                // Sort by timestamp
                activities = activities.OrderByDescending(a => a.Timestamp).Take(limit).ToList();

                return Ok(activities);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting recent activity");
                return StatusCode(500, new { message = "Error retrieving recent activity" });
            }
        }

        /// <summary>
        /// Get performance metrics
        /// </summary>
        [HttpGet("metrics")]
        public async Task<IActionResult> GetPerformanceMetrics()
        {
            try
            {
                var now = DateTime.UtcNow;
                var lastMonth = now.AddMonths(-1);

                // Calculate metrics
                var totalIncidents = await _context.Reports.CountAsync(r => r.Type == "Incident-Management");
                var closedReports = await _context.Reports.CountAsync(r => r.Status == "Closed");
                var totalReports = await _context.Reports.CountAsync();

                var averageResponseTime = 2.5; // Mock data - would calculate from actual response times
                var averageResolutionTime = 5.8; // Mock data - would calculate from actual resolution times
                var safetyScore = totalReports > 0 ? (double)closedReports / totalReports * 100 : 0;

                var kpis = new List<KPIDto>
                {
                    new KPIDto
                    {
                        Name = "Response Time",
                        Value = averageResponseTime,
                        Unit = "hours",
                        Target = "< 4 hours",
                        Status = averageResponseTime < 4 ? "good" : "warning",
                        PercentageChange = -15.2
                    },
                    new KPIDto
                    {
                        Name = "Resolution Time",
                        Value = averageResolutionTime,
                        Unit = "days",
                        Target = "< 7 days",
                        Status = averageResolutionTime < 7 ? "good" : "warning",
                        PercentageChange = -8.7
                    },
                    new KPIDto
                    {
                        Name = "Safety Score",
                        Value = safetyScore,
                        Unit = "%",
                        Target = "> 85%",
                        Status = safetyScore > 85 ? "good" : safetyScore > 70 ? "warning" : "critical",
                        PercentageChange = 12.3
                    }
                };

                var metrics = new PerformanceMetricsDto
                {
                    AverageResponseTime = averageResponseTime,
                    AverageResolutionTime = averageResolutionTime,
                    TotalIncidents = totalIncidents,
                    PreventedIncidents = totalReports - totalIncidents,
                    SafetyScore = safetyScore,
                    KPIs = kpis
                };

                return Ok(metrics);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting performance metrics");
                return StatusCode(500, new { message = "Error retrieving performance metrics" });
            }
        }

        private async Task<List<MonthlyData>> GetReportTrends()
        {
            var trends = new List<MonthlyData>();
            var now = DateTime.UtcNow;

            for (int i = 5; i >= 0; i--)
            {
                var month = now.AddMonths(-i);
                var monthStart = new DateTime(month.Year, month.Month, 1);
                var monthEnd = monthStart.AddMonths(1);

                var count = await _context.Reports
                    .CountAsync(r => r.CreatedAt >= monthStart && r.CreatedAt < monthEnd);

                trends.Add(new MonthlyData
                {
                    Month = month.ToString("yyyy-MM"),
                    Label = month.ToString("MMM yyyy"),
                    Count = count
                });
            }

            return trends;
        }

        private async Task<List<MonthlyData>> GetActionTrends()
        {
            var trends = new List<MonthlyData>();
            var now = DateTime.UtcNow;

            for (int i = 5; i >= 0; i--)
            {
                var month = now.AddMonths(-i);
                var monthStart = new DateTime(month.Year, month.Month, 1);
                var monthEnd = monthStart.AddMonths(1);

                var count = await _context.Actions
                    .CountAsync(a => a.CreatedAt >= monthStart && a.CreatedAt < monthEnd);

                trends.Add(new MonthlyData
                {
                    Month = month.ToString("yyyy-MM"),
                    Label = month.ToString("MMM yyyy"),
                    Count = count
                });
            }

            return trends;
        }

        private async Task<List<DailyData>> GetRecentActivity()
        {
            var activity = new List<DailyData>();
            var now = DateTime.UtcNow;

            for (int i = 6; i >= 0; i--)
            {
                var date = now.AddDays(-i).Date;
                var nextDay = date.AddDays(1);

                var reports = await _context.Reports
                    .CountAsync(r => r.CreatedAt >= date && r.CreatedAt < nextDay);

                var actions = await _context.Actions
                    .CountAsync(a => a.CreatedAt >= date && a.CreatedAt < nextDay);

                var comments = await _context.Comments
                    .CountAsync(c => c.CreatedAt >= date && c.CreatedAt < nextDay);

                activity.Add(new DailyData
                {
                    Date = date,
                    Reports = reports,
                    Actions = actions,
                    Comments = comments
                });
            }

            return activity;
        }
    }
}