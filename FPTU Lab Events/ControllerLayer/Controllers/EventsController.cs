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
        /// AC-01: System must display list of all events
        /// AC-02: Each event must show: Event ID, Title, Description, Date, Time, Location, Status
        /// AC-03: User can filter by date range or status
        /// AC-04: Display upcoming events at the top
        /// AC-05: Display message "No events found" if list is empty
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetAllEvents([FromQuery] EventFilterRequest? filter)
        {
            try
            {
                var events = await _eventService.GetAllEventsAsync(filter);
                
                if (events.Count == 0)
                {
                    return SuccessResp.Ok(new { Message = "No events found", Events = events });
                }
                
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
                
                if (events.Count == 0)
                {
                    return SuccessResp.Ok(new { Message = "No upcoming events found", Events = events });
                }
                
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
                
                if (events.Count == 0)
                {
                    return SuccessResp.Ok(new { Message = "No events found in this date range", Events = events });
                }
                
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
        /// AC-01: System must display fields: Title, Description, Start Date, End Date, Location, Status
        /// AC-02: Required fields (Title, Start Date, End Date) cannot be null
        /// AC-03: End Date must be after Start Date
        /// AC-04: Upon successful submission, save event and send notification to all users
        /// AC-05: Display message: "Create event successfully"
        /// AC-06: Log creation event with admin ID, timestamp, and event ID
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
                return SuccessResp.Created(new { Message = "Create event successfully", Event = eventDetail });
            }
            catch (Exception ex)
            {
                return ErrorResp.BadRequest(ex.Message);
            }
        }

        /// <summary>
        /// Cập nhật event (Admin only)
        /// AC-01: System must display editable fields: Title, Description, Start Date, End Date, Location, Status
        /// AC-02: Required fields cannot be null
        /// AC-03: Upon valid submission, update event information
        /// AC-04: Send notification to users about event changes
        /// AC-05: Display message: "Update event successfully"
        /// AC-06: Log edit event with admin ID, event ID, timestamp, and changes
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
                return SuccessResp.Ok(new { Message = "Update event successfully", Event = eventDetail });
            }
            catch (Exception ex)
            {
                return ErrorResp.BadRequest(ex.Message);
            }
        }

        /// <summary>
        /// Xóa event (Admin only)
        /// AC-01: Admin must confirm deletion before removing
        /// AC-02: Upon confirmation, delete event from system
        /// AC-03: Send notification to users about event cancellation
        /// AC-04: Display message: "Delete event successfully"
        /// AC-05: Log deletion event with admin ID, event ID, and timestamp
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
                return SuccessResp.Ok(new { Message = "Delete event successfully" });
            }
            catch (Exception ex)
            {
                return ErrorResp.BadRequest(ex.Message);
            }
        }
    }
}
