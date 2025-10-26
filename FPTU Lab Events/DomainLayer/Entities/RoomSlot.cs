using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace DomainLayer.Entities
{
	[Table("tbl_room_slots")]
	public class RoomSlot : BaseEntity
	{
		public Guid RoomId { get; set; }

		[ForeignKey(nameof(RoomId))]
		public Room Room { get; set; } = null!;

		public DateTime Date { get; set; } // Ngày cụ thể của slot (2025-10-20, 2025-10-21, ...)

		public int SlotNumber { get; set; } // 1-8 (Slot 1 -> Slot 8)

		public int DayOfWeek { get; set; } // 0 = Sunday, 1 = Monday, ..., 6 = Saturday (computed from Date)

		public TimeOnly StartTime { get; set; }

		public TimeOnly EndTime { get; set; }

		public Guid? EventId { get; set; } // Nullable - có thể chưa có event

		[ForeignKey(nameof(EventId))]
		public Event? Event { get; set; }

		public string? Status { get; set; } // "attended", "absent", "pending", null
	}
}


