using System;
using DomainLayer.Enum;

namespace Application.DTOs.Lab
{
    public class LabListItem
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = null!;
        public string? Description { get; set; }
        public string? Location { get; set; }
        public int Capacity { get; set; }
        public string Status { get; set; } = null!;
        public Guid? RoomId { get; set; }
        public string? RoomName { get; set; }
        public int MemberCount { get; set; }
        public int EquipmentCount { get; set; }
        public int ActiveBookings { get; set; }
    }

    public class LabDetail : LabListItem
    {
        public DateTime CreatedAt { get; set; }
        public DateTime LastUpdatedAt { get; set; }
        public RoomInfo? Room { get; set; }
        public List<LabMemberInfo> Members { get; set; } = new List<LabMemberInfo>();
        public List<EquipmentInfo> Equipments { get; set; } = new List<EquipmentInfo>();
        public List<BookingInfo> RecentBookings { get; set; } = new List<BookingInfo>();
    }

    public class LabMemberInfo
    {
        public Guid Id { get; set; }
        public Guid UserId { get; set; }
        public string UserName { get; set; } = null!;
        public string UserEmail { get; set; } = null!;
        public string Role { get; set; } = null!;
        public string Status { get; set; } = null!;
        public DateTime JoinedAt { get; set; }
    }

    public class EquipmentInfo
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = null!;
        public string Type { get; set; } = null!;
        public string Status { get; set; } = null!;
        public string? SerialNumber { get; set; }
    }

    public class BookingInfo
    {
        public Guid Id { get; set; }
        public string UserName { get; set; } = null!;
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public string Status { get; set; } = null!;
        public string Purpose { get; set; } = null!;
    }

    public class RoomInfo
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = null!;
        public string Description { get; set; } = null!;
        public string Location { get; set; } = null!;
        public int Capacity { get; set; }
        public string Status { get; set; } = null!;
    }

    public class CreateLabRequest
    {
        public string Name { get; set; } = null!;
        public string? Description { get; set; }
        public string? Location { get; set; }
        public int Capacity { get; set; }
        public Guid? RoomId { get; set; }
        public LabStatus Status { get; set; } = LabStatus.Active;
    }

    public class UpdateLabRequest
    {
        public string? Name { get; set; }
        public string? Description { get; set; }
        public string? Location { get; set; }
        public int? Capacity { get; set; }
        public Guid? RoomId { get; set; }
        public LabStatus? Status { get; set; }
    }

    public class UpdateLabStatusRequest
    {
        public LabStatus Status { get; set; }
    }

    public class LabFilterRequest
    {
        public string? Name { get; set; }
        public string? Location { get; set; }
        public LabStatus? Status { get; set; }
        public int? MinCapacity { get; set; }
        public int? MaxCapacity { get; set; }
        public int? Page { get; set; }
        public int? PageSize { get; set; }
    }

    public class DeleteLabRequest
    {
        public bool ConfirmDeletion { get; set; }
    }

    public class LabLogInfo
    {
        public Guid AdminId { get; set; }
        public string AdminName { get; set; } = null!;
        public Guid LabId { get; set; }
        public string LabName { get; set; } = null!;
        public string Action { get; set; } = null!; // Create, Update, Delete
        public DateTime Timestamp { get; set; }
        public string? Changes { get; set; } // For update operations
    }
}
