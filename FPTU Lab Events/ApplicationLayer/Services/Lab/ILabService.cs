using Application.DTOs.Lab;
using DomainLayer.Enum;

namespace Application.Services.Lab
{
    public interface ILabService
    {
        Task<IReadOnlyList<LabListItem>> GetAllLabsAsync(LabFilterRequest? filter = null);
        Task<LabDetail> GetLabByIdAsync(Guid id);
        Task<LabDetail> CreateLabAsync(CreateLabRequest request, Guid adminId);
        Task<LabDetail> UpdateLabAsync(Guid id, UpdateLabRequest request, Guid adminId);
        Task<LabDetail> UpdateLabStatusAsync(Guid id, UpdateLabStatusRequest request);
        Task DeleteLabAsync(Guid id, DeleteLabRequest request, Guid adminId);
        
        // Utility functions
        Task<IReadOnlyList<LabListItem>> GetAvailableLabsAsync();
        Task<bool> IsLabAvailableAsync(Guid labId);
        Task<int> GetLabCountAsync();
        Task<int> GetActiveLabCountAsync();
    }
}
