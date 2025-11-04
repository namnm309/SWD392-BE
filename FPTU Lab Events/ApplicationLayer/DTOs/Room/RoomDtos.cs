using System;
using DomainLayer.Enum;

namespace Application.DTOs.Room
{
    public class RoomListItem
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = null!;
        public int Capacity { get; set; }
        public string Status { get; set; } = null!;
        public Guid? LabId { get; set; }
        public string? LabName { get; set; }
        public int EquipmentCount { get; set; }
        public int ActiveBookings { get; set; }
    }

    public class RoomDetail : RoomListItem
    {
        public DateTime CreatedAt { get; set; }
        public DateTime LastUpdatedAt { get; set; }
        public List<EquipmentInfo> Equipments { get; set; } = new List<EquipmentInfo>();
        public List<BookingInfo> RecentBookings { get; set; } = new List<BookingInfo>();
        public List<RoomSlotInfo> RoomSlots { get; set; } = new List<RoomSlotInfo>();
    }

    public class RoomSlotInfo
    {
        public Guid Id { get; set; }
        public DateTime Date { get; set; } // Ngày cụ thể (2025-10-20)
        public string DateFormatted { get; set; } = null!; // "20/10/2025" hoặc "Mon, 20 Oct 2025"
        public int SlotNumber { get; set; } // 1-8
        public int DayOfWeek { get; set; } // 0-6 (0=Sunday, 1=Monday, etc.)
        public string DayOfWeekName { get; set; } = null!; // "Monday", "Tuesday", etc.
        public TimeOnly StartTime { get; set; }
        public TimeOnly EndTime { get; set; }
        public string TimeRange { get; set; } = null!; // "12:30-14:45"
        public Guid? EventId { get; set; }
        public string? EventTitle { get; set; }
        public string? EventCode { get; set; } // Course code like "SWD392", "PRN222"
        public string? Status { get; set; } // "attended", "absent", "pending", null
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
        public int Capacity { get; set; }
        public Guid? LabId { get; set; }
    }

    public class UpdateRoomRequest
    {
        public string? Name { get; set; }
        public int? Capacity { get; set; }
        public Guid? LabId { get; set; }
    }

    public class UpdateRoomStatusRequest
    {
        public RoomStatus Status { get; set; }
    }

    public class RoomFilterRequest
    {
        public string? Name { get; set; }
        public RoomStatus? Status { get; set; }
        public int? MinCapacity { get; set; }
        public int? MaxCapacity { get; set; }
        public int? Page { get; set; }
        public int? PageSize { get; set; }
    }

    // RoomSlot DTOs
    public class CreateRoomSlotRequest
    {
        /// <summary>
        /// ID của room để tạo slot
        /// </summary>
        public Guid RoomId { get; set; }
        
        /// <summary>
        /// Ngày cụ thể của slot (2025-10-20, 2025-10-21, ...)
        /// </summary>
        public DateTime Date { get; set; }
        
        /// <summary>
        /// Số slot (1-8)
        /// </summary>
        public int SlotNumber { get; set; }
        
        /// <summary>
        /// Thời gian bắt đầu (ví dụ: "07:00:00")
        /// </summary>
        public TimeOnly StartTime { get; set; }
        
        /// <summary>
        /// Thời gian kết thúc (ví dụ: "09:00:00")
        /// </summary>
        public TimeOnly EndTime { get; set; }
        
        /// <summary>
        /// ID của event (tùy chọn - có thể gán sau)
        /// </summary>
        public Guid? EventId { get; set; }
        
        /// <summary>
        /// Trạng thái của slot (tùy chọn)
        /// </summary>
        public string? Status { get; set; }
    }

    public class UpdateRoomSlotRequest
    {
        /// <summary>
        /// ID của room mới (tùy chọn - để chuyển slot sang room khác)
        /// </summary>
        public Guid? RoomId { get; set; }
        
        /// <summary>
        /// Ngày mới (tùy chọn)
        /// </summary>
        public DateTime? Date { get; set; }
        
        /// <summary>
        /// Số slot mới (tùy chọn)
        /// </summary>
        public int? SlotNumber { get; set; }
        
        /// <summary>
        /// Thời gian bắt đầu mới (tùy chọn)
        /// </summary>
        public TimeOnly? StartTime { get; set; }
        
        /// <summary>
        /// Thời gian kết thúc mới (tùy chọn)
        /// </summary>
        public TimeOnly? EndTime { get; set; }
        
        /// <summary>
        /// ID của event (tùy chọn - để gán hoặc bỏ gán event)
        /// </summary>
        public Guid? EventId { get; set; }
        
        /// <summary>
        /// Trạng thái của slot (tùy chọn)
        /// </summary>
        public string? Status { get; set; }
    }

    public class RoomScheduleRequest
    {
        public Guid RoomId { get; set; }
        public DateTime StartDate { get; set; } // Start of week
        public DateTime EndDate { get; set; } // End of week
    }
}
