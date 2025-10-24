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
	}

	public class BookingDetail : BookingListItem
	{
		public Guid? EventId { get; set; }
		public string? Purpose { get; set; }
		public string? Notes { get; set; }
	}

	public class CreateBookingRequest
	{
		/// <summary>
		/// ID của phòng cần đặt
		/// </summary>
		public Guid RoomId { get; set; }
		
		/// <summary>
		/// Thời gian bắt đầu đặt phòng (ISO 8601 format)
		/// </summary>
		public DateTime StartTime { get; set; }
		
		/// <summary>
		/// Thời gian kết thúc đặt phòng (ISO 8601 format)
		/// </summary>
		public DateTime EndTime { get; set; }
		
		/// <summary>
		/// Mục đích sử dụng phòng
		/// </summary>
		public string Purpose { get; set; } = null!;
		
		/// <summary>
		/// ID của sự kiện liên quan (tùy chọn)
		/// </summary>
		public Guid? EventId { get; set; }
		
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
		/// Trạng thái booking (0=Pending, 1=Approved, 2=Rejected, 3=Cancelled, 4=Completed)
		/// </summary>
		public BookingStatus? Status { get; set; }
		
		public DateTime? From { get; set; }
		public DateTime? To { get; set; }
		public int? Page { get; set; }
		public int? PageSize { get; set; }
	}
}


