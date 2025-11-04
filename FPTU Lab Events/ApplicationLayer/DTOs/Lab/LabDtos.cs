using System;
using DomainLayer.Enum;

namespace Application.DTOs.Lab
{
    public class LabListItem
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = null!;
        public string? Location { get; set; }
        public string Status { get; set; } = null!;
        public int RoomCount { get; set; }
        public int MemberCount { get; set; }
        public int EquipmentCount { get; set; }
        public int ActiveBookings { get; set; }
    }

    public class LabDetail : LabListItem
    {
        public DateTime CreatedAt { get; set; }
        public DateTime LastUpdatedAt { get; set; }
        public List<RoomInfo> Rooms { get; set; } = new List<RoomInfo>();
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
        public int Capacity { get; set; }
        public string Status { get; set; } = null!;
    }

    public class CreateLabRequest
    {
        /// <summary>
        /// Tên của lab
        /// </summary>
        public string Name { get; set; } = null!;
        
        /// <summary>
        /// Địa điểm của lab (tùy chọn)
        /// </summary>
        public string? Location { get; set; }
        
        /// <summary>
        /// Trạng thái của lab (mặc định: Active)
        /// </summary>
        public LabStatus Status { get; set; } = LabStatus.Active;
        
        /// <summary>
        /// Danh sách ID các room muốn gán vào lab (tùy chọn)
        /// </summary>
        public List<Guid>? RoomIds { get; set; }
    }

    public class UpdateLabRequest
    {
        public string? Name { get; set; }
        public string? Location { get; set; }
        public LabStatus? Status { get; set; }
        
        /// <summary>
        /// Danh sách ID các room muốn gán vào lab (tùy chọn). Nếu null thì không thay đổi, nếu có giá trị thì sẽ thay thế toàn bộ danh sách rooms hiện tại.
        /// </summary>
        public List<Guid>? RoomIds { get; set; }
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
