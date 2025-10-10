using System;
using DomainLayer.Enum;

namespace Application.DTOs.Room
{
    public class RoomListItem
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = null!;
        public string Description { get; set; } = null!;
        public string Location { get; set; } = null!;
        public int Capacity { get; set; }
        public string Status { get; set; } = null!;
        public string? ImageUrl { get; set; }
        public int EquipmentCount { get; set; }
        public int ActiveBookings { get; set; }
    }

    public class RoomDetail : RoomListItem
    {
        public DateTime CreatedAt { get; set; }
        public DateTime LastUpdatedAt { get; set; }
        public List<EquipmentInfo> Equipments { get; set; } = new List<EquipmentInfo>();
        public List<BookingInfo> RecentBookings { get; set; } = new List<BookingInfo>();
    }

    public class EquipmentInfo
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = null!;
        public string Type { get; set; } = null!;
        public string Status { get; set; } = null!;
    }

    public class BookingInfo
    {
        public Guid Id { get; set; }
        public string UserName { get; set; } = null!;
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public string Status { get; set; } = null!;
    }

    public class CreateRoomRequest
    {
        public string Name { get; set; } = null!;
        public string Description { get; set; } = null!;
        public string Location { get; set; } = null!;
        public int Capacity { get; set; }
        public string? ImageUrl { get; set; }
    }

    public class UpdateRoomRequest
    {
        public string? Name { get; set; }
        public string? Description { get; set; }
        public string? Location { get; set; }
        public int? Capacity { get; set; }
        public string? ImageUrl { get; set; }
    }

    public class UpdateRoomStatusRequest
    {
        public RoomStatus Status { get; set; }
    }

    public class RoomFilterRequest
    {
        public string? Name { get; set; }
        public string? Location { get; set; }
        public RoomStatus? Status { get; set; }
        public int? MinCapacity { get; set; }
        public int? MaxCapacity { get; set; }
        public int? Page { get; set; }
        public int? PageSize { get; set; }
    }
}
