using Application.DTOs.Room;
using DomainLayer.Enum;

namespace Application.Services.Room
{
    public interface IRoomService
    {
        Task<IReadOnlyList<RoomListItem>> GetAllRoomsAsync(RoomFilterRequest? filter = null);
        Task<RoomDetail> GetRoomByIdAsync(Guid id);
        Task<RoomDetail> CreateRoomAsync(CreateRoomRequest request);
        Task<RoomDetail> UpdateRoomAsync(Guid id, UpdateRoomRequest request);
        Task<RoomDetail> UpdateRoomStatusAsync(Guid id, UpdateRoomStatusRequest request);
        Task DeleteRoomAsync(Guid id);
        
        // Utility functions
        Task<IReadOnlyList<RoomListItem>> GetAvailableRoomsAsync(DateTime startTime, DateTime endTime);
        Task<bool> IsRoomAvailableAsync(Guid roomId, DateTime startTime, DateTime endTime);
        Task<int> GetRoomCountAsync();
        Task<int> GetAvailableRoomCountAsync();
    }
}
