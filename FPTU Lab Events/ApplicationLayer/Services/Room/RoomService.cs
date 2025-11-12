using Application.DTOs.Room;
using DomainLayer.Entities;
using DomainLayer.Enum;
using InfrastructureLayer.Core.Redis;
using InfrastructureLayer.Data;
using Microsoft.EntityFrameworkCore;

namespace Application.Services.Room
{
    public class RoomService : IRoomService
    {
        private readonly LabDbContext _db;
        private readonly IRedisService _redis;

        public RoomService(LabDbContext db, IRedisService redis)
        {
            _db = db;
            _redis = redis;
        }

        public async Task<IReadOnlyList<RoomListItem>> GetAllRoomsAsync(RoomFilterRequest? filter = null)
        {
            var cacheKey = BuildRoomListCacheKey(filter);
            var cached = await _redis.GetAsync<IReadOnlyList<RoomListItem>>(cacheKey);
            if (cached != null) return cached;

            var query = _db.Rooms
                .Include(r => r.Equipments)
                .Include(r => r.Bookings.Where(b => b.Status == BookingStatus.Approved))
                .AsQueryable();

            if (filter != null)
            {
                if (!string.IsNullOrEmpty(filter.Name))
                    query = query.Where(r => r.Name.Contains(filter.Name));
                
                if (filter.Status.HasValue)
                    query = query.Where(r => r.Status == filter.Status.Value);
                
                if (filter.MinCapacity.HasValue)
                    query = query.Where(r => r.Capacity >= filter.MinCapacity.Value);
                
                if (filter.MaxCapacity.HasValue)
                    query = query.Where(r => r.Capacity <= filter.MaxCapacity.Value);

                if (filter.LabId.HasValue)
                    query = query.Where(r => r.LabId == filter.LabId.Value);
            }

            query = query.OrderBy(r => r.Name);

            if (filter?.Page.HasValue == true && filter.PageSize.HasValue)
            {
                query = query.Skip(filter.Page.Value * filter.PageSize.Value)
                           .Take(filter.PageSize.Value);
            }

            var rooms = await query
                .Include(r => r.Lab)
                .ToListAsync();

            var result = rooms.Select(r => new RoomListItem
            {
                Id = r.Id,
                Name = r.Name,
                Capacity = r.Capacity,
                Status = r.Status.ToString(),
                LabId = r.LabId,
                LabName = r.Lab?.Name,
                EquipmentCount = r.Equipments.Count,
                ActiveBookings = r.Bookings.Count(b => b.StartTime <= DateTime.UtcNow && b.EndTime >= DateTime.UtcNow)
            }).ToList();

            await _redis.SetAsync(cacheKey, result, RedisCacheDefaults.DefaultTtl);
            return result;
        }

        public async Task<RoomDetail> GetRoomByIdAsync(Guid id)
        {
            var cacheKey = RedisCacheKeyBuilder.Build("rooms:detail:v1", ("id", id));
            var cached = await _redis.GetAsync<RoomDetail>(cacheKey);
            if (cached != null) return cached;

            var room = await _db.Rooms
                .Include(r => r.Lab)
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

            var detail = new RoomDetail
            {
                Id = room.Id,
                Name = room.Name,
                Capacity = room.Capacity,
                Status = room.Status.ToString(),
                LabId = room.LabId,
                LabName = room.Lab?.Name,
                EquipmentCount = room.Equipments.Count,
                ActiveBookings = room.Bookings.Count(b => b.StartTime <= DateTime.UtcNow && b.EndTime >= DateTime.UtcNow),
                CreatedAt = room.CreatedAt,
                LastUpdatedAt = room.LastUpdatedAt,
                Equipments = equipments,
                RecentBookings = recentBookings,
                RoomSlots = roomSlots
            };

            await _redis.SetAsync(cacheKey, detail, RedisCacheDefaults.DefaultTtl);
            return detail;
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
            // Validate LabId if provided
            if (request.LabId.HasValue)
            {
                var labExists = await _db.Labs.AnyAsync(l => l.Id == request.LabId.Value);
                if (!labExists)
                    throw new Exception("Lab not found");
            }

            var room = new DomainLayer.Entities.Room
            {
                Id = Guid.NewGuid(),
                Name = request.Name,
                Capacity = request.Capacity,
                Status = RoomStatus.Available,
                LabId = request.LabId,
                CreatedAt = DateTime.UtcNow,
                LastUpdatedAt = DateTime.UtcNow
            };

            _db.Rooms.Add(room);
            await _db.SaveChangesAsync();

            await InvalidateRoomCaches(room.Id);
            return await GetRoomByIdAsync(room.Id);
        }

