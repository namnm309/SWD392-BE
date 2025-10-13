using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace DomainLayer.Entities
{
	[Table("tbl_events")]
	public class Event : BaseEntity
	{
		public Guid OrganizationId { get; set; }

		public string Title { get; set; } = null!;

		public string? Description { get; set; }

		public bool Visibility { get; set; } = true;

		public string? RecurrenceRule { get; set; }

		public DateTime? Deadline { get; set; }

		public ICollection<Booking> Bookings { get; set; } = new List<Booking>();
	}
}


