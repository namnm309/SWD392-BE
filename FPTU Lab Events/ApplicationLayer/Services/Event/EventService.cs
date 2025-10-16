using Application.DTOs.Event;
using DomainLayer.Entities;
using DomainLayer.Enum;
using InfrastructureLayer.Data;
using Microsoft.EntityFrameworkCore;

namespace Application.Services.Event
{
    public class EventService : IEventService
    {
        private readonly LabDbContext _db;

        public EventService(LabDbContext db)
        {
            _db = db;
        }

        public async Task<IReadOnlyList<EventListItem>> GetAllEventsAsync(EventFilterRequest? filter = null)
        {
            var query = _db.Events
                .Include(e => e.CreatedByUser)
                .Include(e => e.Bookings)
                .AsQueryable();

            if (filter != null)
            {
                if (!string.IsNullOrEmpty(filter.Title))
                    query = query.Where(e => e.Title.Contains(filter.Title));
                
                if (!string.IsNullOrEmpty(filter.Location))
                    query = query.Where(e => e.Location != null && e.Location.Contains(filter.Location));
                
                if (filter.Status.HasValue)
                    query = query.Where(e => e.Status == filter.Status.Value);
                
                if (filter.StartDateFrom.HasValue)
                    query = query.Where(e => e.StartDate >= filter.StartDateFrom.Value);
                
                if (filter.StartDateTo.HasValue)
                    query = query.Where(e => e.StartDate <= filter.StartDateTo.Value);

                if (filter.IsUpcoming.HasValue && filter.IsUpcoming.Value)
                    query = query.Where(e => e.StartDate > DateTime.UtcNow);
            }

            // AC-04: Display upcoming events at the top
            query = query.OrderBy(e => e.StartDate);

            if (filter?.Page.HasValue == true && filter.PageSize.HasValue)
            {
                query = query.Skip(filter.Page.Value * filter.PageSize.Value)
                           .Take(filter.PageSize.Value);
            }

            var events = await query.ToListAsync();

            return events.Select(e => new EventListItem
            {
                Id = e.Id,
                Title = e.Title,
                Description = e.Description,
                StartDate = e.StartDate,
                EndDate = e.EndDate,
                Location = e.Location,
                Status = e.Status.ToString(),
                Visibility = e.Visibility,
                CreatedBy = e.CreatedByUser.Fullname,
                BookingCount = e.Bookings.Count,
                IsUpcoming = e.StartDate > DateTime.UtcNow
            }).ToList();
        }

        public async Task<EventDetail> GetEventByIdAsync(Guid id)
        {
            var eventEntity = await _db.Events
                .Include(e => e.CreatedByUser)
                .Include(e => e.Bookings)
                    .ThenInclude(b => b.User)
                .Include(e => e.Bookings)
                    .ThenInclude(b => b.Room)
                .FirstOrDefaultAsync(e => e.Id == id)
                ?? throw new Exception("Event not found");

            var bookings = eventEntity.Bookings.Select(b => new BookingInfo
            {
                Id = b.Id,
                UserName = b.User.Fullname,
                RoomName = b.Room.Name,
                StartTime = b.StartTime,
                EndTime = b.EndTime,
                Status = b.Status.ToString(),
                Purpose = b.Purpose
            }).ToList();

            return new EventDetail
            {
                Id = eventEntity.Id,
                Title = eventEntity.Title,
                Description = eventEntity.Description,
                StartDate = eventEntity.StartDate,
                EndDate = eventEntity.EndDate,
                Location = eventEntity.Location,
                Status = eventEntity.Status.ToString(),
                Visibility = eventEntity.Visibility,
                CreatedBy = eventEntity.CreatedByUser.Fullname,
                BookingCount = eventEntity.Bookings.Count,
                IsUpcoming = eventEntity.StartDate > DateTime.UtcNow,
                CreatedAt = eventEntity.CreatedAt,
                LastUpdatedAt = eventEntity.LastUpdatedAt,
                RecurrenceRule = eventEntity.RecurrenceRule,
                Bookings = bookings
            };
        }

