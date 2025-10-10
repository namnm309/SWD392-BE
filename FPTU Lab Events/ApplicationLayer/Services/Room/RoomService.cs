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
                RecentBookings = recentBookings
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
    }
}
