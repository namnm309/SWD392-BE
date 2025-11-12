using Application.DTOs.Event;
using DomainLayer.Enum;

namespace Application.Services.Event
{
    public interface IEventService
    {
        Task<IReadOnlyList<EventListItem>> GetAllEventsAsync(EventFilterRequest? filter = null);
        Task<EventDetail> GetEventByIdAsync(Guid id);
        Task<EventDetail> CreateEventAsync(CreateEventRequest request, Guid userId, string userRole);
        Task<EventDetail> UpdateEventAsync(Guid id, UpdateEventRequest request, Guid adminId);
        Task DeleteEventAsync(Guid id, DeleteEventRequest request, Guid adminId);
        
        // Event approval (Staff only)
        Task<EventDetail> ApproveEventAsync(Guid eventId, Guid staffId, string? approvalNote = null);
        Task<EventDetail> RejectEventAsync(Guid eventId, Guid staffId, string rejectionReason);
        
        // Utility functions
        Task<IReadOnlyList<EventListItem>> GetUpcomingEventsAsync();
        Task<IReadOnlyList<EventListItem>> GetEventsByDateRangeAsync(DateTime startDate, DateTime endDate);
        Task<IReadOnlyList<EventListItem>> GetEventsByUserIdAsync(Guid userId);
        Task<IReadOnlyList<EventListItem>> GetPendingEventsAsync();
        Task<int> GetEventCountAsync();
        Task<int> GetActiveEventCountAsync();
        Task<int> GetPendingEventCountAsync();
    }
}
