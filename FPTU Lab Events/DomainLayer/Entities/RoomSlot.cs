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

		public int DayOfWeek { get; set; }

		public TimeOnly StartTime { get; set; }

		public TimeOnly EndTime { get; set; }
	}
}


