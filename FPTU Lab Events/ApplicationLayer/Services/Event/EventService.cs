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
                Status = e.Status.ToString(),
                Visibility = e.Visibility,
                CreatedBy = e.CreatedByUser.Fullname,
                BookingCount = e.Bookings.Count,
                IsUpcoming = e.StartDate > DateTime.UtcNow,
                Capacity = e.Capacity,
                ImageUrl = e.ImageUrl
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
                .Include(e => e.RoomSlots)
                    .ThenInclude(rs => rs.Room)
                        .ThenInclude(r => r.Lab)
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

            var roomSlots = eventEntity.RoomSlots.Select(rs => new EventRoomSlotInfo
            {
                Id = rs.Id,
                RoomId = rs.RoomId,
                RoomName = rs.Room.Name,
                Date = rs.Date,
                DateFormatted = rs.Date.ToString("dd/MM/yyyy"),
                SlotNumber = rs.SlotNumber,
                DayOfWeekName = GetDayOfWeekName(rs.DayOfWeek),
                TimeRange = $"{rs.StartTime:HH:mm}-{rs.EndTime:HH:mm}",
                Status = rs.Status
            }).ToList();

            // Determine RoomId and LabId from first RoomSlot
            var firstSlot = eventEntity.RoomSlots.FirstOrDefault();
            var labId = firstSlot?.Room?.LabId;
            var labName = firstSlot?.Room?.Lab?.Name;

            return new EventDetail
            {
                Id = eventEntity.Id,
                Title = eventEntity.Title,
                Description = eventEntity.Description,
                StartDate = eventEntity.StartDate,
                EndDate = eventEntity.EndDate,
                Status = eventEntity.Status.ToString(),
                Visibility = eventEntity.Visibility,
                CreatedBy = eventEntity.CreatedByUser.Fullname,
                BookingCount = eventEntity.Bookings.Count,
                IsUpcoming = eventEntity.StartDate > DateTime.UtcNow,
                CreatedAt = eventEntity.CreatedAt,
                LastUpdatedAt = eventEntity.LastUpdatedAt,
                Bookings = bookings,
                LabId = labId,
                LabName = labName,
                RoomId = firstSlot?.RoomId,
                RoomName = firstSlot?.Room.Name,
                RoomSlots = roomSlots,
                Capacity = eventEntity.Capacity,
                ImageUrl = eventEntity.ImageUrl
            };
        }

        private string GetDayOfWeekName(int dayOfWeek)
        {
            return dayOfWeek switch
            {
                0 => "Sunday",
                1 => "Monday",
                2 => "Tuesday",
                3 => "Wednesday",
                4 => "Thursday",
                5 => "Friday",
                6 => "Saturday",
                _ => "Unknown"
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

            // Validate Lab if provided
            DomainLayer.Entities.Lab? lab = null;
            if (request.LabId.HasValue)
            {
                lab = await _db.Labs
                    .FirstOrDefaultAsync(l => l.Id == request.LabId.Value);
                if (lab == null)
                    throw new Exception($"Lab not found with ID: {request.LabId.Value}");
            }

            // Validate Room if provided
            DomainLayer.Entities.Room? room = null;
            if (request.RoomId.HasValue)
            {
                room = await _db.Rooms
                    .Include(r => r.Lab)
                    .FirstOrDefaultAsync(r => r.Id == request.RoomId.Value);
                if (room == null)
                    throw new Exception($"Room not found with ID: {request.RoomId.Value}");

                // If LabId is provided, validate that Room belongs to that Lab
                if (request.LabId.HasValue && room.LabId != request.LabId.Value)
                    throw new Exception($"Room '{room.Name}' does not belong to the specified Lab");
            }

            // Validate RoomSlots if provided
            if (request.RoomSlotIds != null && request.RoomSlotIds.Any())
            {
                // Get all requested room slots with Room and Lab info
                var roomSlots = await _db.RoomSlots
                    .Include(rs => rs.Room)
                        .ThenInclude(r => r.Lab)
                    .Where(rs => request.RoomSlotIds.Contains(rs.Id))
                    .ToListAsync();

                if (roomSlots.Count != request.RoomSlotIds.Count)
                    throw new Exception("Some RoomSlots not found");

                // Check if all slots belong to the same room
                var distinctRoomIds = roomSlots.Select(rs => rs.RoomId).Distinct().ToList();
                if (distinctRoomIds.Count > 1)
                    throw new Exception("All RoomSlots must belong to the same Room");

                // Get the room from the first slot
                var slotRoom = roomSlots.First().Room;

                // If RoomId is provided, validate it matches the slots' room
                if (request.RoomId.HasValue && !distinctRoomIds.Contains(request.RoomId.Value))
                    throw new Exception("RoomSlots do not belong to the specified Room");

                // If LabId is provided, validate that the slots' room belongs to that Lab
                if (request.LabId.HasValue && slotRoom.LabId != request.LabId.Value)
                    throw new Exception($"RoomSlots belong to a Room that does not belong to the specified Lab");

                // Check if any slot already has an event
                var slotsWithEvents = roomSlots.Where(rs => rs.EventId.HasValue).ToList();
                if (slotsWithEvents.Any())
                {
                    var slotInfo = slotsWithEvents.First();
                    throw new Exception($"RoomSlot (Date: {slotInfo.Date:dd/MM/yyyy}, Slot: {slotInfo.SlotNumber}) is already assigned to another event");
                }
            }

            var eventEntity = new DomainLayer.Entities.Event
            {
                Id = Guid.NewGuid(),
                Title = request.Title,
                Description = request.Description,
                StartDate = request.StartDate,
                EndDate = request.EndDate,
                Status = request.Status,
                Visibility = request.Visibility,
                Capacity = request.Capacity,
                ImageUrl = request.ImageUrl,
                CreatedBy = adminId,
                CreatedAt = DateTime.UtcNow,
                LastUpdatedAt = DateTime.UtcNow
            };

            try
            {
                _db.Events.Add(eventEntity);
                await _db.SaveChangesAsync();
                Console.WriteLine($"Event created successfully with ID: {eventEntity.Id}");

                // Assign Event to RoomSlots if provided
                if (request.RoomSlotIds != null && request.RoomSlotIds.Any())
                {
                    var roomSlotsToUpdate = await _db.RoomSlots
                        .Where(rs => request.RoomSlotIds.Contains(rs.Id))
                        .ToListAsync();

                    foreach (var slot in roomSlotsToUpdate)
                    {
                        slot.EventId = eventEntity.Id;
                        slot.LastUpdatedAt = DateTime.UtcNow;
                    }

                    await _db.SaveChangesAsync();
                    Console.WriteLine($"Assigned event to {roomSlotsToUpdate.Count} room slots");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Database error: {ex.Message}");
                Console.WriteLine($"Inner exception: {ex.InnerException?.Message}");
                throw new Exception($"Failed to save event: {ex.InnerException?.Message ?? ex.Message}");
            }

            // AC-04: Send notification to all users (temporarily disabled for debugging)
            // await SendEventNotificationAsync(eventEntity.Id, "New Event Created", 
            //     $"A new event '{eventEntity.Title}' has been created.");

            // Log creation event - AC-06 (temporarily disabled for debugging)
            // await LogEventActionAsync(adminId, eventEntity.Id, eventEntity.Title, "Create", null);

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

            if (request.Capacity.HasValue && request.Capacity.Value != eventEntity.Capacity)
            {
                changes.Add($"Capacity: {eventEntity.Capacity} -> {request.Capacity.Value}");
                eventEntity.Capacity = request.Capacity.Value;
            }

            if (request.ImageUrl != null && request.ImageUrl != eventEntity.ImageUrl)
            {
                changes.Add($"ImageUrl: '{eventEntity.ImageUrl}' -> '{request.ImageUrl}'");
                eventEntity.ImageUrl = request.ImageUrl;
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

            // Check if event has any bookings (for information purposes)
            var relatedBookings = await _db.Bookings
                .Where(b => b.EventId == id)
                .ToListAsync();

            // Note: We can still delete the event even with bookings because
            // the database is configured with SetNull behavior for EventId in Bookings
            // The bookings will remain but their EventId will be set to null

            // AC-03: Send notification to users about event cancellation
            await SendEventNotificationAsync(eventEntity.Id, "Event Cancelled", 
                $"Event '{eventEntity.Title}' has been cancelled.");

            // Log deletion event - AC-05
            await LogEventActionAsync(adminId, eventEntity.Id, eventEntity.Title, "Delete", 
                relatedBookings.Any() ? $"Event had {relatedBookings.Count} related bookings" : null);

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
                Status = e.Status.ToString(),
                Visibility = e.Visibility,
                CreatedBy = e.CreatedByUser.Fullname,
                BookingCount = e.Bookings.Count,
                IsUpcoming = true,
                Capacity = e.Capacity,
                ImageUrl = e.ImageUrl
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
                Status = e.Status.ToString(),
                Visibility = e.Visibility,
                CreatedBy = e.CreatedByUser.Fullname,
                BookingCount = e.Bookings.Count,
                IsUpcoming = e.StartDate > DateTime.UtcNow,
                Capacity = e.Capacity,
                ImageUrl = e.ImageUrl
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
