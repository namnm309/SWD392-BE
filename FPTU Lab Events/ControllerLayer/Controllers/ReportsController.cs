using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Application.DTOs.Report;
using Application.ResponseCode;
using Application.Services.Report;
using InfrastructureLayer.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ControllerLayer.Controllers
{
    /// <summary>
    /// Tú
    /// </summary>
    [ApiController]
    [Route("api/reports")]
    [Authorize]
    public class ReportsController : ControllerBase
    {
        private readonly IReportService _reportService;

        public ReportsController(IReportService reportService)
        {
            _reportService = reportService;
        }

        /// <summary>
        /// Lấy tất cả báo cáo (Admin only)
        /// </summary>
        [HttpGet("admin/all")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetAllReports([FromQuery] ReportFilterRequest? filter)
        {
            try
            {
                var reports = await _reportService.GetAllReportsAsync(filter);
                return SuccessResp.Ok(reports);
            }
            catch (Exception ex)
            {
                return ErrorResp.BadRequest(ex.Message);
            }
        }

        /// <summary>
        /// Lấy báo cáo theo ID (Admin only)
        /// </summary>
        [HttpGet("admin/{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetReportByIdForAdmin(Guid id)
        {
            try
            {
                var report = await _reportService.GetReportByIdForAdminAsync(id);
                return SuccessResp.Ok(report);
            }
            catch (Exception ex)
            {
                return ErrorResp.NotFound(ex.Message);
            }
        }

        /// <summary>
        /// Cập nhật trạng thái báo cáo (Admin only)
        /// </summary>
        [HttpPut("admin/{id}/status")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> UpdateReportStatus(Guid id, [FromBody] AdminResponseRequest request)
        {
            try
            {
                var adminId = GetCurrentUserId();
                var report = await _reportService.UpdateReportStatusAsync(id, request, adminId);
                return SuccessResp.Ok(report);
            }
            catch (Exception ex)
            {
                return ErrorResp.BadRequest(ex.Message);
            }
        }

        /// <summary>
        /// Lấy báo cáo của user
        /// </summary>
        [HttpGet("user")]
        public async Task<IActionResult> GetUserReports([FromQuery] ReportFilterRequest? filter)
        {
            try
            {
                var userId = GetCurrentUserId();
                var reports = await _reportService.GetUserReportsAsync(userId, filter);
                return SuccessResp.Ok(reports);
            }
            catch (Exception ex)
            {
                return ErrorResp.BadRequest(ex.Message);
            }
        }

        /// <summary>
        /// Lấy báo cáo theo ID của user
        /// </summary>
        [HttpGet("user/{id}")]
        public async Task<IActionResult> GetUserReportById(Guid id)
        {
            try
            {
                var userId = GetCurrentUserId();
                var report = await _reportService.GetReportByIdAsync(id, userId);
                return SuccessResp.Ok(report);
            }
            catch (Exception ex)
            {
                return ErrorResp.NotFound(ex.Message);
            }
        }

        /// <summary>
        /// Tạo báo cáo mới
        /// </summary>
        [HttpPost("user")]
        public async Task<IActionResult> CreateReport([FromBody] CreateReportRequest request)
        {
            try
            {
                var userId = GetCurrentUserId();
                var report = await _reportService.CreateReportAsync(request, userId);
                return SuccessResp.Created(report);
            }
            catch (Exception ex)
            {
                return ErrorResp.BadRequest(ex.Message);
            }
        }

        /// <summary>
        /// Cập nhật báo cáo của user
        /// </summary>
        [HttpPut("user/{id}")]
        public async Task<IActionResult> UpdateUserReport(Guid id, [FromBody] UpdateReportRequest request)
        {
            try
            {
                var userId = GetCurrentUserId();
                var report = await _reportService.UpdateReportAsync(id, request, userId);
                return SuccessResp.Ok(report);
            }
            catch (Exception ex)
            {
                return ErrorResp.BadRequest(ex.Message);
            }
        }

        /// <summary>
        /// Xóa báo cáo của user
        /// </summary>
        [HttpDelete("user/{id}")]
        public async Task<IActionResult> DeleteUserReport(Guid id)
        {
            try
            {
                var userId = GetCurrentUserId();
                await _reportService.DeleteReportAsync(id, userId);
                return SuccessResp.NoContent();
            }
            catch (Exception ex)
            {
                return ErrorResp.BadRequest(ex.Message);
            }
        }

        /// <summary>
        /// Lấy số lượng báo cáo của user
        /// </summary>
        [HttpGet("user/count")]
        public async Task<IActionResult> GetUserReportCount()
        {
            try
            {
                var userId = GetCurrentUserId();
                var count = await _reportService.GetUserReportCountAsync(userId);
                return SuccessResp.Ok(new { Count = count });
            }
            catch (Exception ex)
            {
                return ErrorResp.BadRequest(ex.Message);
            }
        }

        /// <summary>
        /// Lấy số lượng báo cáo chờ xử lý (Admin only)
        /// </summary>
        [HttpGet("admin/pending-count")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetPendingReportCount()
        {
            try
            {
                var count = await _reportService.GetPendingReportCountAsync();
                return SuccessResp.Ok(new { PendingCount = count });
            }
            catch (Exception ex)
            {
                return ErrorResp.BadRequest(ex.Message);
            }
        }

        private Guid GetCurrentUserId()
        {
            // Try different ways to get session ID
            var sessionIdStr = User.FindFirst("sessionID")?.Value ?? 
                              User.FindFirst("sessionId")?.Value ?? 
                              User.FindFirst("SessionID")?.Value ?? 
                              User.FindFirst("SessionId")?.Value;
            
            if (string.IsNullOrEmpty(sessionIdStr))
            {
                throw new Exception("Session ID not found in token");
            }
            
            // Get user ID from session
            using var scope = HttpContext.RequestServices.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<LabDbContext>();
            
            var session = db.UserSessions
                .Include(s => s.User)
                .ThenInclude(u => u.Roles)
                .FirstOrDefault(s => s.Id == Guid.Parse(sessionIdStr) && s.RevokedAt == null);
                
            if (session == null)
            {
                throw new Exception("Session not found or expired");
            }
            
            return session.User.Id;
        }
    }
}