        public async Task<EventDetail> CreateEventAsync(CreateEventRequest request, Guid adminId)
        {
            // Validation - AC-02: Required fields cannot be null
            if (string.IsNullOrWhiteSpace(request.Title))
                throw new Exception("Event Title is required");
            
            if (request.StartDate == default)
                throw new Exception("Start Date is required");
            
            if (request.EndDate == default)
                throw new Exception("End Date is required");

            // AC-03: End Date must be after Start Date
            if (request.EndDate <= request.StartDate)
                throw new Exception("End Date must be after Start Date");

            // Check if event title already exists for the same date
            var existingEvent = await _db.Events
                .AnyAsync(e => e.Title == request.Title && e.StartDate.Date == request.StartDate.Date);
            if (existingEvent)
                throw new Exception("Event with this title already exists on the same date");

            var eventEntity = new DomainLayer.Entities.Event
            {
                Id = Guid.NewGuid(),
                Title = request.Title,
                Description = request.Description,
                StartDate = request.StartDate,
                EndDate = request.EndDate,
                Location = request.Location,
                Status = request.Status,
                Visibility = request.Visibility,
                RecurrenceRule = request.RecurrenceRule,
                CreatedBy = adminId,
                CreatedAt = DateTime.UtcNow,
                LastUpdatedAt = DateTime.UtcNow
            };

            _db.Events.Add(eventEntity);
            await _db.SaveChangesAsync();

            // AC-04: Send notification to all users
            await SendEventNotificationAsync(eventEntity.Id, "New Event Created", 
                $"A new event '{eventEntity.Title}' has been created.");

            // Log creation event - AC-06
            await LogEventActionAsync(adminId, eventEntity.Id, eventEntity.Title, "Create", null);

            return await GetEventByIdAsync(eventEntity.Id);
        }

        public async Task<EventDetail> UpdateEventAsync(Guid id, UpdateEventRequest request, Guid adminId)
        {
            var eventEntity = await _db.Events
                .FirstOrDefaultAsync(e => e.Id == id)
                ?? throw new Exception("Event not found");

            var changes = new List<string>();

            // Validation - AC-02: Required fields cannot be null
            if (!string.IsNullOrWhiteSpace(request.Title) && request.Title != eventEntity.Title)
            {
                if (string.IsNullOrWhiteSpace(request.Title))
                    throw new Exception("Event Title cannot be empty");
                
                // Check if new title already exists for the same date
                var existingEvent = await _db.Events
                    .AnyAsync(e => e.Title == request.Title && e.StartDate.Date == eventEntity.StartDate.Date && e.Id != id);
                if (existingEvent)
                    throw new Exception("Event with this title already exists on the same date");
                
                changes.Add($"Title: '{eventEntity.Title}' -> '{request.Title}'");
                eventEntity.Title = request.Title;
            }
            
            if (request.Description != null && request.Description != eventEntity.Description)
            {
                changes.Add($"Description: '{eventEntity.Description}' -> '{request.Description}'");
                eventEntity.Description = request.Description;
            }
            
            if (request.StartDate.HasValue && request.StartDate.Value != eventEntity.StartDate)
            {
                changes.Add($"StartDate: {eventEntity.StartDate} -> {request.StartDate.Value}");
                eventEntity.StartDate = request.StartDate.Value;
            }
            
            if (request.EndDate.HasValue && request.EndDate.Value != eventEntity.EndDate)
            {
                changes.Add($"EndDate: {eventEntity.EndDate} -> {request.EndDate.Value}");
                eventEntity.EndDate = request.EndDate.Value;
            }

            if (request.Location != null && request.Location != eventEntity.Location)
            {
                changes.Add($"Location: '{eventEntity.Location}' -> '{request.Location}'");
                eventEntity.Location = request.Location;
            }

            if (request.Status.HasValue && request.Status.Value != eventEntity.Status)
            {
                changes.Add($"Status: {eventEntity.Status} -> {request.Status.Value}");
                eventEntity.Status = request.Status.Value;
            }

            if (request.Visibility.HasValue && request.Visibility.Value != eventEntity.Visibility)
            {
                changes.Add($"Visibility: {eventEntity.Visibility} -> {request.Visibility.Value}");
                eventEntity.Visibility = request.Visibility.Value;
            }

            if (request.RecurrenceRule != null && request.RecurrenceRule != eventEntity.RecurrenceRule)
            {
                changes.Add($"RecurrenceRule: '{eventEntity.RecurrenceRule}' -> '{request.RecurrenceRule}'");
                eventEntity.RecurrenceRule = request.RecurrenceRule;
            }

            // Validate End Date is after Start Date
            if (eventEntity.EndDate <= eventEntity.StartDate)
                throw new Exception("End Date must be after Start Date");

            eventEntity.LastUpdatedAt = DateTime.UtcNow;
            _db.Events.Update(eventEntity);
            await _db.SaveChangesAsync();

            // AC-04: Send notification to users about event changes
            if (changes.Any())
            {
                await SendEventNotificationAsync(eventEntity.Id, "Event Updated", 
                    $"Event '{eventEntity.Title}' has been updated. Changes: {string.Join("; ", changes)}");
            }

            // Log edit event - AC-06
            await LogEventActionAsync(adminId, eventEntity.Id, eventEntity.Title, "Update", string.Join("; ", changes));

            return await GetEventByIdAsync(eventEntity.Id);
        }

