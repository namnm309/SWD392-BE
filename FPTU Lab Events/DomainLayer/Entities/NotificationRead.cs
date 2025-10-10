using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace DomainLayer.Entities
{
    [Table("tbl_notification_reads")]
    public class NotificationRead : BaseEntity
    {
        public Guid NotificationId { get; set; }
        
        [ForeignKey(nameof(NotificationId))]
        public Notification Notification { get; set; } = null!;
        
        public Guid UserId { get; set; }
        
        [ForeignKey(nameof(UserId))]
        public Users User { get; set; } = null!;
        
        public DateTime ReadAt { get; set; }
    }
}
