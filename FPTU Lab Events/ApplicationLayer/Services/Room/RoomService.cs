using Application.DTOs.Room;
using DomainLayer.Entities;
using DomainLayer.Enum;
using InfrastructureLayer.Data;
using Microsoft.EntityFrameworkCore;

namespace Application.Services.Room
{
    public class RoomService : IRoomService
    {
        private readonly LabDbContext _db;

        public RoomService(LabDbContext db)
        {
            _db = db;
        }

        public async Task<IReadOnlyList<RoomListItem>> GetAllRoomsAsync(RoomFilterRequest? filter = null)
        {
            var query = _db.Rooms
                .Include(r => r.Equipments)
                .Include(r => r.Bookings.Where(b => b.Status == BookingStatus.Approved))
                .AsQueryable();

            if (filter != null)
            {
                if (!string.IsNullOrEmpty(filter.Name))
                    query = query.Where(r => r.Name.Contains(filter.Name));
                
                if (!string.IsNullOrEmpty(filter.Location))
                    query = query.Where(r => r.Location.Contains(filter.Location));
                
                if (filter.Status.HasValue)
                    query = query.Where(r => r.Status == filter.Status.Value);
                
                if (filter.MinCapacity.HasValue)
                    query = query.Where(r => r.Capacity >= filter.MinCapacity.Value);
                
                if (filter.MaxCapacity.HasValue)
                    query = query.Where(r => r.Capacity <= filter.MaxCapacity.Value);
            }

            query = query.OrderBy(r => r.Name);

            if (filter?.Page.HasValue == true && filter.PageSize.HasValue)
            {
                query = query.Skip(filter.Page.Value * filter.PageSize.Value)
                           .Take(filter.PageSize.Value);
            }

            var rooms = await query.ToListAsync();

            return rooms.Select(r => new RoomListItem
            {
                Id = r.Id,
                Name = r.Name,
                Description = r.Description,
                Location = r.Location,
                Capacity = r.Capacity,
                Status = r.Status.ToString(),
                ImageUrl = r.ImageUrl,
                EquipmentCount = r.Equipments.Count,
                ActiveBookings = r.Bookings.Count(b => b.StartTime <= DateTime.UtcNow && b.EndTime >= DateTime.UtcNow)
            }).ToList();
        }

