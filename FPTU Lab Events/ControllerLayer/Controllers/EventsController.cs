using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Application.DTOs.Event;
using Application.ResponseCode;
using Application.Services.Event;
using DomainLayer.Enum;
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
    [Route("api/events")]
    [Authorize]
    public class EventsController : ControllerBase
    {
        private readonly IEventService _eventService;

        public EventsController(IEventService eventService)
        {
            _eventService = eventService;
        }

        /// <summary>
        /// Lấy tất cả events (View Event API)
        /// - User thường (Student, Lecturer): Chỉ xem events Active
        /// - Staff/Admin: Xem tất cả events (kể cả Pending, Rejected)
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetAllEvents([FromQuery] EventFilterRequest? filter)
        {
            try
            {
                var userRole = User.FindFirst("Role")?.Value ?? User.FindFirst("role")?.Value;
                
                // Nếu không phải Staff/Admin, chỉ cho xem Active events
                if (userRole != "Admin" && userRole != "Staff")
                {
                    filter ??= new EventFilterRequest();
                    filter.Status = EventStatus.Active;
                }
                
                var events = await _eventService.GetAllEventsAsync(filter);
                return SuccessResp.Ok(events);
            }
            catch (Exception ex)
            {
                return ErrorResp.BadRequest(ex.Message);
            }
        }

        /// <summary>
        /// Lấy event theo ID
        /// </summary>
        [HttpGet("{id}")]
        public async Task<IActionResult> GetEventById(Guid id)
        {
            try
            {
                var eventDetail = await _eventService.GetEventByIdAsync(id);
                return SuccessResp.Ok(eventDetail);
            }
            catch (Exception ex)
            {
                return ErrorResp.NotFound(ex.Message);
            }
        }

        /// <summary>
        /// Lấy upcoming events
        /// </summary>
        [HttpGet("upcoming")]
        public async Task<IActionResult> GetUpcomingEvents()
        {
            try
            {
                var events = await _eventService.GetUpcomingEventsAsync();
                return SuccessResp.Ok(events);
            }
            catch (Exception ex)
            {
                return ErrorResp.BadRequest(ex.Message);
            }
        }

        /// <summary>
        /// Lấy events của user hiện tại (my events)
        /// </summary>
        [HttpGet("my-events")]
        public async Task<IActionResult> GetMyEvents()
        {
            try
            {
                var userId = GetCurrentUserId();
                var events = await _eventService.GetEventsByUserIdAsync(userId);
                return SuccessResp.Ok(events);
            }
            catch (Exception ex)
            {
                return ErrorResp.BadRequest(ex.Message);
            }
        }

        /// <summary>
        /// Lấy events theo userId
        /// - Lecturer chỉ xem được events của chính mình
        /// - Admin có thể xem events của bất kỳ user nào
        /// </summary>
        [HttpGet("user/{userId}")]
        [Authorize(Roles = "Admin,Lecturer")]
        public async Task<IActionResult> GetEventsByUserId(Guid userId)
        {
            try
            {
                var currentUserId = GetCurrentUserId();
                var userRole = User.FindFirst("Role")?.Value ?? User.FindFirst("role")?.Value;
                
                // Nếu là Lecturer, chỉ được xem events của chính mình
                if (userRole == "Lecturer" && currentUserId != userId)
                {
                    return ErrorResp.Forbidden("Lecturers can only view their own events. Please use your own userId or use /api/events/my-events instead.");
                }
                
                // Admin có thể xem events của bất kỳ ai
                var events = await _eventService.GetEventsByUserIdAsync(userId);
                return SuccessResp.Ok(events);
            }
            catch (Exception ex)
            {
                return ErrorResp.BadRequest(ex.Message);
            }
        }

        /// <summary>
        /// Lấy events theo date range
        /// </summary>
        [HttpGet("date-range")]
        public async Task<IActionResult> GetEventsByDateRange([FromQuery] DateTime startDate, [FromQuery] DateTime endDate)
        {
            try
            {
                var events = await _eventService.GetEventsByDateRangeAsync(startDate, endDate);
                return SuccessResp.Ok(events);
            }
            catch (Exception ex)
            {
                return ErrorResp.BadRequest(ex.Message);
            }
        }

        /// <summary>
        /// Lấy số lượng events
        /// </summary>
        [HttpGet("count")]
        public async Task<IActionResult> GetEventCount()
        {
            try
            {
                var count = await _eventService.GetEventCountAsync();
                return SuccessResp.Ok(new { Count = count });
            }
            catch (Exception ex)
            {
                return ErrorResp.BadRequest(ex.Message);
            }
        }

        /// <summary>
        /// Lấy số lượng active events
        /// </summary>
        [HttpGet("active-count")]
        public async Task<IActionResult> GetActiveEventCount()
        {
            try
            {
                var count = await _eventService.GetActiveEventCountAsync();
                return SuccessResp.Ok(new { ActiveCount = count });
            }
            catch (Exception ex)
            {
                return ErrorResp.BadRequest(ex.Message);
            }
        }

        /// <summary>
        /// Tạo event mới
        /// - Lecturer: Event được tạo với status Pending, cần Staff duyệt
        /// - Admin/Staff: Event được tạo với status Active, không cần duyệt
        /// </summary>
        [HttpPost]
        [Authorize(Roles = "Admin,Lecturer")]
        public async Task<IActionResult> CreateEvent([FromBody] CreateEventRequest request)
        {
            try
            {
                var userId = GetCurrentUserId();
                var userRole = User.FindFirst("Role")?.Value ?? User.FindFirst("role")?.Value ?? "Unknown";
                
                Console.WriteLine($"=== DEBUG: CreateEvent Request ===");
                Console.WriteLine($"Title: {request.Title}");
                Console.WriteLine($"User ID: {userId}");
                Console.WriteLine($"User Role: {userRole}");
                
                var eventDetail = await _eventService.CreateEventAsync(request, userId, userRole);
                
                // Return different messages based on status
                if (eventDetail.Status == "Pending")
                {
                    return SuccessResp.Created(new 
                    { 
                        Event = eventDetail, 
                        Message = "Event created successfully and is pending Staff approval." 
                    });
                }
                
                return SuccessResp.Created(eventDetail);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"=== ERROR: CreateEvent ===");
                Console.WriteLine($"Error: {ex.Message}");
                Console.WriteLine($"StackTrace: {ex.StackTrace}");
                return ErrorResp.BadRequest(ex.Message);
            }
        }

        /// <summary>
        /// Cập nhật event (Admin only)
        /// </summary>
        [HttpPut("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> UpdateEvent(Guid id, [FromBody] UpdateEventRequest request)
        {
            try
            {
                var adminId = GetCurrentUserId();
                var eventDetail = await _eventService.UpdateEventAsync(id, request, adminId);
                return SuccessResp.Ok(eventDetail);
            }
            catch (Exception ex)
            {
                return ErrorResp.BadRequest(ex.Message);
            }
        }

        /// <summary>
        /// Xóa event (Admin only)
        /// </summary>
        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteEvent(Guid id, [FromBody] DeleteEventRequest request)
        {
            try
            {
                var adminId = GetCurrentUserId();
                await _eventService.DeleteEventAsync(id, request, adminId);
                return NoContent();
            }
            catch (Exception ex)
            {
                return ErrorResp.BadRequest(ex.Message);
            }
        }

        /// <summary>
        /// Lấy danh sách events chờ duyệt (Admin only)
        /// </summary>
        [HttpGet("pending")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetPendingEvents()
        {
            try
            {
                var events = await _eventService.GetPendingEventsAsync();
                return SuccessResp.Ok(events);
            }
            catch (Exception ex)
            {
                return ErrorResp.BadRequest(ex.Message);
            }
        }

        /// <summary>
        /// Lấy số lượng events chờ duyệt (Admin only)
        /// </summary>
        [HttpGet("pending-count")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetPendingEventCount()
        {
            try
            {
                var count = await _eventService.GetPendingEventCountAsync();
                return SuccessResp.Ok(new { PendingCount = count });
            }
            catch (Exception ex)
            {
                return ErrorResp.BadRequest(ex.Message);
            }
        }

        /// <summary>
        /// Duyệt event (Admin only)
        /// </summary>
        [HttpPost("{id}/approve")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> ApproveEvent(Guid id, [FromBody] ApproveEventRequest? request = null)
        {
            try
            {
                var staffId = GetCurrentUserId();
                var approvalNote = request?.ApprovalNote;
                var eventDetail = await _eventService.ApproveEventAsync(id, staffId, approvalNote);
                return SuccessResp.Ok(new 
                { 
                    Event = eventDetail, 
                    Message = "Event approved successfully and is now active." 
                });
            }
            catch (Exception ex)
            {
                return ErrorResp.BadRequest(ex.Message);
            }
        }

        /// <summary>
        /// Từ chối event (Admin only)
        /// </summary>
        [HttpPost("{id}/reject")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> RejectEvent(Guid id, [FromBody] RejectEventRequest request)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(request.RejectionReason))
                    return ErrorResp.BadRequest("Rejection reason is required");

                var staffId = GetCurrentUserId();
                var eventDetail = await _eventService.RejectEventAsync(id, staffId, request.RejectionReason);
                return SuccessResp.Ok(new 
                { 
                    Event = eventDetail, 
                    Message = "Event rejected successfully." 
                });
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
