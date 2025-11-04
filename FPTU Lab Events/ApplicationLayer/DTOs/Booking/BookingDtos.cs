using System;
using DomainLayer.Enum;

namespace Application.DTOs.Booking
{
	public class BookingListItem
	{
		public Guid Id { get; set; }
		public Guid RoomId { get; set; }
		public string RoomName { get; set; } = null!;
		public Guid UserId { get; set; }
		public string UserName { get; set; } = null!;
		public DateTime StartTime { get; set; }
		public DateTime EndTime { get; set; }
		public BookingStatus Status { get; set; }
		public Guid? EventId { get; set; }
		public string? Purpose { get; set; }
	}

	public class BookingDetail : BookingListItem
	{
		public string? Notes { get; set; }
	}

	public class CreateBookingRequest
	{
		/// <summary>
		/// ID của sự kiện cần đặt (bắt buộc)
		/// </summary>
		public Guid EventId { get; set; }
		
		/// <summary>
		/// Thời gian bắt đầu đặt (ISO 8601 format) - nếu không có sẽ dùng StartDate của Event
		/// </summary>
		public DateTime? StartTime { get; set; }
		
		/// <summary>
		/// Thời gian kết thúc đặt (ISO 8601 format) - nếu không có sẽ dùng EndDate của Event
		/// </summary>
		public DateTime? EndTime { get; set; }
		
		/// <summary>
		/// Mục đích tham gia event
		/// </summary>
		public string? Purpose { get; set; }
		
		/// <summary>
		/// Ghi chú thêm (tùy chọn)
		/// </summary>
		public string? Notes { get; set; }
	}

	public class UpdateBookingStatusRequest
	{
		/// <summary>
		/// Trạng thái mới của booking (0=Pending, 1=Approved, 2=Rejected, 3=Cancelled, 4=Completed)
		/// </summary>
		public BookingStatus Status { get; set; }
		
		/// <summary>
		/// Ghi chú khi cập nhật trạng thái (tùy chọn)
		/// </summary>
		public string? Notes { get; set; }
	}

	public class BookingFilterRequest
	{
		public Guid? RoomId { get; set; }
		public Guid? UserId { get; set; }
		
		/// <summary>
		/// ID của event để filter bookings
		/// </summary>
		public Guid? EventId { get; set; }
		
		/// <summary>
		/// Trạng thái booking (0=Pending, 1=Approved, 2=Rejected, 3=Cancelled, 4=Completed)
		/// </summary>
		public BookingStatus? Status { get; set; }
		
		public DateTime? From { get; set; }
		public DateTime? To { get; set; }
		public int? Page { get; set; }
		public int? PageSize { get; set; }
	}
}


