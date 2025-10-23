using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Application.DTOs.Lab;
using Application.ResponseCode;
using Application.Services.Lab;
using InfrastructureLayer.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ControllerLayer.Controllers
{
    /// <summary>
    /// Hiền 
    /// </summary>
    [ApiController]
    [Route("api/labs")]
    [Authorize]
    public class LabsController : ControllerBase
    {
        private readonly ILabService _labService;

        public LabsController(ILabService labService)
        {
            _labService = labService;
        }

        /// <summary>
        /// Lấy tất cả lab (View Lab API)
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetAllLabs([FromQuery] LabFilterRequest? filter)
        {
            try
            {
                var labs = await _labService.GetAllLabsAsync(filter);
                return SuccessResp.Ok(labs);
            }
            catch (Exception ex)
            {
                return ErrorResp.BadRequest(ex.Message);
            }
        }

        /// <summary>
        /// Lấy lab theo ID (View Lab Detail)
        /// </summary>
        [HttpGet("{id}")]
        public async Task<IActionResult> GetLabById(Guid id)
        {
            try
            {
                var lab = await _labService.GetLabByIdAsync(id);
                return SuccessResp.Ok(lab);
            }
            catch (Exception ex)
            {
                return ErrorResp.NotFound(ex.Message);
            }
        }

        /// <summary>
        /// Lấy lab có sẵn (Available Labs)
        /// </summary>
        [HttpGet("available")]
        public async Task<IActionResult> GetAvailableLabs()
        {
            try
            {
                var labs = await _labService.GetAvailableLabsAsync();
                return SuccessResp.Ok(labs);
            }
            catch (Exception ex)
            {
                return ErrorResp.BadRequest(ex.Message);
            }
        }

        /// <summary>
        /// Kiểm tra lab có sẵn không
        /// </summary>
        [HttpGet("{id}/available")]
        public async Task<IActionResult> IsLabAvailable(Guid id)
        {
            try
            {
                var isAvailable = await _labService.IsLabAvailableAsync(id);
                return SuccessResp.Ok(new { IsAvailable = isAvailable });
            }
            catch (Exception ex)
            {
                return ErrorResp.BadRequest(ex.Message);
            }
        }

        /// <summary>
        /// Lấy số lượng lab
        /// </summary>
        [HttpGet("count")]
        public async Task<IActionResult> GetLabCount()
        {
            try
            {
                var count = await _labService.GetLabCountAsync();
                return SuccessResp.Ok(new { Count = count });
            }
            catch (Exception ex)
            {
                return ErrorResp.BadRequest(ex.Message);
            }
        }

        /// <summary>
        /// Lấy số lượng lab đang hoạt động
        /// </summary>
        [HttpGet("active-count")]
        public async Task<IActionResult> GetActiveLabCount()
        {
            try
            {
                var count = await _labService.GetActiveLabCountAsync();
                return SuccessResp.Ok(new { ActiveCount = count });
            }
            catch (Exception ex)
            {
                return ErrorResp.BadRequest(ex.Message);
            }
        }

        /// <summary>
        /// Tạo lab mới (Admin only)
        /// </summary>
        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> CreateLab([FromBody] CreateLabRequest request)
        {
            try
            {
                var adminId = GetCurrentUserId();
                var lab = await _labService.CreateLabAsync(request, adminId);
                return SuccessResp.Created(lab);
            }
            catch (Exception ex)
            {
                return ErrorResp.BadRequest(ex.Message);
            }
        }

        /// <summary>
        /// Cập nhật lab (Admin only)
        /// </summary>
        [HttpPut("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> UpdateLab(Guid id, [FromBody] UpdateLabRequest request)
        {
            try
            {
                var adminId = GetCurrentUserId();
                var lab = await _labService.UpdateLabAsync(id, request, adminId);
                return SuccessResp.Ok(lab);
            }
            catch (Exception ex)
            {
                return ErrorResp.BadRequest(ex.Message);
            }
        }

        /// <summary>
        /// Cập nhật trạng thái lab (Admin only)
        /// </summary>
        [HttpPatch("{id}/status")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> UpdateLabStatus(Guid id, [FromBody] UpdateLabStatusRequest request)
        {
            try
            {
                var lab = await _labService.UpdateLabStatusAsync(id, request);
                return SuccessResp.Ok(lab);
            }
            catch (Exception ex)
            {
                return ErrorResp.BadRequest(ex.Message);
            }
        }

        /// <summary>
        /// Xóa lab (Admin only)
        /// </summary>
        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteLab(Guid id, [FromBody] DeleteLabRequest request)
        {
            try
            {
                var adminId = GetCurrentUserId();
                await _labService.DeleteLabAsync(id, request, adminId);
                return NoContent();
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
