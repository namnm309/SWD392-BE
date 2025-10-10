using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using DomainLayer.Enum;

namespace DomainLayer.Entities
{
    [Table("tbl_rooms")]
    public class Room : BaseEntity
    {
        public string Name { get; set; } = null!;
        
        public string Description { get; set; } = null!;
        
        public string Location { get; set; } = null!;
        
        public int Capacity { get; set; }
        
        public RoomStatus Status { get; set; } = RoomStatus.Available;
        
        public string? ImageUrl { get; set; }
        
        public ICollection<Booking> Bookings { get; set; } = new List<Booking>();
        
        public ICollection<Equipment> Equipments { get; set; } = new List<Equipment>();
    }
}
