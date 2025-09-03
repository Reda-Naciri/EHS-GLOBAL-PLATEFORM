using HSEBackend.DTOs;
using HSEBackend.Models;

namespace HSEBackend.Interfaces
{
    public interface IReportService
    {
        Task<Report> CreateReportAsync(CreateReportDto dto);
        Task<bool> UpdateStatusAsync(UpdateStatusDto dto);
        Task<Comment> AddCommentAsync(CreateCommentDto dto);
        Task<IEnumerable<Report>> GetAllReportsAsync();
        Task<Report?> GetReportByIdAsync(int id);
        Task<Report?> GetReportByTrackingNumberAsync(string trackingNumber);
        Task<IEnumerable<ReportSummaryDto>> GetReportsAsync(string userId, string? type = null, string? zone = null, string? status = null);
        Task<bool> UpdateReportStatusAsync(int reportId, string status);
        Task<bool> OpenReportAsync(int reportId, string userId);
        Task<IEnumerable<RecentReportDto>> GetRecentReportsAsync(string userId, int limit = 10);
    }
}
