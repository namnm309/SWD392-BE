using Application.DTOs.Equipment;
using DomainLayer.Enum;

namespace Application.Services.Equipment
{
    public interface IEquipmentService
    {
        Task<IReadOnlyList<EquipmentListItem>> GetAllEquipmentsAsync(EquipmentFilterRequest? filter = null);
        Task<EquipmentDetail> GetEquipmentByIdAsync(Guid id);
        Task<EquipmentDetail> CreateEquipmentAsync(CreateEquipmentRequest request);
        Task<EquipmentDetail> UpdateEquipmentAsync(Guid id, UpdateEquipmentRequest request);
        Task<EquipmentDetail> UpdateEquipmentStatusAsync(Guid id, UpdateEquipmentStatusRequest request);
        Task DeleteEquipmentAsync(Guid id);
        
        // Utility functions
        Task<IReadOnlyList<EquipmentListItem>> GetEquipmentsByRoomAsync(Guid roomId);
        Task<IReadOnlyList<EquipmentListItem>> GetAvailableEquipmentsAsync();
        Task<int> GetEquipmentCountAsync();
        Task<int> GetAvailableEquipmentCountAsync();
        Task<IReadOnlyList<EquipmentListItem>> GetEquipmentsNeedingMaintenanceAsync();
    }
}
