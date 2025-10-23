using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Application.DTOs.Notification;
using Application.ResponseCode;
using Application.Services.Notification;
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
    [Route("api/notifications")]
    [Authorize]
    public class NotificationsController : ControllerBase
    {
        private readonly INotificationService _notificationService;

        public NotificationsController(INotificationService notificationService)
        {
            _notificationService = notificationService;
        }

        /// <summary>
        /// Lấy tất cả thông báo (Admin only)
        /// </summary>
        [HttpGet("admin/all")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetAllNotifications([FromQuery] NotificationFilterRequest? filter)
        {
            try
            {
                var notifications = await _notificationService.GetAllNotificationsAsync(filter);
                return SuccessResp.Ok(notifications);
            }
            catch (Exception ex)
            {
                return ErrorResp.BadRequest(ex.Message);
            }
        }

        /// <summary>
        /// Lấy thông báo theo ID (Admin only)
        /// </summary>
        [HttpGet("admin/{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetNotificationById(Guid id)
        {
            try
            {
                var notification = await _notificationService.GetNotificationByIdAsync(id);
                return SuccessResp.Ok(notification);
            }
            catch (Exception ex)
            {
                return ErrorResp.NotFound(ex.Message);
            }
        }

        /// <summary>
        /// Tạo thông báo mới (Admin only)
        /// </summary>
        [HttpPost("admin")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> CreateNotification([FromBody] CreateNotificationRequest request)
        {
            try
            {
                var adminId = GetCurrentUserId();
                var notification = await _notificationService.CreateNotificationAsync(request, adminId);
                return SuccessResp.Created(notification);
            }
            catch (Exception ex)
            {
                return ErrorResp.BadRequest(ex.Message);
            }
        }

        /// <summary>
        /// Cập nhật thông báo (Admin only)
        /// </summary>
        [HttpPut("admin/{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> UpdateNotification(Guid id, [FromBody] UpdateNotificationRequest request)
        {
            try
            {
                var adminId = GetCurrentUserId();
                var notification = await _notificationService.UpdateNotificationAsync(id, request, adminId);
                return SuccessResp.Ok(notification);
            }
            catch (Exception ex)
            {
                return ErrorResp.BadRequest(ex.Message);
            }
        }

        /// <summary>
        /// Xóa thông báo (Admin only)
        /// </summary>
        [HttpDelete("admin/{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteNotification(Guid id)
        {
            try
            {
                var adminId = GetCurrentUserId();
                await _notificationService.DeleteNotificationAsync(id, adminId);
                return NoContent(); // Return 204 No Content directly
            }
            catch (Exception ex)
            {
                return ErrorResp.BadRequest(ex.Message);
            }
        }

        /// <summary>
        /// Lấy thông báo của user
        /// </summary>
        [HttpGet("user")]
        public async Task<IActionResult> GetUserNotifications([FromQuery] NotificationFilterRequest? filter)
        {
            try
            {
                var userId = GetCurrentUserId();
                var notifications = await _notificationService.GetUserNotificationsAsync(userId, filter);
                return SuccessResp.Ok(notifications);
            }
            catch (Exception ex)
            {
                return ErrorResp.BadRequest(ex.Message);
            }
        }

        /// <summary>
        /// Đánh dấu thông báo đã đọc
        /// </summary>
        [HttpPost("mark-read")]
        public async Task<IActionResult> MarkAsRead([FromBody] MarkAsReadRequest request)
        {
            try
            {
                var userId = GetCurrentUserId();
                await _notificationService.MarkAsReadAsync(request.NotificationId, userId);
                return SuccessResp.Ok("Marked as read successfully");
            }
            catch (Exception ex)
            {
                return ErrorResp.BadRequest(ex.Message);
            }
        }

        /// <summary>
        /// Đánh dấu tất cả thông báo đã đọc
        /// </summary>
        [HttpPost("mark-all-read")]
        public async Task<IActionResult> MarkAllAsRead()
        {
            try
            {
                var userId = GetCurrentUserId();
                await _notificationService.MarkAllAsReadAsync(userId);
                return SuccessResp.Ok("All notifications marked as read");
            }
            catch (Exception ex)
            {
                return ErrorResp.BadRequest(ex.Message);
            }
        }

        /// <summary>
        /// Lấy số lượng thông báo chưa đọc
        /// </summary>
        [HttpGet("unread-count")]
        public async Task<IActionResult> GetUnreadCount()
        {
            try
            {
                var userId = GetCurrentUserId();
                var count = await _notificationService.GetUnreadCountAsync(userId);
                return SuccessResp.Ok(new { UnreadCount = count });
            }
            catch (Exception ex)
            {
                return ErrorResp.BadRequest(ex.Message);
            }
        }

        /// <summary>
        /// Cập nhật trạng thái thông báo (scheduled task)
        /// </summary>
        [HttpPost("update-status")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> UpdateNotificationStatus()
        {
            try
            {
                await _notificationService.UpdateNotificationStatusAsync();
                return SuccessResp.Ok("Notification status updated successfully");
            }
            catch (Exception ex)
            {
                return ErrorResp.BadRequest(ex.Message);
            }
        }

        private Guid GetCurrentUserId()
        {
            // Debug: Log all claims to see what's available
            Console.WriteLine("=== DEBUG: All Claims ===");
            foreach (var claim in User.Claims)
            {
                Console.WriteLine($"Claim Type: '{claim.Type}', Value: '{claim.Value}'");
            }
            
            // Try different ways to get session ID
            var sessionIdStr = User.FindFirst("sessionID")?.Value ?? 
                              User.FindFirst("sessionId")?.Value ?? 
                              User.FindFirst("SessionID")?.Value ?? 
                              User.FindFirst("SessionId")?.Value;
            
            Console.WriteLine($"Session ID found: '{sessionIdStr}'");
            
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
            
            Console.WriteLine($"User ID from session: {session.User.Id}");
            return session.User.Id;
        }
    }
}
