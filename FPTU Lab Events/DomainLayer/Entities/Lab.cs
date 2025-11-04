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

		public string? Location { get; set; }

		public LabStatus Status { get; set; } = LabStatus.Active;

		public ICollection<Room> Rooms { get; set; } = new List<Room>();

		public ICollection<LabMember> Members { get; set; } = new List<LabMember>();
	}
}


