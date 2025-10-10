using System;
using DomainLayer.Enum;

namespace Application.DTOs.Notification
{
    public class NotificationListItem
    {
        public Guid Id { get; set; }
        public string Title { get; set; } = null!;
        public string Content { get; set; } = null!;
        public string TargetGroup { get; set; } = null!;
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public string Status { get; set; } = null!;
        public DateTime CreatedAt { get; set; }
        public string CreatedBy { get; set; } = null!;
        public bool IsRead { get; set; }
    }

    public class NotificationDetail : NotificationListItem
    {
        public string CreatedByFullName { get; set; } = null!;
        public int TotalReaders { get; set; }
        public int UnreadCount { get; set; }
    }

    public class CreateNotificationRequest
    {
        public string Title { get; set; } = null!;
        public string Content { get; set; } = null!;
        public string TargetGroup { get; set; } = null!; // All, Lecturer, Student
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
    }

    public class UpdateNotificationRequest
    {
        public string? Title { get; set; }
        public string? Content { get; set; }
        public string? TargetGroup { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
    }

    public class NotificationFilterRequest
    {
        public string? TargetGroup { get; set; }
        public string? Status { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public int? Page { get; set; }
        public int? PageSize { get; set; }
    }

    public class MarkAsReadRequest
    {
        public Guid NotificationId { get; set; }
    }
}
