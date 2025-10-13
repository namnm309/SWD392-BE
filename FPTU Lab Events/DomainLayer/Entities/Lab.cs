using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace DomainLayer.Entities
{
	[Table("tbl_labs")]
	public class Lab : BaseEntity
	{
		public string Name { get; set; } = null!;

		public string? Description { get; set; }

		public string? Location { get; set; }

		public ICollection<LabMember> Members { get; set; } = new List<LabMember>();
	}
}


