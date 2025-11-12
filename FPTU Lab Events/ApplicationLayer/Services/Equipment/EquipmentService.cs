using Application.DTOs.Equipment;
using DomainLayer.Entities;
using DomainLayer.Enum;
using InfrastructureLayer.Core.Redis;
using InfrastructureLayer.Data;
using Microsoft.EntityFrameworkCore;

namespace Application.Services.Equipment
{
    public class EquipmentService : IEquipmentService
    {
        private readonly LabDbContext _db;
        private readonly IRedisService _redis;

        public EquipmentService(LabDbContext db, IRedisService redis)
        {
            _db = db;
            _redis = redis;
        }

        public async Task<IReadOnlyList<EquipmentListItem>> GetAllEquipmentsAsync(EquipmentFilterRequest? filter = null)
        {
            var cacheKey = BuildEquipmentListCacheKey(filter);
            var cached = await _redis.GetAsync<IReadOnlyList<EquipmentListItem>>(cacheKey);
            if (cached != null) return cached;

            var query = _db.Equipments
                .Include(e => e.Room)
                .AsQueryable();

            if (filter != null)
            {
                if (!string.IsNullOrEmpty(filter.Name))
                    query = query.Where(e => e.Name.Contains(filter.Name));
                
                if (!string.IsNullOrEmpty(filter.SerialNumber))
                    query = query.Where(e => e.SerialNumber.Contains(filter.SerialNumber));
                
                if (filter.Type.HasValue)
                    query = query.Where(e => e.Type == filter.Type.Value);
                
                if (filter.Status.HasValue)
                    query = query.Where(e => e.Status == filter.Status.Value);
                
                if (filter.RoomId.HasValue)
                    query = query.Where(e => e.RoomId == filter.RoomId.Value);
            }

            query = query.OrderBy(e => e.Name);

            if (filter?.Page.HasValue == true && filter.PageSize.HasValue)
            {
                query = query.Skip(filter.Page.Value * filter.PageSize.Value)
                           .Take(filter.PageSize.Value);
            }

            var equipments = await query.ToListAsync();

            var result = equipments.Select(e => new EquipmentListItem
            {
                Id = e.Id,
                Name = e.Name,
                Description = e.Description,
                SerialNumber = e.SerialNumber,
                Type = e.Type.ToString(),
                Status = e.Status.ToString(),
                ImageUrl = e.ImageUrl,
                RoomName = e.Room?.Name,
                LastMaintenanceDate = e.LastMaintenanceDate,
                NextMaintenanceDate = e.NextMaintenanceDate
            }).ToList();

            await _redis.SetAsync(cacheKey, result, RedisCacheDefaults.DefaultTtl);
            return result;
        }

        public async Task<EquipmentDetail> GetEquipmentByIdAsync(Guid id)
        {
            var cacheKey = RedisCacheKeyBuilder.Build("equipments:detail:v1", ("id", id));
            var cached = await _redis.GetAsync<EquipmentDetail>(cacheKey);
            if (cached != null) return cached;

            var equipment = await _db.Equipments
                .Include(e => e.Room)
                .FirstOrDefaultAsync(e => e.Id == id)
                ?? throw new Exception("Equipment not found");

            var detail = new EquipmentDetail
            {
                Id = equipment.Id,
                Name = equipment.Name,
                Description = equipment.Description,
                SerialNumber = equipment.SerialNumber,
                Type = equipment.Type.ToString(),
                Status = equipment.Status.ToString(),
                ImageUrl = equipment.ImageUrl,
                RoomId = equipment.RoomId,
                RoomName = equipment.Room?.Name,
                LastMaintenanceDate = equipment.LastMaintenanceDate,
                NextMaintenanceDate = equipment.NextMaintenanceDate,
                CreatedAt = equipment.CreatedAt,
                LastUpdatedAt = equipment.LastUpdatedAt
            };

            await _redis.SetAsync(cacheKey, detail, RedisCacheDefaults.DefaultTtl);
            return detail;
        }

        public async Task<EquipmentDetail> CreateEquipmentAsync(CreateEquipmentRequest request)
        {
            // Check if serial number already exists
            var existingEquipment = await _db.Equipments
                .FirstOrDefaultAsync(e => e.SerialNumber == request.SerialNumber);

            if (existingEquipment != null)
                throw new Exception("Equipment with this serial number already exists");

            // Check if room exists (if provided)
            if (request.RoomId.HasValue)
            {
                var room = await _db.Rooms
                    .FirstOrDefaultAsync(r => r.Id == request.RoomId.Value);
                
                if (room == null)
                    throw new Exception("Room not found");
            }

            var equipment = new DomainLayer.Entities.Equipment
            {
                Id = Guid.NewGuid(),
                Name = request.Name,
                Description = request.Description,
                SerialNumber = request.SerialNumber,
                Type = request.Type,
                ImageUrl = request.ImageUrl,
                RoomId = request.RoomId,
                LastMaintenanceDate = request.LastMaintenanceDate,
                NextMaintenanceDate = request.NextMaintenanceDate,
                Status = EquipmentStatus.Available,
                CreatedAt = DateTime.UtcNow,
                LastUpdatedAt = DateTime.UtcNow
            };

            _db.Equipments.Add(equipment);
            await _db.SaveChangesAsync();

            await InvalidateEquipmentCaches(equipment.Id, equipment.RoomId);
            return await GetEquipmentByIdAsync(equipment.Id);
        }