        public async Task<RoomDetail> UpdateRoomAsync(Guid id, UpdateRoomRequest request)
        {
            var room = await _db.Rooms
                .FirstOrDefaultAsync(r => r.Id == id)
                ?? throw new Exception("Room not found");

            if (!string.IsNullOrWhiteSpace(request.Name))
                room.Name = request.Name;
            
            if (request.Capacity.HasValue)
                room.Capacity = request.Capacity.Value;

            if (request.LabId != room.LabId)
            {
                if (request.LabId.HasValue)
                {
                    var labExists = await _db.Labs.AnyAsync(l => l.Id == request.LabId.Value);
                    if (!labExists)
                        throw new Exception("Lab not found");
                }
                room.LabId = request.LabId;
            }

            room.LastUpdatedAt = DateTime.UtcNow;
            _db.Rooms.Update(room);
            await _db.SaveChangesAsync();

            await InvalidateRoomCaches(room.Id);
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

            await InvalidateRoomCaches(room.Id);
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

            await InvalidateRoomCaches(room.Id);
        }

        public async Task<IReadOnlyList<RoomListItem>> GetAvailableRoomsAsync(DateTime startTime, DateTime endTime)
        {
            var cacheKey = RedisCacheKeyBuilder.Build(
                "rooms:available:v1",
                ("start", startTime),
                ("end", endTime));
            var cached = await _redis.GetAsync<IReadOnlyList<RoomListItem>>(cacheKey);
            if (cached != null) return cached;

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

            var result = availableRooms.Select(r => new RoomListItem
            {
                Id = r.Id,
                Name = r.Name,
                Capacity = r.Capacity,
                Status = r.Status.ToString(),
                EquipmentCount = r.Equipments.Count,
                ActiveBookings = 0
            }).ToList();

            await _redis.SetAsync(cacheKey, result, RedisCacheDefaults.DefaultTtl);
            return result;
        }

        public async Task<bool> IsRoomAvailableAsync(Guid roomId, DateTime startTime, DateTime endTime)
        {
            var cacheKey = RedisCacheKeyBuilder.Build(
                "rooms:availability:v1",
                ("roomId", roomId),
                ("start", startTime),
                ("end", endTime));
            var cached = await _redis.GetAsync<bool?>(cacheKey);
            if (cached.HasValue) return cached.Value;

            var room = await _db.Rooms
                .Include(r => r.Bookings)
                .FirstOrDefaultAsync(r => r.Id == roomId);

            if (room == null || room.Status != RoomStatus.Available)
            {
                await _redis.SetAsync(cacheKey, false, RedisCacheDefaults.DefaultTtl);
                return false;
            }

            var isAvailable = !room.Bookings.Any(b => 
                b.Status == BookingStatus.Approved &&
                ((b.StartTime <= startTime && b.EndTime > startTime) ||
                 (b.StartTime < endTime && b.EndTime >= endTime) ||
                 (b.StartTime >= startTime && b.EndTime <= endTime)));

            await _redis.SetAsync(cacheKey, isAvailable, RedisCacheDefaults.DefaultTtl);
            return isAvailable;
        }

        public async Task<int> GetRoomCountAsync()
        {
            const string cacheKey = "rooms:count:v1";
            var cached = await _redis.GetAsync<int?>(cacheKey);
            if (cached.HasValue) return cached.Value;

            var count = await _db.Rooms.CountAsync();
            await _redis.SetAsync(cacheKey, count, RedisCacheDefaults.DefaultTtl);
            return count;
        }

        public async Task<int> GetAvailableRoomCountAsync()
        {
            const string cacheKey = "rooms:available-count:v1";
            var cached = await _redis.GetAsync<int?>(cacheKey);
            if (cached.HasValue) return cached.Value;

            var count = await _db.Rooms
                .CountAsync(r => r.Status == RoomStatus.Available);
            await _redis.SetAsync(cacheKey, count, RedisCacheDefaults.DefaultTtl);
            return count;
        }

        // RoomSlot Management Methods
        public async Task<RoomSlotInfo> GetRoomSlotByIdAsync(Guid slotId)
        {
            var cacheKey = RedisCacheKeyBuilder.Build("rooms:slots:detail:v1", ("slotId", slotId));
            var cached = await _redis.GetAsync<RoomSlotInfo>(cacheKey);
            if (cached != null) return cached;

            var slot = await _db.RoomSlots
                .Include(rs => rs.Event)
                .Include(rs => rs.Room)
                .FirstOrDefaultAsync(rs => rs.Id == slotId)
                ?? throw new Exception("RoomSlot not found");

            var detail = new RoomSlotInfo
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

            await _redis.SetAsync(cacheKey, detail, RedisCacheDefaults.DefaultTtl);
            return detail;
        }

