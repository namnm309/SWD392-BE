using System;
using DomainLayer.Enum;

namespace Application.DTOs.Report
{
    public class ReportListItem
    {
        public Guid Id { get; set; }
        public string Title { get; set; } = null!;
        public string Description { get; set; } = null!;
        public string Type { get; set; } = null!;
        public string? ImageUrl { get; set; }
        public DateTime ReportedDate { get; set; }
        public string Status { get; set; } = null!;
        public string ReporterName { get; set; } = null!;
        public DateTime? ResolvedAt { get; set; }
        public string? ResolvedByName { get; set; }
    }

    public class ReportDetail : ReportListItem
    {
        public Guid ReporterId { get; set; }
        public string? AdminResponse { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime LastUpdatedAt { get; set; }
    }

    public class CreateReportRequest
    {
        public string Title { get; set; } = null!;
        public string Description { get; set; } = null!;
        public ReportType Type { get; set; }
        public string? ImageUrl { get; set; }
    }

    public class UpdateReportRequest
    {
        public string? Title { get; set; }
        public string? Description { get; set; }
        public ReportType? Type { get; set; }
        public string? ImageUrl { get; set; }
    }

    public class ReportFilterRequest
    {
        public ReportType? Type { get; set; }
        public ReportStatus? Status { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public int? Page { get; set; }
        public int? PageSize { get; set; }
    }

    public class AdminResponseRequest
    {
        public string AdminResponse { get; set; } = null!;
        public ReportStatus Status { get; set; }
    }
}