        public async Task<EquipmentDetail> UpdateEquipmentAsync(Guid id, UpdateEquipmentRequest request)
        {
            var equipment = await _db.Equipments
                .FirstOrDefaultAsync(e => e.Id == id)
                ?? throw new Exception("Equipment not found");

            var previousRoomId = equipment.RoomId;

            // Check if serial number already exists (if changed)
            if (!string.IsNullOrWhiteSpace(request.SerialNumber) && request.SerialNumber != equipment.SerialNumber)
            {
                var existingEquipment = await _db.Equipments
                    .FirstOrDefaultAsync(e => e.SerialNumber == request.SerialNumber && e.Id != id);

                if (existingEquipment != null)
                    throw new Exception("Equipment with this serial number already exists");
            }

            // Check if room exists (if provided)
            if (request.RoomId.HasValue)
            {
                var room = await _db.Rooms
                    .FirstOrDefaultAsync(r => r.Id == request.RoomId.Value);
                
                if (room == null)
                    throw new Exception("Room not found");
            }

            if (!string.IsNullOrWhiteSpace(request.Name))
                equipment.Name = request.Name;
            
            if (!string.IsNullOrWhiteSpace(request.Description))
                equipment.Description = request.Description;
            
            if (!string.IsNullOrWhiteSpace(request.SerialNumber))
                equipment.SerialNumber = request.SerialNumber;
            
            if (request.Type.HasValue)
                equipment.Type = request.Type.Value;
            
            if (request.ImageUrl != null)
                equipment.ImageUrl = request.ImageUrl;
            
            if (request.RoomId.HasValue)
                equipment.RoomId = request.RoomId.Value;
            
            if (request.LastMaintenanceDate.HasValue)
                equipment.LastMaintenanceDate = request.LastMaintenanceDate.Value;
            
            if (request.NextMaintenanceDate.HasValue)
                equipment.NextMaintenanceDate = request.NextMaintenanceDate.Value;

            equipment.LastUpdatedAt = DateTime.UtcNow;
            _db.Equipments.Update(equipment);
            await _db.SaveChangesAsync();

            await InvalidateEquipmentCaches(equipment.Id, equipment.RoomId, previousRoomId);
            return await GetEquipmentByIdAsync(equipment.Id);
        }

        public async Task<EquipmentDetail> UpdateEquipmentStatusAsync(Guid id, UpdateEquipmentStatusRequest request)
        {
            var equipment = await _db.Equipments
                .FirstOrDefaultAsync(e => e.Id == id)
                ?? throw new Exception("Equipment not found");

            equipment.Status = request.Status;
            equipment.LastUpdatedAt = DateTime.UtcNow;

            _db.Equipments.Update(equipment);
            await _db.SaveChangesAsync();

            await InvalidateEquipmentCaches(equipment.Id, equipment.RoomId);
            return await GetEquipmentByIdAsync(equipment.Id);
        }

        public async Task DeleteEquipmentAsync(Guid id)
        {
            var equipment = await _db.Equipments
                .FirstOrDefaultAsync(e => e.Id == id)
                ?? throw new Exception("Equipment not found");

            // Check if equipment is currently in use
            if (equipment.Status == EquipmentStatus.InUse)
                throw new Exception("Cannot delete equipment that is currently in use");

            _db.Equipments.Remove(equipment);
            await _db.SaveChangesAsync();

            await InvalidateEquipmentCaches(equipment.Id, equipment.RoomId);
        }

        public async Task<IReadOnlyList<EquipmentListItem>> GetEquipmentsByRoomAsync(Guid roomId)
        {
            var cacheKey = RedisCacheKeyBuilder.Build("equipments:by-room:v1", ("roomId", roomId));
            var cached = await _redis.GetAsync<IReadOnlyList<EquipmentListItem>>(cacheKey);
            if (cached != null) return cached;

            var equipments = await _db.Equipments
                .Include(e => e.Room)
                .Where(e => e.RoomId == roomId)
                .OrderBy(e => e.Name)
                .ToListAsync();

            var result = equipments.Select(e => new EquipmentListItem
            {
                Id = e.Id,
                Name = e.Name,
                Description = e.Description,
                SerialNumber = e.SerialNumber,
                Type = e.Type.ToString(),
                Status = e.Status.ToString(),
                ImageUrl = e.ImageUrl,
                RoomName = e.Room?.Name,
                LastMaintenanceDate = e.LastMaintenanceDate,
                NextMaintenanceDate = e.NextMaintenanceDate
            }).ToList();

            await _redis.SetAsync(cacheKey, result, RedisCacheDefaults.DefaultTtl);
            return result;
        }

