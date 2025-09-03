namespace HSEBackend.DTOs
{
    public class DashboardStatsDto
    {
        public ReportStats Reports { get; set; } = new();
        public IncidentStats Incidents { get; set; } = new();
        public ActionStats Actions { get; set; } = new();
        public UserStats Users { get; set; } = new();
        public TrendData Trends { get; set; } = new();
    }

    public class ReportStats
    {
        public int TotalReports { get; set; }
        public int OpenReports { get; set; }
        public int InProgressReports { get; set; }
        public int ClosedReports { get; set; }
        public int ReportsThisMonth { get; set; }
        public int ReportsLastMonth { get; set; }
        public double MonthlyGrowthRate { get; set; }
        public Dictionary<string, int> ReportsByType { get; set; } = new();
        public Dictionary<string, int> ReportsByZone { get; set; } = new();
        public Dictionary<string, int> ReportsByStatus { get; set; } = new();
    }

    public class IncidentStats
    {
        public int TotalIncidents { get; set; }
        public int OpenIncidents { get; set; }
        public int InProgressIncidents { get; set; }
        public int ClosedIncidents { get; set; }
    }

    public class ActionStats
    {
        public int TotalActions { get; set; }
        public int CompletedActions { get; set; }
        public int InProgressActions { get; set; }
        public int NotStartedActions { get; set; }
        public int OverdueActions { get; set; }
        public double CompletionRate { get; set; }
        public Dictionary<string, int> ActionsByHierarchy { get; set; } = new();
        public Dictionary<string, int> ActionsByStatus { get; set; } = new();
    }

    public class UserStats
    {
        public int TotalUsers { get; set; }
        public int ActiveUsers { get; set; }
        public int PendingUsers { get; set; }
        public Dictionary<string, int> UsersByRole { get; set; } = new();
        public Dictionary<string, int> UsersByDepartment { get; set; } = new();
    }

    public class TrendData
    {
        public List<MonthlyData> ReportTrends { get; set; } = new();
        public List<MonthlyData> ActionTrends { get; set; } = new();
        public List<DailyData> RecentActivity { get; set; } = new();
    }

    public class MonthlyData
    {
        public string Month { get; set; } = "";
        public int Count { get; set; }
        public string Label { get; set; } = "";
    }

    public class DailyData
    {
        public DateTime Date { get; set; }
        public int Reports { get; set; }
        public int Actions { get; set; }
        public int Comments { get; set; }
    }

    public class ChartDataDto
    {
        public string Type { get; set; } = "";
        public string Title { get; set; } = "";
        public Dictionary<string, object> Data { get; set; } = new();
    }

    public class RecentActivityDto
    {
        public string Type { get; set; } = ""; // "report", "action", "comment"
        public string Title { get; set; } = "";
        public string Description { get; set; } = "";
        public string UserName { get; set; } = "";
        public DateTime Timestamp { get; set; }
        public string Status { get; set; } = "";
        public int? RelatedId { get; set; }
    }

    public class PerformanceMetricsDto
    {
        public double AverageResponseTime { get; set; } // Average time to first response
        public double AverageResolutionTime { get; set; } // Average time to resolution
        public int TotalIncidents { get; set; }
        public int PreventedIncidents { get; set; }
        public double SafetyScore { get; set; }
        public List<KPIDto> KPIs { get; set; } = new();
    }

    public class KPIDto
    {
        public string Name { get; set; } = "";
        public double Value { get; set; }
        public string Unit { get; set; } = "";
        public string Target { get; set; } = "";
        public string Status { get; set; } = ""; // "good", "warning", "critical"
        public double PercentageChange { get; set; }
    }
}