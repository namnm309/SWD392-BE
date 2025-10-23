using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace DomainLayer.Entities
{
	[Table("tbl_booking_applies")]
	public class BookingApply : BaseEntity
	{
		public Guid BookingId { get; set; }

		[ForeignKey(nameof(BookingId))]
		public Booking Booking { get; set; } = null!;

		public Guid? RoomSlotId { get; set; }

		[ForeignKey(nameof(RoomSlotId))]
		public RoomSlot? RoomSlot { get; set; }

		public string? Status { get; set; }

		public string? Note { get; set; }
	}
}