        public async Task<IReadOnlyList<RoomSlotInfo>> GetRoomSlotsByRoomIdAsync(Guid roomId)
        {
            var cacheKey = RedisCacheKeyBuilder.Build("rooms:slots:list:v1", ("roomId", roomId));
            var cached = await _redis.GetAsync<IReadOnlyList<RoomSlotInfo>>(cacheKey);
            if (cached != null) return cached;

            var slots = await _db.RoomSlots
                .Include(rs => rs.Event)
                .Where(rs => rs.RoomId == roomId)
                .OrderBy(rs => rs.Date)
                .ThenBy(rs => rs.SlotNumber)
                .ToListAsync();

            var result = slots.Select(rs => new RoomSlotInfo
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

            await _redis.SetAsync(cacheKey, result, RedisCacheDefaults.DefaultTtl);
            return result;
        }

        public async Task<IReadOnlyList<RoomSlotInfo>> GetRoomSlotsByDateRangeAsync(Guid roomId, DateTime startDate, DateTime endDate)
        {
            var cacheKey = RedisCacheKeyBuilder.Build(
                "rooms:slots:range:v1",
                ("roomId", roomId),
                ("start", startDate),
                ("end", endDate));
            var cached = await _redis.GetAsync<IReadOnlyList<RoomSlotInfo>>(cacheKey);
            if (cached != null) return cached;

            var slots = await _db.RoomSlots
                .Include(rs => rs.Event)
                .Where(rs => rs.RoomId == roomId && 
                            rs.Date.Date >= startDate.Date && 
                            rs.Date.Date <= endDate.Date)
                .OrderBy(rs => rs.Date)
                .ThenBy(rs => rs.SlotNumber)
                .ToListAsync();

            var result = slots.Select(rs => new RoomSlotInfo
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

            await _redis.SetAsync(cacheKey, result, RedisCacheDefaults.DefaultTtl);
            return result;
        }

        public async Task<IReadOnlyList<RoomSlotInfo>> GetAvailableRoomSlotsAsync(Guid roomId, DateTime? startDate = null, DateTime? endDate = null)
        {
            var cacheKey = RedisCacheKeyBuilder.Build(
                "rooms:slots:available:v1",
                ("roomId", roomId),
                ("start", startDate),
                ("end", endDate));
            var cached = await _redis.GetAsync<IReadOnlyList<RoomSlotInfo>>(cacheKey);
            if (cached != null) return cached;

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

            var result = slots.Select(rs => new RoomSlotInfo
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

            await _redis.SetAsync(cacheKey, result, RedisCacheDefaults.DefaultTtl);
            return result;
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

            await InvalidateRoomSlotCaches(roomSlot.RoomId, roomSlot.Id);
            return await GetRoomSlotByIdAsync(roomSlot.Id);
        }

        public async Task<RoomSlotInfo> UpdateRoomSlotAsync(Guid slotId, UpdateRoomSlotRequest request)
        {
            var roomSlot = await _db.RoomSlots
                .Include(rs => rs.Room)
                .FirstOrDefaultAsync(rs => rs.Id == slotId)
                ?? throw new Exception($"RoomSlot not found with ID: {slotId}");

            // Update RoomId if provided
            if (request.RoomId.HasValue)
            {
                var roomExists = await _db.Rooms.AnyAsync(r => r.Id == request.RoomId.Value);
                if (!roomExists)
                    throw new Exception($"Room not found with ID: {request.RoomId.Value}");
                
                roomSlot.RoomId = request.RoomId.Value;
            }

            // Update Date if provided
            if (request.Date.HasValue)
            {
                roomSlot.Date = request.Date.Value.Date;
                // Recalculate DayOfWeek when Date changes
                roomSlot.DayOfWeek = (int)request.Date.Value.DayOfWeek;
            }

            // Update SlotNumber if provided
            if (request.SlotNumber.HasValue)
            {
                if (request.SlotNumber.Value < 1 || request.SlotNumber.Value > 8)
                    throw new Exception("Slot number must be between 1 and 8");
                roomSlot.SlotNumber = request.SlotNumber.Value;
            }

            // Update StartTime if provided
            if (request.StartTime.HasValue)
            {
                roomSlot.StartTime = request.StartTime.Value;
            }

            // Update EndTime if provided
            if (request.EndTime.HasValue)
            {
                roomSlot.EndTime = request.EndTime.Value;
            }

            // Update EventId if provided
            if (request.EventId.HasValue)
            {
                // If EventId is Guid.Empty, set to null (remove event assignment)
                if (request.EventId.Value == Guid.Empty)
                {
                    roomSlot.EventId = null;
                }
                else
                {
                    // Validate event exists
                    var eventExists = await _db.Events.AnyAsync(e => e.Id == request.EventId.Value);
                    if (!eventExists)
                        throw new Exception($"Event not found with ID: {request.EventId.Value}");
                    
                    roomSlot.EventId = request.EventId.Value;
                }
            }

            // Update Status if provided
            if (request.Status != null)
            {
                roomSlot.Status = request.Status;
            }

            // Check if updated slot conflicts with existing slot
            var finalRoomId = request.RoomId ?? roomSlot.RoomId;
            var finalDate = request.Date ?? roomSlot.Date;
            var finalSlotNumber = request.SlotNumber ?? roomSlot.SlotNumber;

            var conflictingSlot = await _db.RoomSlots
                .AnyAsync(rs => rs.Id != slotId &&
                               rs.RoomId == finalRoomId &&
                               rs.Date.Date == finalDate.Date &&
                               rs.SlotNumber == finalSlotNumber);

            if (conflictingSlot)
            {
                var room = await _db.Rooms.FirstOrDefaultAsync(r => r.Id == finalRoomId);
                throw new Exception($"Room slot already exists for Room {room?.Name ?? finalRoomId.ToString()}, Date {finalDate:dd/MM/yyyy}, Slot {finalSlotNumber}");
            }

            roomSlot.LastUpdatedAt = DateTime.UtcNow;

            _db.RoomSlots.Update(roomSlot);
            await _db.SaveChangesAsync();

            await InvalidateRoomSlotCaches(roomSlot.RoomId, roomSlot.Id);
            return await GetRoomSlotByIdAsync(slotId);
        }

        public async Task DeleteRoomSlotAsync(Guid slotId)
        {
            var roomSlot = await _db.RoomSlots.FirstOrDefaultAsync(rs => rs.Id == slotId)
                ?? throw new Exception($"RoomSlot not found with ID: {slotId}");

            _db.RoomSlots.Remove(roomSlot);
            await _db.SaveChangesAsync();

            await InvalidateRoomSlotCaches(roomSlot.RoomId, roomSlot.Id);
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
            await InvalidateRoomSlotCaches(roomId);
            return await GetRoomSlotsByRoomIdAsync(roomId);
        }

        private static string BuildRoomListCacheKey(RoomFilterRequest? filter)
        {
            if (filter == null)
            {
                return "rooms:list:v1";
            }

            return RedisCacheKeyBuilder.Build(
                "rooms:list:v1",
                ("name", filter.Name),
                ("status", filter.Status),
                ("minCapacity", filter.MinCapacity),
                ("maxCapacity", filter.MaxCapacity),
                ("labId", filter.LabId),
                ("page", filter.Page),
                ("pageSize", filter.PageSize));
        }

        private async Task InvalidateRoomCaches(Guid roomId)
        {
            await _redis.RemoveAsync("rooms:list:v1");
            await _redis.RemoveAsync(RedisCacheKeyBuilder.Build("rooms:detail:v1", ("id", roomId)));
            await _redis.RemoveAsync("rooms:count:v1");
            await _redis.RemoveAsync("rooms:available-count:v1");
            await _redis.RemoveAsync("rooms:available:v1");
            await _redis.RemoveAsync(RedisCacheKeyBuilder.Build("rooms:availability:v1", ("roomId", roomId)));
        }

        private async Task InvalidateRoomSlotCaches(Guid roomId, Guid? slotId = null)
        {
            await InvalidateRoomCaches(roomId);
            await _redis.RemoveAsync(RedisCacheKeyBuilder.Build("rooms:slots:list:v1", ("roomId", roomId)));
            await _redis.RemoveAsync(RedisCacheKeyBuilder.Build("rooms:slots:available:v1", ("roomId", roomId)));
            await _redis.RemoveAsync(RedisCacheKeyBuilder.Build("rooms:slots:range:v1", ("roomId", roomId)));
            if (slotId.HasValue)
            {
                await _redis.RemoveAsync(RedisCacheKeyBuilder.Build("rooms:slots:detail:v1", ("slotId", slotId.Value)));
            }
        }
    }
}
