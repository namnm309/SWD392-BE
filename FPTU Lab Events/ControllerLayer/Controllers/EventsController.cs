using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Application.DTOs.Event;
using Application.ResponseCode;
using Application.Services.Event;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

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
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetAllEvents([FromQuery] EventFilterRequest? filter)
        {
            try
            {
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
        /// Tạo event mới (Admin only)
        /// </summary>
        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> CreateEvent([FromBody] CreateEventRequest request)
        {
            try
            {
                // Get admin ID from JWT token
                var adminIdClaim = User.FindFirst("sub") ?? User.FindFirst("id");
                if (adminIdClaim == null || !Guid.TryParse(adminIdClaim.Value, out var adminId))
                    return ErrorResp.Unauthorized("Invalid admin ID");

                var eventDetail = await _eventService.CreateEventAsync(request, adminId);
                return SuccessResp.Created(eventDetail);
            }
            catch (Exception ex)
            {
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
                // Get admin ID from JWT token
                var adminIdClaim = User.FindFirst("sub") ?? User.FindFirst("id");
                if (adminIdClaim == null || !Guid.TryParse(adminIdClaim.Value, out var adminId))
                    return ErrorResp.Unauthorized("Invalid admin ID");

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
                // Get admin ID from JWT token
                var adminIdClaim = User.FindFirst("sub") ?? User.FindFirst("id");
                if (adminIdClaim == null || !Guid.TryParse(adminIdClaim.Value, out var adminId))
                    return ErrorResp.Unauthorized("Invalid admin ID");

                await _eventService.DeleteEventAsync(id, request, adminId);
                return SuccessResp.NoContent();
            }
            catch (Exception ex)
            {
                return ErrorResp.BadRequest(ex.Message);
            }
        }
    }
}
