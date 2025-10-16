using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using DomainLayer.Enum;

namespace DomainLayer.Entities
{
	[Table("tbl_events")]
	public class Event : BaseEntity
	{
		public string Title { get; set; } = null!;

		public string? Description { get; set; }

		public DateTime StartDate { get; set; }

		public DateTime EndDate { get; set; }

		public string? Location { get; set; }

		public EventStatus Status { get; set; } = EventStatus.Active;

		public bool Visibility { get; set; } = true;

		public string? RecurrenceRule { get; set; }

		public Guid CreatedBy { get; set; }

		[ForeignKey(nameof(CreatedBy))]
		public Users CreatedByUser { get; set; } = null!;

		public ICollection<Booking> Bookings { get; set; } = new List<Booking>();
	}
}


