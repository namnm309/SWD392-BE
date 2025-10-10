using System;
using DomainLayer.Enum;

namespace Application.DTOs.Equipment
{
    public class EquipmentListItem
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = null!;
        public string Description { get; set; } = null!;
        public string SerialNumber { get; set; } = null!;
        public string Type { get; set; } = null!;
        public string Status { get; set; } = null!;
        public string? ImageUrl { get; set; }
        public string? RoomName { get; set; }
        public DateTime? LastMaintenanceDate { get; set; }
        public DateTime? NextMaintenanceDate { get; set; }
    }

    public class EquipmentDetail : EquipmentListItem
    {
        public Guid? RoomId { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime LastUpdatedAt { get; set; }
    }

    public class CreateEquipmentRequest
    {
        public string Name { get; set; } = null!;
        public string Description { get; set; } = null!;
        public string SerialNumber { get; set; } = null!;
        public EquipmentType Type { get; set; }
        public string? ImageUrl { get; set; }
        public Guid? RoomId { get; set; }
        public DateTime? LastMaintenanceDate { get; set; }
        public DateTime? NextMaintenanceDate { get; set; }
    }

    public class UpdateEquipmentRequest
    {
        public string? Name { get; set; }
        public string? Description { get; set; }
        public string? SerialNumber { get; set; }
        public EquipmentType? Type { get; set; }
        public string? ImageUrl { get; set; }
        public Guid? RoomId { get; set; }
        public DateTime? LastMaintenanceDate { get; set; }
        public DateTime? NextMaintenanceDate { get; set; }
    }

    public class UpdateEquipmentStatusRequest
    {
        public EquipmentStatus Status { get; set; }
    }

    public class EquipmentFilterRequest
    {
        public string? Name { get; set; }
        public string? SerialNumber { get; set; }
        public EquipmentType? Type { get; set; }
        public EquipmentStatus? Status { get; set; }
        public Guid? RoomId { get; set; }
        public int? Page { get; set; }
        public int? PageSize { get; set; }
    }
}
