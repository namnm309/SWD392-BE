using Application.DTOs.Equipment;
using DomainLayer.Entities;
using DomainLayer.Enum;
using InfrastructureLayer.Data;
using Microsoft.EntityFrameworkCore;

namespace Application.Services.Equipment
{
    public class EquipmentService : IEquipmentService
    {
        private readonly LabDbContext _db;

        public EquipmentService(LabDbContext db)
        {
            _db = db;
        }

        public async Task<IReadOnlyList<EquipmentListItem>> GetAllEquipmentsAsync(EquipmentFilterRequest? filter = null)
        {
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

            return equipments.Select(e => new EquipmentListItem
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
        }

        public async Task<EquipmentDetail> GetEquipmentByIdAsync(Guid id)
        {
            var equipment = await _db.Equipments
                .Include(e => e.Room)
                .FirstOrDefaultAsync(e => e.Id == id)
                ?? throw new Exception("Equipment not found");

            return new EquipmentDetail
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

            return await GetEquipmentByIdAsync(equipment.Id);
        }

        public async Task<EquipmentDetail> UpdateEquipmentAsync(Guid id, UpdateEquipmentRequest request)
        {
            var equipment = await _db.Equipments
                .FirstOrDefaultAsync(e => e.Id == id)
                ?? throw new Exception("Equipment not found");

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
        }

        public async Task<IReadOnlyList<EquipmentListItem>> GetEquipmentsByRoomAsync(Guid roomId)
        {
            var equipments = await _db.Equipments
                .Include(e => e.Room)
                .Where(e => e.RoomId == roomId)
                .OrderBy(e => e.Name)
                .ToListAsync();

            return equipments.Select(e => new EquipmentListItem
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
        }

        public async Task<IReadOnlyList<EquipmentListItem>> GetAvailableEquipmentsAsync()
        {
            var equipments = await _db.Equipments
                .Include(e => e.Room)
                .Where(e => e.Status == EquipmentStatus.Available)
                .OrderBy(e => e.Name)
                .ToListAsync();

            return equipments.Select(e => new EquipmentListItem
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
        }

        public async Task<int> GetEquipmentCountAsync()
        {
            return await _db.Equipments.CountAsync();
        }

        public async Task<int> GetAvailableEquipmentCountAsync()
        {
            return await _db.Equipments
                .CountAsync(e => e.Status == EquipmentStatus.Available);
        }

        public async Task<IReadOnlyList<EquipmentListItem>> GetEquipmentsNeedingMaintenanceAsync()
        {
            var now = DateTime.UtcNow;
            var equipments = await _db.Equipments
                .Include(e => e.Room)
                .Where(e => e.NextMaintenanceDate.HasValue && e.NextMaintenanceDate <= now)
                .OrderBy(e => e.NextMaintenanceDate)
                .ToListAsync();

            return equipments.Select(e => new EquipmentListItem
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
        }
    }
}
