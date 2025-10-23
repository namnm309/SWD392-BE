using System;
using DomainLayer.Entities;

namespace Application.DTOs.User
{
	public class LabMemberListItem
	{
		public Guid Id { get; set; }
		public Guid LabId { get; set; }
		public string LabName { get; set; } = null!;
		public Guid UserId { get; set; }
		public string Fullname { get; set; } = null!;
		public LabMemberRole Role { get; set; }
		public LabMemberStatus Status { get; set; }
		public DateTime JoinedAt { get; set; }
	}

	public class LabMemberDetail : LabMemberListItem
	{
		public DateTime? LeftAt { get; set; }
	}

	public class CreateLabMemberRequest
	{
		public Guid LabId { get; set; }
		public Guid UserId { get; set; }
		public LabMemberRole Role { get; set; } = LabMemberRole.Member;
	}

	public class UpdateLabMemberRequest
	{
		public LabMemberRole? Role { get; set; }
		public LabMemberStatus? Status { get; set; }
		public DateTime? LeftAt { get; set; }
	}
}


