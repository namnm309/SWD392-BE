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
		public Guid RoomId { get; set; }
		public DateTime StartTime { get; set; }
		public DateTime EndTime { get; set; }
		public string Purpose { get; set; } = null!;
		public Guid? EventId { get; set; }
		public string? Notes { get; set; }
	}

	public class UpdateBookingStatusRequest
	{
		public BookingStatus Status { get; set; }
		public string? Notes { get; set; }
	}

	public class BookingFilterRequest
	{
		public Guid? RoomId { get; set; }
		public Guid? UserId { get; set; }
		public BookingStatus? Status { get; set; }
		public DateTime? From { get; set; }
		public DateTime? To { get; set; }
		public int? Page { get; set; }
		public int? PageSize { get; set; }
	}
}


