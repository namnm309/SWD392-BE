using System;
using System.ComponentModel.DataAnnotations.Schema;
using DomainLayer.Enum;

namespace DomainLayer.Entities
{
    [Table("tbl_equipments")]
    public class Equipment : BaseEntity
    {
        public string Name { get; set; } = null!;
        
        public string Description { get; set; } = null!;
        
        public string SerialNumber { get; set; } = null!;
        
        public EquipmentType Type { get; set; }
        
        public EquipmentStatus Status { get; set; } = EquipmentStatus.Available;
        
        public string? ImageUrl { get; set; }
        
        public Guid? RoomId { get; set; }
        
        [ForeignKey(nameof(RoomId))]
        public Room? Room { get; set; }
        
        public DateTime? LastMaintenanceDate { get; set; }
        
        public DateTime? NextMaintenanceDate { get; set; }
    }
}
