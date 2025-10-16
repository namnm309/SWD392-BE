using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using DomainLayer.Enum;

namespace DomainLayer.Entities
{
	[Table("tbl_labs")]
	public class Lab : BaseEntity
	{
		public string Name { get; set; } = null!;

		public string? Description { get; set; }

		public string? Location { get; set; }

		public int Capacity { get; set; }

		public LabStatus Status { get; set; } = LabStatus.Active;

		public Guid? RoomId { get; set; }

		[ForeignKey(nameof(RoomId))]
		public Room? Room { get; set; }

		public ICollection<LabMember> Members { get; set; } = new List<LabMember>();
	}
}


