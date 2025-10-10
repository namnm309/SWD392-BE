using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using DomainLayer.Enum;

namespace DomainLayer.Entities
{
    [Table("tbl_notifications")]
    public class Notification : BaseEntity
    {
        public string Title { get; set; } = null!;
        
        public string Content { get; set; } = null!;
        
        public string TargetGroup { get; set; } = null!; // All, Lecturer, Student
        
        public DateTime StartDate { get; set; }
        
        public DateTime EndDate { get; set; }
        
        public NotificationStatus Status { get; set; } = NotificationStatus.Active;
        
        public Guid CreatedBy { get; set; }
        
        [ForeignKey(nameof(CreatedBy))]
        public Users CreatedByUser { get; set; } = null!;
        
        public ICollection<NotificationRead> NotificationReads { get; set; } = new List<NotificationRead>();
    }
}