        public async Task<IReadOnlyList<EquipmentListItem>> GetAvailableEquipmentsAsync()
        {
            const string cacheKey = "equipments:available:v1";
            var cached = await _redis.GetAsync<IReadOnlyList<EquipmentListItem>>(cacheKey);
            if (cached != null) return cached;

            var equipments = await _db.Equipments
                .Include(e => e.Room)
                .Where(e => e.Status == EquipmentStatus.Available)
                .OrderBy(e => e.Name)
                .ToListAsync();

            var result = equipments.Select(e => new EquipmentListItem
            {
                Id = e.Id,
                Name = e.Name,
                Description = e.Description,
                SerialNumber = e.SerialNumber,
                Type = e.Type.ToString(),
                Status = e.Status.ToString(),
                ImageUrl = e.ImageUrl,
                RoomName = e.Room?.Name,
                LastMaintenanceDate = e.LastMaintenanceDate,
                NextMaintenanceDate = e.NextMaintenanceDate
            }).ToList();

            await _redis.SetAsync(cacheKey, result, RedisCacheDefaults.DefaultTtl);
            return result;
        }

        public async Task<int> GetEquipmentCountAsync()
        {
            const string cacheKey = "equipments:count:v1";
            var cached = await _redis.GetAsync<int?>(cacheKey);
            if (cached.HasValue) return cached.Value;

            var count = await _db.Equipments.CountAsync();
            await _redis.SetAsync(cacheKey, count, RedisCacheDefaults.DefaultTtl);
            return count;
        }

        public async Task<int> GetAvailableEquipmentCountAsync()
        {
            const string cacheKey = "equipments:available-count:v1";
            var cached = await _redis.GetAsync<int?>(cacheKey);
            if (cached.HasValue) return cached.Value;

            var count = await _db.Equipments
                .CountAsync(e => e.Status == EquipmentStatus.Available);
            await _redis.SetAsync(cacheKey, count, RedisCacheDefaults.DefaultTtl);
            return count;
        }

        public async Task<IReadOnlyList<EquipmentListItem>> GetEquipmentsNeedingMaintenanceAsync()
        {
            var now = DateTime.UtcNow;
            var roundedKeyTime = new DateTime(now.Year, now.Month, now.Day, now.Hour, 0, 0, DateTimeKind.Utc);
            var cacheKey = RedisCacheKeyBuilder.Build("equipments:maintenance:v1", ("untilHour", roundedKeyTime));
            var cached = await _redis.GetAsync<IReadOnlyList<EquipmentListItem>>(cacheKey);
            if (cached != null) return cached;

            var equipments = await _db.Equipments
                .Include(e => e.Room)
                .Where(e => e.NextMaintenanceDate.HasValue && e.NextMaintenanceDate <= now)
                .OrderBy(e => e.NextMaintenanceDate)
                .ToListAsync();

            var result = equipments.Select(e => new EquipmentListItem
            {
                Id = e.Id,
                Name = e.Name,
                Description = e.Description,
                SerialNumber = e.SerialNumber,
                Type = e.Type.ToString(),
                Status = e.Status.ToString(),
                ImageUrl = e.ImageUrl,
                RoomName = e.Room?.Name,
                LastMaintenanceDate = e.LastMaintenanceDate,
                NextMaintenanceDate = e.NextMaintenanceDate
            }).ToList();

            await _redis.SetAsync(cacheKey, result, RedisCacheDefaults.DefaultTtl);
            return result;
        }

        private static string BuildEquipmentListCacheKey(EquipmentFilterRequest? filter)
        {
            if (filter == null)
            {
                return "equipments:list:v1";
            }

            return RedisCacheKeyBuilder.Build(
                "equipments:list:v1",
                ("name", filter.Name),
                ("serial", filter.SerialNumber),
                ("type", filter.Type),
                ("status", filter.Status),
                ("roomId", filter.RoomId),
                ("page", filter.Page),
                ("pageSize", filter.PageSize));
        }

        private async Task InvalidateEquipmentCaches(Guid equipmentId, Guid? currentRoomId = null, Guid? previousRoomId = null)
        {
            await _redis.RemoveAsync("equipments:list:v1");
            await _redis.RemoveAsync("equipments:available:v1");
            await _redis.RemoveAsync("equipments:count:v1");
            await _redis.RemoveAsync("equipments:available-count:v1");
            await _redis.RemoveAsync("equipments:maintenance:v1");
            await _redis.RemoveAsync(RedisCacheKeyBuilder.Build("equipments:detail:v1", ("id", equipmentId)));

            if (currentRoomId.HasValue)
            {
                await _redis.RemoveAsync(RedisCacheKeyBuilder.Build("equipments:by-room:v1", ("roomId", currentRoomId.Value)));
            }

            if (previousRoomId.HasValue && previousRoomId != currentRoomId)
            {
                await _redis.RemoveAsync(RedisCacheKeyBuilder.Build("equipments:by-room:v1", ("roomId", previousRoomId.Value)));
            }
        }
    }
}