        public async Task<RoomDetail> GetRoomByIdAsync(Guid id)
        {
            var room = await _db.Rooms
                .Include(r => r.Equipments)
                .Include(r => r.Bookings.Where(b => b.Status == BookingStatus.Approved))
                    .ThenInclude(b => b.User)
                .Include(r => r.RoomSlots)
                    .ThenInclude(rs => rs.Event)
                .FirstOrDefaultAsync(r => r.Id == id)
                ?? throw new Exception("Room not found");

            var equipments = room.Equipments.Select(e => new EquipmentInfo
            {
                Id = e.Id,
                Name = e.Name,
                Type = e.Type.ToString(),
                Status = e.Status.ToString()
            }).ToList();

            var recentBookings = room.Bookings
                .OrderByDescending(b => b.StartTime)
                .Take(10)
                .Select(b => new BookingInfo
                {
                    Id = b.Id,
                    UserName = b.User.Fullname,
                    StartTime = b.StartTime,
                    EndTime = b.EndTime,
                    Status = b.Status.ToString()
                }).ToList();

            var roomSlots = room.RoomSlots
                .OrderBy(rs => rs.Date)
                .ThenBy(rs => rs.SlotNumber)
                .Select(rs => new RoomSlotInfo
                {
                    Id = rs.Id,
                    Date = rs.Date.Date,
                    DateFormatted = rs.Date.ToString("dd/MM/yyyy"),
                    SlotNumber = rs.SlotNumber,
                    DayOfWeek = rs.DayOfWeek,
                    DayOfWeekName = GetDayOfWeekName(rs.DayOfWeek),
                    StartTime = rs.StartTime,
                    EndTime = rs.EndTime,
                    TimeRange = $"{rs.StartTime:HH:mm}-{rs.EndTime:HH:mm}",
                    EventId = rs.EventId,
                    EventTitle = rs.Event?.Title,
                    EventCode = rs.Event?.Title, // Assuming Title contains course code
                    Status = rs.Status
                }).ToList();

            return new RoomDetail
            {
                Id = room.Id,
                Name = room.Name,
                Description = room.Description,
                Location = room.Location,
                Capacity = room.Capacity,
                Status = room.Status.ToString(),
                ImageUrl = room.ImageUrl,
                EquipmentCount = room.Equipments.Count,
                ActiveBookings = room.Bookings.Count(b => b.StartTime <= DateTime.UtcNow && b.EndTime >= DateTime.UtcNow),
                CreatedAt = room.CreatedAt,
                LastUpdatedAt = room.LastUpdatedAt,
                Equipments = equipments,
                RecentBookings = recentBookings,
                RoomSlots = roomSlots
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

        public async Task<RoomDetail> CreateRoomAsync(CreateRoomRequest request)
        {
            var room = new DomainLayer.Entities.Room
            {
                Id = Guid.NewGuid(),
                Name = request.Name,
                Description = request.Description,
                Location = request.Location,
                Capacity = request.Capacity,
                ImageUrl = request.ImageUrl,
                Status = RoomStatus.Available,
                CreatedAt = DateTime.UtcNow,
                LastUpdatedAt = DateTime.UtcNow
            };

            _db.Rooms.Add(room);
            await _db.SaveChangesAsync();

            return await GetRoomByIdAsync(room.Id);
        }

        public async Task<RoomDetail> UpdateRoomAsync(Guid id, UpdateRoomRequest request)
        {
            var room = await _db.Rooms
                .FirstOrDefaultAsync(r => r.Id == id)
                ?? throw new Exception("Room not found");

            if (!string.IsNullOrWhiteSpace(request.Name))
                room.Name = request.Name;
            
            if (!string.IsNullOrWhiteSpace(request.Description))
                room.Description = request.Description;
            
            if (!string.IsNullOrWhiteSpace(request.Location))
                room.Location = request.Location;
            
            if (request.Capacity.HasValue)
                room.Capacity = request.Capacity.Value;
            
            if (request.ImageUrl != null)
                room.ImageUrl = request.ImageUrl;

            room.LastUpdatedAt = DateTime.UtcNow;
            _db.Rooms.Update(room);
            await _db.SaveChangesAsync();

            return await GetRoomByIdAsync(room.Id);
        }

        public async Task<RoomDetail> UpdateRoomStatusAsync(Guid id, UpdateRoomStatusRequest request)
        {
            var room = await _db.Rooms
                .FirstOrDefaultAsync(r => r.Id == id)
                ?? throw new Exception("Room not found");

            room.Status = request.Status;
            room.LastUpdatedAt = DateTime.UtcNow;

            _db.Rooms.Update(room);
            await _db.SaveChangesAsync();

            return await GetRoomByIdAsync(room.Id);
        }

        public async Task DeleteRoomAsync(Guid id)
        {
            var room = await _db.Rooms
                .FirstOrDefaultAsync(r => r.Id == id)
                ?? throw new Exception("Room not found");

            // Check if room has active bookings
            var hasActiveBookings = await _db.Bookings
                .AnyAsync(b => b.RoomId == id && 
                              b.Status == BookingStatus.Approved && 
                              b.StartTime <= DateTime.UtcNow && 
                              b.EndTime >= DateTime.UtcNow);

            if (hasActiveBookings)
                throw new Exception("Cannot delete room with active bookings");

            _db.Rooms.Remove(room);
            await _db.SaveChangesAsync();
        }

        public async Task<IReadOnlyList<RoomListItem>> GetAvailableRoomsAsync(DateTime startTime, DateTime endTime)
        {
            var rooms = await _db.Rooms
                .Include(r => r.Bookings)
                .Where(r => r.Status == RoomStatus.Available)
                .ToListAsync();

            var availableRooms = rooms.Where(r => !r.Bookings.Any(b => 
                b.Status == BookingStatus.Approved &&
                ((b.StartTime <= startTime && b.EndTime > startTime) ||
                 (b.StartTime < endTime && b.EndTime >= endTime) ||
                 (b.StartTime >= startTime && b.EndTime <= endTime))))
                .ToList();

            return availableRooms.Select(r => new RoomListItem
            {
                Id = r.Id,
                Name = r.Name,
                Description = r.Description,
                Location = r.Location,
                Capacity = r.Capacity,
                Status = r.Status.ToString(),
                ImageUrl = r.ImageUrl,
                EquipmentCount = r.Equipments.Count,
                ActiveBookings = 0
            }).ToList();
        }

        public async Task<bool> IsRoomAvailableAsync(Guid roomId, DateTime startTime, DateTime endTime)
        {
            var room = await _db.Rooms
                .Include(r => r.Bookings)
                .FirstOrDefaultAsync(r => r.Id == roomId);

            if (room == null || room.Status != RoomStatus.Available)
                return false;

            return !room.Bookings.Any(b => 
                b.Status == BookingStatus.Approved &&
                ((b.StartTime <= startTime && b.EndTime > startTime) ||
                 (b.StartTime < endTime && b.EndTime >= endTime) ||
                 (b.StartTime >= startTime && b.EndTime <= endTime)));
        }

        public async Task<int> GetRoomCountAsync()
        {
            return await _db.Rooms.CountAsync();
        }

        public async Task<int> GetAvailableRoomCountAsync()
        {
            return await _db.Rooms
                .CountAsync(r => r.Status == RoomStatus.Available);
        }

        // RoomSlot Management Methods
        public async Task<RoomSlotInfo> GetRoomSlotByIdAsync(Guid slotId)
        {
            var slot = await _db.RoomSlots
                .Include(rs => rs.Event)
                .Include(rs => rs.Room)
                .FirstOrDefaultAsync(rs => rs.Id == slotId)
                ?? throw new Exception("RoomSlot not found");

            return new RoomSlotInfo
            {
                Id = slot.Id,
                Date = slot.Date.Date,
                DateFormatted = slot.Date.ToString("dd/MM/yyyy"),
                SlotNumber = slot.SlotNumber,
                DayOfWeek = slot.DayOfWeek,
                DayOfWeekName = GetDayOfWeekName(slot.DayOfWeek),
                StartTime = slot.StartTime,
                EndTime = slot.EndTime,
                TimeRange = $"{slot.StartTime:HH:mm}-{slot.EndTime:HH:mm}",
                EventId = slot.EventId,
                EventTitle = slot.Event?.Title,
                EventCode = slot.Event?.Title,
                Status = slot.Status
            };
        }

        public async Task<IReadOnlyList<RoomSlotInfo>> GetRoomSlotsByRoomIdAsync(Guid roomId)
        {
            var slots = await _db.RoomSlots
                .Include(rs => rs.Event)
                .Where(rs => rs.RoomId == roomId)
                .OrderBy(rs => rs.Date)
                .ThenBy(rs => rs.SlotNumber)
                .ToListAsync();

            return slots.Select(rs => new RoomSlotInfo
            {
                Id = rs.Id,
                Date = rs.Date.Date,
                DateFormatted = rs.Date.ToString("dd/MM/yyyy"),
                SlotNumber = rs.SlotNumber,
                DayOfWeek = rs.DayOfWeek,
                DayOfWeekName = GetDayOfWeekName(rs.DayOfWeek),
                StartTime = rs.StartTime,
                EndTime = rs.EndTime,
                TimeRange = $"{rs.StartTime:HH:mm}-{rs.EndTime:HH:mm}",
                EventId = rs.EventId,
                EventTitle = rs.Event?.Title,
                EventCode = rs.Event?.Title,
                Status = rs.Status
            }).ToList();
        }

        public async Task<IReadOnlyList<RoomSlotInfo>> GetRoomSlotsByDateRangeAsync(Guid roomId, DateTime startDate, DateTime endDate)
        {
            var slots = await _db.RoomSlots
                .Include(rs => rs.Event)
                .Where(rs => rs.RoomId == roomId && 
                            rs.Date.Date >= startDate.Date && 
                            rs.Date.Date <= endDate.Date)
                .OrderBy(rs => rs.Date)
                .ThenBy(rs => rs.SlotNumber)
                .ToListAsync();

            return slots.Select(rs => new RoomSlotInfo
            {
                Id = rs.Id,
                Date = rs.Date.Date,
                DateFormatted = rs.Date.ToString("dd/MM/yyyy"),
                SlotNumber = rs.SlotNumber,
                DayOfWeek = rs.DayOfWeek,
                DayOfWeekName = GetDayOfWeekName(rs.DayOfWeek),
                StartTime = rs.StartTime,
                EndTime = rs.EndTime,
                TimeRange = $"{rs.StartTime:HH:mm}-{rs.EndTime:HH:mm}",
                EventId = rs.EventId,
                EventTitle = rs.Event?.Title,
                EventCode = rs.Event?.Title,
                Status = rs.Status
            }).ToList();
        }

        public async Task<IReadOnlyList<RoomSlotInfo>> GetAvailableRoomSlotsAsync(Guid roomId, DateTime? startDate = null, DateTime? endDate = null)
        {
            // Validate room exists
            var room = await _db.Rooms.FindAsync(roomId);
            if (room == null)
                throw new Exception($"Room not found with ID: {roomId}");

            var query = _db.RoomSlots
                .Include(rs => rs.Event)
                .Where(rs => rs.RoomId == roomId && rs.EventId == null); // Only slots without event

            // Apply date range filter if provided
            if (startDate.HasValue)
                query = query.Where(rs => rs.Date.Date >= startDate.Value.Date);

            if (endDate.HasValue)
                query = query.Where(rs => rs.Date.Date <= endDate.Value.Date);

            var slots = await query
                .OrderBy(rs => rs.Date)
                .ThenBy(rs => rs.SlotNumber)
                .ToListAsync();

            return slots.Select(rs => new RoomSlotInfo
            {
                Id = rs.Id,
                Date = rs.Date.Date,
                DateFormatted = rs.Date.ToString("dd/MM/yyyy"),
                SlotNumber = rs.SlotNumber,
                DayOfWeek = rs.DayOfWeek,
                DayOfWeekName = GetDayOfWeekName(rs.DayOfWeek),
                StartTime = rs.StartTime,
                EndTime = rs.EndTime,
                TimeRange = $"{rs.StartTime:HH:mm}-{rs.EndTime:HH:mm}",
                EventId = null,
                EventTitle = null,
                EventCode = null,
                Status = rs.Status
            }).ToList();
        }

        public async Task<RoomSlotInfo> CreateRoomSlotAsync(CreateRoomSlotRequest request)
        {
            // Validate room exists
            var room = await _db.Rooms.FirstOrDefaultAsync(r => r.Id == request.RoomId);
            if (room == null)
                throw new Exception($"Room not found with ID: {request.RoomId}");

            // Validate EventId if provided
            if (request.EventId.HasValue)
            {
                var eventExists = await _db.Events.AnyAsync(e => e.Id == request.EventId.Value);
                if (!eventExists)
                    throw new Exception($"Event not found with ID: {request.EventId.Value}");
            }

            // Validate Date
            if (request.Date == default)
                throw new Exception("Date is required");

            // Check if slot already exists for this date
            var existingSlot = await _db.RoomSlots
                .AnyAsync(rs => rs.RoomId == request.RoomId && 
                               rs.Date.Date == request.Date.Date && 
                               rs.SlotNumber == request.SlotNumber);

            if (existingSlot)
                throw new Exception($"Room slot already exists for Room {room.Name}, Date {request.Date:dd/MM/yyyy}, Slot {request.SlotNumber}");

            // Validate slot number (1-8)
            if (request.SlotNumber < 1 || request.SlotNumber > 8)
                throw new Exception("Slot number must be between 1 and 8");

            // Calculate DayOfWeek from Date
            var dayOfWeek = (int)request.Date.DayOfWeek;

            var roomSlot = new DomainLayer.Entities.RoomSlot
            {
                Id = Guid.NewGuid(),
                RoomId = request.RoomId,
                Date = request.Date.Date,
                SlotNumber = request.SlotNumber,
                DayOfWeek = dayOfWeek, // Auto-calculated from Date
                StartTime = request.StartTime,
                EndTime = request.EndTime,
                EventId = request.EventId,
                Status = request.Status,
                CreatedAt = DateTime.UtcNow,
                LastUpdatedAt = DateTime.UtcNow
            };

            _db.RoomSlots.Add(roomSlot);
            await _db.SaveChangesAsync();

            return await GetRoomSlotByIdAsync(roomSlot.Id);
        }

        public async Task<RoomSlotInfo> UpdateRoomSlotAsync(Guid slotId, UpdateRoomSlotRequest request)
        {
            var roomSlot = await _db.RoomSlots.FirstOrDefaultAsync(rs => rs.Id == slotId)
                ?? throw new Exception($"RoomSlot not found with ID: {slotId}");

            if (request.EventId.HasValue)
            {
                // Validate event exists if setting to a value (not null)
                if (request.EventId.Value != Guid.Empty)
                {
                    var eventExists = await _db.Events.AnyAsync(e => e.Id == request.EventId.Value);
                    if (!eventExists)
                        throw new Exception($"Event not found with ID: {request.EventId.Value}");
                }
                roomSlot.EventId = request.EventId.Value == Guid.Empty ? null : request.EventId.Value;
            }

            if (request.Status != null)
                roomSlot.Status = request.Status;

            roomSlot.LastUpdatedAt = DateTime.UtcNow;

            _db.RoomSlots.Update(roomSlot);
            await _db.SaveChangesAsync();

            return await GetRoomSlotByIdAsync(slotId);
        }

        public async Task DeleteRoomSlotAsync(Guid slotId)
        {
            var roomSlot = await _db.RoomSlots.FirstOrDefaultAsync(rs => rs.Id == slotId)
                ?? throw new Exception($"RoomSlot not found with ID: {slotId}");

            _db.RoomSlots.Remove(roomSlot);
            await _db.SaveChangesAsync();
        }

        public async Task<IReadOnlyList<RoomSlotInfo>> GenerateWeeklyRoomSlotsAsync(Guid roomId, DateTime weekStartDate)
        {
            // Validate room exists
            var room = await _db.Rooms.FirstOrDefaultAsync(r => r.Id == roomId);
            if (room == null)
                throw new Exception($"Room not found with ID: {roomId}");

            // Define default time slots (8 slots per day)
            var timeSlots = new List<(TimeOnly Start, TimeOnly End)>
            {
                (new TimeOnly(7, 0), new TimeOnly(9, 0)),     // Slot 1: 07:00-09:00
                (new TimeOnly(9, 15), new TimeOnly(11, 15)),  // Slot 2: 09:15-11:15
                (new TimeOnly(12, 30), new TimeOnly(14, 30)), // Slot 3: 12:30-14:30
                (new TimeOnly(14, 45), new TimeOnly(16, 45)), // Slot 4: 14:45-16:45
                (new TimeOnly(15, 0), new TimeOnly(17, 0)),   // Slot 5: 15:00-17:00
                (new TimeOnly(17, 15), new TimeOnly(19, 15)), // Slot 6: 17:15-19:15
                (new TimeOnly(18, 0), new TimeOnly(20, 0)),   // Slot 7: 18:00-20:00
                (new TimeOnly(20, 15), new TimeOnly(22, 15))  // Slot 8: 20:15-22:15
            };

            var createdSlots = new List<DomainLayer.Entities.RoomSlot>();

            // Generate slots for 7 days starting from weekStartDate
            var currentDate = weekStartDate.Date;
            for (int day = 0; day < 7; day++)
            {
                var date = currentDate.AddDays(day);
                var dayOfWeek = (int)date.DayOfWeek;

                // Only generate for weekdays (Monday-Friday)
                if (dayOfWeek >= 1 && dayOfWeek <= 5)
                {
                    for (int slotNumber = 1; slotNumber <= 8; slotNumber++)
                    {
                        // Check if slot already exists for this date
                        var exists = await _db.RoomSlots
                            .AnyAsync(rs => rs.RoomId == roomId && 
                                           rs.Date.Date == date && 
                                           rs.SlotNumber == slotNumber);

                        if (!exists)
                        {
                            var timeSlot = timeSlots[slotNumber - 1];
                            var roomSlot = new DomainLayer.Entities.RoomSlot
                            {
                                Id = Guid.NewGuid(),
                                RoomId = roomId,
                                Date = date,
                                SlotNumber = slotNumber,
                                DayOfWeek = dayOfWeek,
                                StartTime = timeSlot.Start,
                                EndTime = timeSlot.End,
                                EventId = null,
                                Status = null,
                                CreatedAt = DateTime.UtcNow,
                                LastUpdatedAt = DateTime.UtcNow
                            };

                            createdSlots.Add(roomSlot);
                        }
                    }
                }
            }

            if (createdSlots.Any())
            {
                _db.RoomSlots.AddRange(createdSlots);
                await _db.SaveChangesAsync();
            }

            // Return all slots for this room
            return await GetRoomSlotsByRoomIdAsync(roomId);
        }
    }
}
