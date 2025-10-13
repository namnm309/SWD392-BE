using Application.DTOs.Booking;
using Application.Services.Booking;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ControllerLayer.Controllers
{
	[ApiController]
	[Route("api/[controller]")]
	/// <summary>
	/// Quản lý đặt lịch phòng: tạo, xem, cập nhật trạng thái và xóa booking.
	/// </summary>
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
		/// <param name="filter">Bộ lọc theo phòng, người dùng, trạng thái, khoảng thời gian, phân trang.</param>
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
		/// <param name="id">Id booking.</param>
		/// <returns>Thông tin chi tiết booking.</returns>
		[HttpGet("{id}")]
		public async Task<ActionResult<BookingDetail>> GetById(Guid id)
		{
			return Ok(await _service.GetByIdAsync(id));
		}

		/// <summary>
		/// Tạo booking mới cho người dùng hiện tại.
		/// </summary>
		/// <param name="request">Thông tin tạo booking.</param>
		/// <returns>Booking vừa tạo.</returns>
		[HttpPost]
		[Authorize]
		public async Task<ActionResult<BookingDetail>> Create(CreateBookingRequest request)
		{
			var userIdClaim = User.FindFirst("id")?.Value;
			if (string.IsNullOrEmpty(userIdClaim)) return Unauthorized();
			var result = await _service.CreateAsync(Guid.Parse(userIdClaim), request);
			return Ok(result);
		}

		/// <summary>
		/// Cập nhật trạng thái của một booking.
		/// </summary>
		/// <param name="id">Id booking.</param>
		/// <param name="request">Trạng thái mới và ghi chú.</param>
		/// <returns>Booking sau khi cập nhật.</returns>
		[HttpPatch("{id}/status")]
		[Authorize]
		public async Task<ActionResult<BookingDetail>> UpdateStatus(Guid id, UpdateBookingStatusRequest request)
		{
			return Ok(await _service.UpdateStatusAsync(id, request));
		}

		/// <summary>
		/// Xóa một booking theo Id.
		/// </summary>
		/// <param name="id">Id booking.</param>
		/// <returns>No content.</returns>
		[HttpDelete("{id}")]
		[Authorize]
		public async Task<IActionResult> Delete(Guid id)
		{
			await _service.DeleteAsync(id);
			return NoContent();
		}
	}
}


