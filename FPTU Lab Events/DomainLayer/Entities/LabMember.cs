using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace DomainLayer.Entities
{
	public enum LabMemberRole
	{
		Member = 0,
		Manager = 1
	}

	public enum LabMemberStatus
	{
		Active = 0,
		Inactive = 1,
		Left = 2
	}

	[Table("tbl_lab_members")]
	public class LabMember : BaseEntity
	{
		public Guid LabId { get; set; }

		[ForeignKey(nameof(LabId))]
		public Lab Lab { get; set; } = null!;

		public Guid UserId { get; set; }

		[ForeignKey(nameof(UserId))]
		public Users User { get; set; } = null!;

		public LabMemberRole Role { get; set; } = LabMemberRole.Member;

		public LabMemberStatus Status { get; set; } = LabMemberStatus.Active;

		public DateTime JoinedAt { get; set; }

		public DateTime? LeftAt { get; set; }
	}
}