        public async Task DeleteEventAsync(Guid id, DeleteEventRequest request, Guid adminId)
        {
            var eventEntity = await _db.Events
                .FirstOrDefaultAsync(e => e.Id == id)
                ?? throw new Exception("Event not found");

            // AC-01: Admin must confirm deletion before removing
            if (!request.ConfirmDeletion)
                throw new Exception("Deletion must be confirmed");

            // Check if event has active bookings
            var hasActiveBookings = await _db.Bookings
                .AnyAsync(b => b.EventId == id && 
                              (b.Status == BookingStatus.Pending || b.Status == BookingStatus.Approved));

            if (hasActiveBookings)
                throw new Exception("Cannot delete event with active bookings");

            // AC-03: Send notification to users about event cancellation
            await SendEventNotificationAsync(eventEntity.Id, "Event Cancelled", 
                $"Event '{eventEntity.Title}' has been cancelled.");

            // Log deletion event - AC-05
            await LogEventActionAsync(adminId, eventEntity.Id, eventEntity.Title, "Delete", null);

            _db.Events.Remove(eventEntity);
            await _db.SaveChangesAsync();
        }

        public async Task<IReadOnlyList<EventListItem>> GetUpcomingEventsAsync()
        {
            var events = await _db.Events
                .Include(e => e.CreatedByUser)
                .Include(e => e.Bookings)
                .Where(e => e.StartDate > DateTime.UtcNow && e.Status == EventStatus.Active)
                .OrderBy(e => e.StartDate)
                .ToListAsync();

            return events.Select(e => new EventListItem
            {
                Id = e.Id,
                Title = e.Title,
                Description = e.Description,
                StartDate = e.StartDate,
                EndDate = e.EndDate,
                Location = e.Location,
                Status = e.Status.ToString(),
                Visibility = e.Visibility,
                CreatedBy = e.CreatedByUser.Fullname,
                BookingCount = e.Bookings.Count,
                IsUpcoming = true
            }).ToList();
        }

        public async Task<IReadOnlyList<EventListItem>> GetEventsByDateRangeAsync(DateTime startDate, DateTime endDate)
        {
            var events = await _db.Events
                .Include(e => e.CreatedByUser)
                .Include(e => e.Bookings)
                .Where(e => e.StartDate >= startDate && e.StartDate <= endDate)
                .OrderBy(e => e.StartDate)
                .ToListAsync();

            return events.Select(e => new EventListItem
            {
                Id = e.Id,
                Title = e.Title,
                Description = e.Description,
                StartDate = e.StartDate,
                EndDate = e.EndDate,
                Location = e.Location,
                Status = e.Status.ToString(),
                Visibility = e.Visibility,
                CreatedBy = e.CreatedByUser.Fullname,
                BookingCount = e.Bookings.Count,
                IsUpcoming = e.StartDate > DateTime.UtcNow
            }).ToList();
        }

        public async Task<int> GetEventCountAsync()
        {
            return await _db.Events.CountAsync();
        }

        public async Task<int> GetActiveEventCountAsync()
        {
            return await _db.Events
                .CountAsync(e => e.Status == EventStatus.Active);
        }

        private async Task SendEventNotificationAsync(Guid eventId, string title, string content)
        {
            // Get all active users
            var users = await _db.Users
                .Where(u => u.status == UserStatus.Active)
                .ToListAsync();

            // Create notification for each user
            var notifications = users.Select(user => new DomainLayer.Entities.Notification
            {
                Id = Guid.NewGuid(),
                Title = title,
                Content = content,
                TargetGroup = "All",
                StartDate = DateTime.UtcNow,
                EndDate = DateTime.UtcNow.AddDays(30), // Show for 30 days
                Status = NotificationStatus.Active,
                CreatedBy = Guid.Empty, // System notification
                CreatedAt = DateTime.UtcNow,
                LastUpdatedAt = DateTime.UtcNow
            }).ToList();

            _db.Notifications.AddRange(notifications);
            await _db.SaveChangesAsync();
        }

        private async Task LogEventActionAsync(Guid adminId, Guid eventId, string eventTitle, string action, string? changes)
        {
            // Get admin name
            var admin = await _db.Users
                .FirstOrDefaultAsync(u => u.Id == adminId);
            
            var adminName = admin?.Fullname ?? "Unknown Admin";

            // Log to console for now (in real app, you might want to log to database or file)
            Console.WriteLine($"[EVENT LOG] {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} - " +
                            $"Admin: {adminName} ({adminId}) - " +
                            $"Action: {action} - " +
                            $"Event: {eventTitle} ({eventId}) - " +
                            $"Changes: {changes ?? "N/A"}");
        }
    }
}
