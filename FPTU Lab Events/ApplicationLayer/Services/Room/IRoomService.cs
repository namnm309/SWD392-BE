using Application.DTOs.Room;
using DomainLayer.Enum;

namespace Application.Services.Room
{
    public interface IRoomService
    {
        // Room management
        Task<IReadOnlyList<RoomListItem>> GetAllRoomsAsync(RoomFilterRequest? filter = null);
        Task<RoomDetail> GetRoomByIdAsync(Guid id);
        Task<RoomDetail> CreateRoomAsync(CreateRoomRequest request);
        Task<RoomDetail> UpdateRoomAsync(Guid id, UpdateRoomRequest request);
        Task<RoomDetail> UpdateRoomStatusAsync(Guid id, UpdateRoomStatusRequest request);
        Task DeleteRoomAsync(Guid id);
        
        // Room utility functions
        Task<IReadOnlyList<RoomListItem>> GetAvailableRoomsAsync(DateTime startTime, DateTime endTime);
        Task<bool> IsRoomAvailableAsync(Guid roomId, DateTime startTime, DateTime endTime);
        Task<int> GetRoomCountAsync();
        Task<int> GetAvailableRoomCountAsync();

        // RoomSlot management
        Task<RoomSlotInfo> GetRoomSlotByIdAsync(Guid slotId);
        Task<IReadOnlyList<RoomSlotInfo>> GetRoomSlotsByRoomIdAsync(Guid roomId);
        Task<IReadOnlyList<RoomSlotInfo>> GetRoomSlotsByDateRangeAsync(Guid roomId, DateTime startDate, DateTime endDate);
        Task<IReadOnlyList<RoomSlotInfo>> GetAvailableRoomSlotsAsync(Guid roomId, DateTime? startDate = null, DateTime? endDate = null);
        Task<RoomSlotInfo> CreateRoomSlotAsync(CreateRoomSlotRequest request);
        Task<RoomSlotInfo> UpdateRoomSlotAsync(Guid slotId, UpdateRoomSlotRequest request);
        Task DeleteRoomSlotAsync(Guid slotId);
        Task<IReadOnlyList<RoomSlotInfo>> GenerateWeeklyRoomSlotsAsync(Guid roomId, DateTime weekStartDate);
    }
}
