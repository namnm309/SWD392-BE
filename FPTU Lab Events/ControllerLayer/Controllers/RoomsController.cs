using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Application.DTOs.Room;
using Application.ResponseCode;
using Application.Services.Room;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ControllerLayer.Controllers
{
    /// <summary>
    /// Tú
    /// </summary>
    [ApiController]
    [Route("api/rooms")]
    [Authorize]
    public class RoomsController : ControllerBase
    {
        private readonly IRoomService _roomService;

        public RoomsController(IRoomService roomService)
        {
            _roomService = roomService;
        }

        /// <summary>
        /// Lấy tất cả phòng
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetAllRooms([FromQuery] RoomFilterRequest? filter)
        {
            try
            {
                var rooms = await _roomService.GetAllRoomsAsync(filter);
                return SuccessResp.Ok(rooms);
            }
            catch (Exception ex)
            {
                return ErrorResp.BadRequest(ex.Message);
            }
        }

        /// <summary>
        /// Lấy phòng theo ID
        /// </summary>
        [HttpGet("{id}")]
        public async Task<IActionResult> GetRoomById(Guid id)
        {
            try
            {
                var room = await _roomService.GetRoomByIdAsync(id);
                return SuccessResp.Ok(room);
            }
            catch (Exception ex)
            {
                return ErrorResp.NotFound(ex.Message);
            }
        }

        /// <summary>
        /// Tạo phòng mới (Admin only)
        /// </summary>
        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> CreateRoom([FromBody] CreateRoomRequest request)
        {
            try
            {
                var room = await _roomService.CreateRoomAsync(request);
                return SuccessResp.Created(room);
            }
            catch (Exception ex)
            {
                return ErrorResp.BadRequest(ex.Message);
            }
        }

        /// <summary>
        /// Cập nhật phòng (Admin only)
        /// </summary>
        [HttpPut("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> UpdateRoom(Guid id, [FromBody] UpdateRoomRequest request)
        {
            try
            {
                var room = await _roomService.UpdateRoomAsync(id, request);
                return SuccessResp.Ok(room);
            }
            catch (Exception ex)
            {
                return ErrorResp.BadRequest(ex.Message);
            }
        }

        /// <summary>
        /// Cập nhật trạng thái phòng (Admin only)
        /// </summary>
        [HttpPatch("{id}/status")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> UpdateRoomStatus(Guid id, [FromBody] UpdateRoomStatusRequest request)
        {
            try
            {
                var room = await _roomService.UpdateRoomStatusAsync(id, request);
                return SuccessResp.Ok(room);
            }
            catch (Exception ex)
            {
                return ErrorResp.BadRequest(ex.Message);
            }
        }

        /// <summary>
        /// Xóa phòng (Admin only)
        /// </summary>
        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteRoom(Guid id)
        {
            try
            {
                await _roomService.DeleteRoomAsync(id);
                return SuccessResp.NoContent();
            }
            catch (Exception ex)
            {
                return ErrorResp.BadRequest(ex.Message);
            }
        }

        /// <summary>
        /// Lấy phòng có sẵn trong khoảng thời gian
        /// </summary>
        [HttpGet("available")]
        public async Task<IActionResult> GetAvailableRooms([FromQuery] DateTime startTime, [FromQuery] DateTime endTime)
        {
            try
            {
                var rooms = await _roomService.GetAvailableRoomsAsync(startTime, endTime);
                return SuccessResp.Ok(rooms);
            }
            catch (Exception ex)
            {
                return ErrorResp.BadRequest(ex.Message);
            }
        }

        /// <summary>
        /// Kiểm tra phòng có sẵn không
        /// </summary>
        [HttpGet("{id}/available")]
        public async Task<IActionResult> IsRoomAvailable(Guid id, [FromQuery] DateTime startTime, [FromQuery] DateTime endTime)
        {
            try
            {
                var isAvailable = await _roomService.IsRoomAvailableAsync(id, startTime, endTime);
                return SuccessResp.Ok(new { IsAvailable = isAvailable });
            }
            catch (Exception ex)
            {
                return ErrorResp.BadRequest(ex.Message);
            }
        }

        /// <summary>
        /// Lấy số lượng phòng
        /// </summary>
        [HttpGet("count")]
        public async Task<IActionResult> GetRoomCount()
        {
            try
            {
                var count = await _roomService.GetRoomCountAsync();
                return SuccessResp.Ok(new { Count = count });
            }
            catch (Exception ex)
            {
                return ErrorResp.BadRequest(ex.Message);
            }
        }

        /// <summary>
        /// Lấy số lượng phòng có sẵn
        /// </summary>
        [HttpGet("available-count")]
        public async Task<IActionResult> GetAvailableRoomCount()
        {
            try
            {
                var count = await _roomService.GetAvailableRoomCountAsync();
                return SuccessResp.Ok(new { AvailableCount = count });
            }
            catch (Exception ex)
            {
                return ErrorResp.BadRequest(ex.Message);
            }
        }
    }
}
