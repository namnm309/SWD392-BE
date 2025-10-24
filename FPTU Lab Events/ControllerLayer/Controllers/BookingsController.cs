using System.Security.Claims;
using Application.DTOs.Booking;
using Application.Services.Booking;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ControllerLayer.Controllers
{
    /// <summary>
    /// Nam
    /// </summary>
    [ApiController]
	[Route("api/[controller]")]
	public class BookingsController : ControllerBase
	{
		private readonly IBookingService _service;

		public BookingsController(IBookingService service)
		{
			_service = service;
		}

		/// <summary>
		/// Lấy danh sách booking với bộ lọc tùy chọn.
		/// </summary>
		/// <returns>Danh sách booking rút gọn.</returns>
		[HttpGet]
		public async Task<ActionResult<IReadOnlyList<BookingListItem>>> Get([FromQuery] BookingFilterRequest? filter)
		{
			var result = await _service.GetBookingsAsync(filter);
			return Ok(result);
		}

		/// <summary>
		/// Lấy chi tiết một booking theo Id.
		/// </summary>
		/// <param name="id">Id booking </param>
		/// <returns>Thông tin chi tiết booking.</returns>
		[HttpGet("{id}")]
		public async Task<ActionResult<BookingDetail>> GetById(Guid id)
		{
			return Ok(await _service.GetByIdAsync(id));
		}

		/// <summary>
		/// Tạo booking mới cho người dùng hiện tại.
		/// </summary>
		/// <param name="request">Phải có RoomId với EventId  </param>
		/// <returns>Booking vừa tạo với trạng thái Pending (chờ duyệt).</returns>
		[HttpPost]
		[Authorize]
		public async Task<ActionResult<BookingDetail>> Create(CreateBookingRequest request)
		{
			// Try to get user ID from different claim types
			var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? 
						   User.FindFirstValue("nameid") ?? 
						   User.FindFirstValue("sub");
			if (string.IsNullOrEmpty(userIdStr)) return Unauthorized();
			var result = await _service.CreateAsync(Guid.Parse(userIdStr), request);
			return Ok(result);
		}

		/// <summary>
		/// Cập nhật trạng thái của một booking (chỉ dành cho Admin).
		/// </summary>
		/// <param name="id">Id booking cần cập nhật trạng thái.</param>
		/// <param name="request">Trạng thái mới (Pending=0, Approved=1, Rejected=2, Cancelled=3, Completed=4) và ghi chú.</param>
		/// <returns>Booking sau khi cập nhật trạng thái.</returns>
		[HttpPatch("{id}/status")]
		[Authorize]
		public async Task<ActionResult<BookingDetail>> UpdateStatus(Guid id, UpdateBookingStatusRequest request)
		{
			return Ok(await _service.UpdateStatusAsync(id, request));
		}

		/// <summary>
		/// Xóa một booking theo Id (chỉ dành cho Admin hoặc người tạo booking).
		/// </summary>
		/// <param name="id">Id booking cần xóa.</param>
		/// <returns>Không có nội dung trả về (204 No Content).</returns>
		[HttpDelete("{id}")]
		[Authorize]
		public async Task<IActionResult> Delete(Guid id)
		{
			await _service.DeleteAsync(id);
			return NoContent();
		}
	}
}


