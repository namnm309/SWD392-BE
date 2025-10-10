using Application.DTOs.Report;
using DomainLayer.Enum;

namespace Application.Services.Report
{
    public interface IReportService
    {
        // User functions
        Task<IReadOnlyList<ReportListItem>> GetUserReportsAsync(Guid userId, ReportFilterRequest? filter = null);
        Task<ReportDetail> GetReportByIdAsync(Guid id, Guid userId);
        Task<ReportDetail> CreateReportAsync(CreateReportRequest request, Guid userId);
        Task<ReportDetail> UpdateReportAsync(Guid id, UpdateReportRequest request, Guid userId);
        Task DeleteReportAsync(Guid id, Guid userId);
        
        // Admin functions
        Task<IReadOnlyList<ReportListItem>> GetAllReportsAsync(ReportFilterRequest? filter = null);
        Task<ReportDetail> GetReportByIdForAdminAsync(Guid id);
        Task<ReportDetail> UpdateReportStatusAsync(Guid id, AdminResponseRequest request, Guid adminId);
        
        // Utility functions
        Task<int> GetUserReportCountAsync(Guid userId);
        Task<int> GetPendingReportCountAsync();
    }
}
