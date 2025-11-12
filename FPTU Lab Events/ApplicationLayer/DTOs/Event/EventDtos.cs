using System;
using DomainLayer.Enum;

namespace Application.DTOs.Event
{
    public class EventListItem
    {
        public Guid Id { get; set; }
        public string Title { get; set; } = null!;
        public string? Description { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public string Status { get; set; } = null!;
        public bool Visibility { get; set; }
        public string CreatedBy { get; set; } = null!;
        public int BookingCount { get; set; }
        public bool IsUpcoming { get; set; }
        public int Capacity { get; set; }
        public string? ImageUrl { get; set; }
    }

    public class EventDetail : EventListItem
    {
        public DateTime CreatedAt { get; set; }
        public DateTime LastUpdatedAt { get; set; }
        public List<BookingInfo> Bookings { get; set; } = new List<BookingInfo>();
        public Guid? LabId { get; set; }
        public string? LabName { get; set; }
        public Guid? RoomId { get; set; }
        public string? RoomName { get; set; }
        public List<EventRoomSlotInfo> RoomSlots { get; set; } = new List<EventRoomSlotInfo>();
    }

    public class EventRoomSlotInfo
    {
        public Guid Id { get; set; }
        public Guid RoomId { get; set; }
        public string RoomName { get; set; } = null!;
        public DateTime Date { get; set; }
        public string DateFormatted { get; set; } = null!;
        public int SlotNumber { get; set; }
        public string DayOfWeekName { get; set; } = null!;
        public string TimeRange { get; set; } = null!;
        public string? Status { get; set; }
    }

    public class BookingInfo
    {
        public Guid Id { get; set; }
        public string UserName { get; set; } = null!;
        public string RoomName { get; set; } = null!;
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public string Status { get; set; } = null!;
        public string Purpose { get; set; } = null!;
    }

    public class CreateEventRequest
    {
        public string Title { get; set; } = null!;
        public string? Description { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public EventStatus Status { get; set; } = EventStatus.Active;
        public bool Visibility { get; set; } = true;
        public int Capacity { get; set; }
        public string? ImageUrl { get; set; }
        
        // Lab, Room & RoomSlot selection (cascade: Lab -> Room -> RoomSlot)
        public Guid? LabId { get; set; } // Optional: Chọn lab trước
        public Guid? RoomId { get; set; } // Optional: Chọn phòng (phải thuộc lab nếu có LabId)
        public List<Guid>? RoomSlotIds { get; set; } // Optional: Chọn các slot trong phòng (phải thuộc room nếu có RoomId)
    }

    public class UpdateEventRequest
    {
        public string? Title { get; set; }
        public string? Description { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public EventStatus? Status { get; set; }
        public bool? Visibility { get; set; }
        public int? Capacity { get; set; }
        public string? ImageUrl { get; set; }
    }

    public class EventFilterRequest
    {
        public string? Title { get; set; }
        public EventStatus? Status { get; set; }
        public DateTime? StartDateFrom { get; set; }
        public DateTime? StartDateTo { get; set; }
        public bool? IsUpcoming { get; set; }
        public int? Page { get; set; }
        public int? PageSize { get; set; }
    }

    public class DeleteEventRequest
    {
        public bool ConfirmDeletion { get; set; }
    }

    public class ApproveEventRequest
    {
        public string? ApprovalNote { get; set; }
    }

    public class RejectEventRequest
    {
        public string RejectionReason { get; set; } = null!;
    }

    public class EventLogInfo
    {
        public Guid AdminId { get; set; }
        public string AdminName { get; set; } = null!;
        public Guid EventId { get; set; }
        public string EventTitle { get; set; } = null!;
        public string Action { get; set; } = null!; // Create, Update, Delete, Approve, Reject
        public DateTime Timestamp { get; set; }
        public string? Changes { get; set; } // For update operations
    }
}
