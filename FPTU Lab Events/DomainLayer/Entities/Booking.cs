using System;
using System.ComponentModel.DataAnnotations.Schema;
using DomainLayer.Enum;

namespace DomainLayer.Entities
{
    [Table("tbl_bookings")]
    public class Booking : BaseEntity
    {
        public Guid UserId { get; set; }
        
        [ForeignKey(nameof(UserId))]
        public Users User { get; set; } = null!;
        
        public Guid RoomId { get; set; }
        
        [ForeignKey(nameof(RoomId))]
        public Room Room { get; set; } = null!;
        
        public DateTime StartTime { get; set; }
        
        public DateTime EndTime { get; set; }
        
        public string Purpose { get; set; } = null!;
        
        public BookingStatus Status { get; set; } = BookingStatus.Pending;
        
        public string? Notes { get; set; }
    }
}
