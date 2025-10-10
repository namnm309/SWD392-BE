using Application.DTOs.Report;
using DomainLayer.Entities;
using DomainLayer.Enum;
using InfrastructureLayer.Data;
using Microsoft.EntityFrameworkCore;

namespace Application.Services.Report
{
    public class ReportService : IReportService
    {
        private readonly LabDbContext _db;

        public ReportService(LabDbContext db)
        {
            _db = db;
        }

        public async Task<IReadOnlyList<ReportListItem>> GetUserReportsAsync(Guid userId, ReportFilterRequest? filter = null)
        {
            var query = _db.Reports
                .Include(r => r.Reporter)
                .Include(r => r.ResolvedByUser)
                .Where(r => r.ReporterId == userId)
                .AsQueryable();

            if (filter != null)
            {
                if (filter.Type.HasValue)
                    query = query.Where(r => r.Type == filter.Type.Value);
                
                if (filter.Status.HasValue)
                    query = query.Where(r => r.Status == filter.Status.Value);
                
                if (filter.StartDate.HasValue)
                    query = query.Where(r => r.ReportedDate >= filter.StartDate.Value);
                
                if (filter.EndDate.HasValue)
                    query = query.Where(r => r.ReportedDate <= filter.EndDate.Value);
            }

            query = query.OrderByDescending(r => r.ReportedDate);

            if (filter?.Page.HasValue == true && filter.PageSize.HasValue)
            {
                query = query.Skip(filter.Page.Value * filter.PageSize.Value)
                           .Take(filter.PageSize.Value);
            }

            var reports = await query.ToListAsync();

            return reports.Select(r => new ReportListItem
            {
                Id = r.Id,
                Title = r.Title,
                Description = r.Description,
                Type = r.Type.ToString(),
                ImageUrl = r.ImageUrl,
                ReportedDate = r.ReportedDate,
                Status = r.Status.ToString(),
                ReporterName = r.Reporter.Fullname,
                ResolvedAt = r.ResolvedAt,
                ResolvedByName = r.ResolvedByUser?.Fullname
            }).ToList();
        }

        public async Task<ReportDetail> GetReportByIdAsync(Guid id, Guid userId)
        {
            var report = await _db.Reports
                .Include(r => r.Reporter)
                .Include(r => r.ResolvedByUser)
                .FirstOrDefaultAsync(r => r.Id == id && r.ReporterId == userId)
                ?? throw new Exception("Report not found");

            return new ReportDetail
            {
                Id = report.Id,
                Title = report.Title,
                Description = report.Description,
                Type = report.Type.ToString(),
                ImageUrl = report.ImageUrl,
                ReportedDate = report.ReportedDate,
                Status = report.Status.ToString(),
                ReporterId = report.ReporterId,
                ReporterName = report.Reporter.Fullname,
                AdminResponse = report.AdminResponse,
                ResolvedAt = report.ResolvedAt,
                ResolvedByName = report.ResolvedByUser?.Fullname,
                CreatedAt = report.CreatedAt,
                LastUpdatedAt = report.LastUpdatedAt
            };
        }

        public async Task<ReportDetail> CreateReportAsync(CreateReportRequest request, Guid userId)
        {
            var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == userId)
                ?? throw new Exception("User not found");

            var report = new DomainLayer.Entities.Report
            {
                Id = Guid.NewGuid(),
                Title = request.Title,
                Description = request.Description,
                Type = request.Type,
                ImageUrl = request.ImageUrl,
                ReportedDate = DateTime.UtcNow,
                Status = ReportStatus.Open,
                ReporterId = userId,
                CreatedAt = DateTime.UtcNow,
                LastUpdatedAt = DateTime.UtcNow
            };

            _db.Reports.Add(report);
            await _db.SaveChangesAsync();

            return await GetReportByIdAsync(report.Id, userId);
        }

        public async Task<ReportDetail> UpdateReportAsync(Guid id, UpdateReportRequest request, Guid userId)
        {
            var report = await _db.Reports
                .FirstOrDefaultAsync(r => r.Id == id && r.ReporterId == userId)
                ?? throw new Exception("Report not found");

            if (report.Status != ReportStatus.Open)
                throw new Exception("Only open reports can be updated");

            if (!string.IsNullOrWhiteSpace(request.Title))
                report.Title = request.Title;
            
            if (!string.IsNullOrWhiteSpace(request.Description))
                report.Description = request.Description;
            
            if (request.Type.HasValue)
                report.Type = request.Type.Value;
            
            if (request.ImageUrl != null)
                report.ImageUrl = request.ImageUrl;

            report.LastUpdatedAt = DateTime.UtcNow;
            _db.Reports.Update(report);
            await _db.SaveChangesAsync();

            return await GetReportByIdAsync(report.Id, userId);
        }

