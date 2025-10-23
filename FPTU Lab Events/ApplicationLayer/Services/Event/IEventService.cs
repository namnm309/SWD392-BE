using Application.DTOs.Event;
using DomainLayer.Enum;

namespace Application.Services.Event
{
    public interface IEventService
    {
        Task<IReadOnlyList<EventListItem>> GetAllEventsAsync(EventFilterRequest? filter = null);
        Task<EventDetail> GetEventByIdAsync(Guid id);
        Task<EventDetail> CreateEventAsync(CreateEventRequest request, Guid adminId);
        Task<EventDetail> UpdateEventAsync(Guid id, UpdateEventRequest request, Guid adminId);
        Task DeleteEventAsync(Guid id, DeleteEventRequest request, Guid adminId);
        
        // Utility functions
        Task<IReadOnlyList<EventListItem>> GetUpcomingEventsAsync();
        Task<IReadOnlyList<EventListItem>> GetEventsByDateRangeAsync(DateTime startDate, DateTime endDate);
        Task<int> GetEventCountAsync();
        Task<int> GetActiveEventCountAsync();
    }
}