        public async Task DeleteReportAsync(Guid id, Guid userId)
        {
            var report = await _db.Reports
                .FirstOrDefaultAsync(r => r.Id == id && r.ReporterId == userId)
                ?? throw new Exception("Report not found");

            if (report.Status != ReportStatus.Open)
                throw new Exception("Only open reports can be deleted");

            _db.Reports.Remove(report);
            await _db.SaveChangesAsync();
        }

        public async Task<IReadOnlyList<ReportListItem>> GetAllReportsAsync(ReportFilterRequest? filter = null)
        {
            var query = _db.Reports
                .Include(r => r.Reporter)
                .Include(r => r.ResolvedByUser)
                .AsQueryable();

            if (filter != null)
            {
                if (filter.Type.HasValue)
                    query = query.Where(r => r.Type == filter.Type.Value);
                
                if (filter.Status.HasValue)
                    query = query.Where(r => r.Status == filter.Status.Value);
                
                if (filter.StartDate.HasValue)
                    query = query.Where(r => r.ReportedDate >= filter.StartDate.Value);
                
                if (filter.EndDate.HasValue)
                    query = query.Where(r => r.ReportedDate <= filter.EndDate.Value);
            }

            query = query.OrderByDescending(r => r.ReportedDate);

            if (filter?.Page.HasValue == true && filter.PageSize.HasValue)
            {
                query = query.Skip(filter.Page.Value * filter.PageSize.Value)
                           .Take(filter.PageSize.Value);
            }

            var reports = await query.ToListAsync();

            return reports.Select(r => new ReportListItem
            {
                Id = r.Id,
                Title = r.Title,
                Description = r.Description,
                Type = r.Type.ToString(),
                ImageUrl = r.ImageUrl,
                ReportedDate = r.ReportedDate,
                Status = r.Status.ToString(),
                ReporterName = r.Reporter.Fullname,
                ResolvedAt = r.ResolvedAt,
                ResolvedByName = r.ResolvedByUser?.Fullname
            }).ToList();
        }

        public async Task<ReportDetail> GetReportByIdForAdminAsync(Guid id)
        {
            var report = await _db.Reports
                .Include(r => r.Reporter)
                .Include(r => r.ResolvedByUser)
                .FirstOrDefaultAsync(r => r.Id == id)
                ?? throw new Exception("Report not found");

            return new ReportDetail
            {
                Id = report.Id,
                Title = report.Title,
                Description = report.Description,
                Type = report.Type.ToString(),
                ImageUrl = report.ImageUrl,
                ReportedDate = report.ReportedDate,
                Status = report.Status.ToString(),
                ReporterId = report.ReporterId,
                ReporterName = report.Reporter.Fullname,
                AdminResponse = report.AdminResponse,
                ResolvedAt = report.ResolvedAt,
                ResolvedByName = report.ResolvedByUser?.Fullname,
                CreatedAt = report.CreatedAt,
                LastUpdatedAt = report.LastUpdatedAt
            };
        }

        public async Task<ReportDetail> UpdateReportStatusAsync(Guid id, AdminResponseRequest request, Guid adminId)
        {
            var report = await _db.Reports
                .FirstOrDefaultAsync(r => r.Id == id)
                ?? throw new Exception("Report not found");

            var admin = await _db.Users.FirstOrDefaultAsync(u => u.Id == adminId)
                ?? throw new Exception("Admin not found");

            report.AdminResponse = request.AdminResponse;
            report.Status = request.Status;
            report.ResolvedBy = adminId;
            report.ResolvedAt = DateTime.UtcNow;
            report.LastUpdatedAt = DateTime.UtcNow;

            _db.Reports.Update(report);
            await _db.SaveChangesAsync();

            return await GetReportByIdForAdminAsync(report.Id);
        }

        public async Task<int> GetUserReportCountAsync(Guid userId)
        {
            return await _db.Reports
                .CountAsync(r => r.ReporterId == userId);
        }

        public async Task<int> GetPendingReportCountAsync()
        {
            return await _db.Reports
                .CountAsync(r => r.Status == ReportStatus.Open);
        }
    }
}
