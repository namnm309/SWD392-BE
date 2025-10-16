using Application.DTOs.Lab;
using DomainLayer.Entities;
using DomainLayer.Enum;
using InfrastructureLayer.Data;
using Microsoft.EntityFrameworkCore;

namespace Application.Services.Lab
{
    public class LabService : ILabService
    {
        private readonly LabDbContext _db;

        public LabService(LabDbContext db)
        {
            _db = db;
        }

        public async Task<IReadOnlyList<LabListItem>> GetAllLabsAsync(LabFilterRequest? filter = null)
        {
            var query = _db.Labs
                .Include(l => l.Members)
                    .ThenInclude(m => m.User)
                .Include(l => l.Room)
                .AsQueryable();

            if (filter != null)
            {
                if (!string.IsNullOrEmpty(filter.Name))
                    query = query.Where(l => l.Name.Contains(filter.Name));
                
                if (!string.IsNullOrEmpty(filter.Location))
                    query = query.Where(l => l.Location != null && l.Location.Contains(filter.Location));
                
                if (filter.Status.HasValue)
                    query = query.Where(l => l.Status == filter.Status.Value);
                
                if (filter.MinCapacity.HasValue)
                    query = query.Where(l => l.Capacity >= filter.MinCapacity.Value);
                
                if (filter.MaxCapacity.HasValue)
                    query = query.Where(l => l.Capacity <= filter.MaxCapacity.Value);
            }

            query = query.OrderBy(l => l.Name);

            if (filter?.Page.HasValue == true && filter.PageSize.HasValue)
            {
                query = query.Skip(filter.Page.Value * filter.PageSize.Value)
                           .Take(filter.PageSize.Value);
            }

            var labs = await query.ToListAsync();

            return labs.Select(l => new LabListItem
            {
                Id = l.Id,
                Name = l.Name,
                Description = l.Description,
                Location = l.Location,
                Capacity = l.Capacity,
                Status = l.Status.ToString(),
                RoomId = l.RoomId,
                RoomName = l.Room?.Name,
                MemberCount = l.Members.Count(m => m.Status == LabMemberStatus.Active),
                EquipmentCount = 0, // Will be calculated separately if needed
                ActiveBookings = 0 // Will be calculated separately if needed
            }).ToList();
        }

        public async Task<LabDetail> GetLabByIdAsync(Guid id)
        {
            var lab = await _db.Labs
                .Include(l => l.Members.Where(m => m.Status == LabMemberStatus.Active))
                    .ThenInclude(m => m.User)
                .Include(l => l.Room)
                .FirstOrDefaultAsync(l => l.Id == id)
                ?? throw new Exception("Lab not found");

            // Get equipment count for this lab (if equipment is linked to lab)
            var equipmentCount = await _db.Equipments
                .CountAsync(e => e.RoomId == null); // Assuming equipment can be lab-level

            // Get active bookings for this lab (if bookings are linked to lab)
            var activeBookings = await _db.Bookings
                .CountAsync(b => b.Status == BookingStatus.Approved && 
                                b.StartTime <= DateTime.UtcNow && 
                                b.EndTime >= DateTime.UtcNow);

            var members = lab.Members.Select(m => new LabMemberInfo
            {
                Id = m.Id,
                UserId = m.UserId,
                UserName = m.User.Fullname,
                UserEmail = m.User.Email,
                Role = m.Role.ToString(),
                Status = m.Status.ToString(),
                JoinedAt = m.JoinedAt
            }).ToList();

            // Get lab equipment (if any)
            var equipments = new List<EquipmentInfo>();

            // Get recent bookings (if any)
            var recentBookings = new List<BookingInfo>();

            // Get room info
            var roomInfo = lab.Room != null ? new RoomInfo
            {
                Id = lab.Room.Id,
                Name = lab.Room.Name,
                Description = lab.Room.Description,
                Location = lab.Room.Location,
                Capacity = lab.Room.Capacity,
                Status = lab.Room.Status.ToString()
            } : null;

            return new LabDetail
            {
                Id = lab.Id,
                Name = lab.Name,
                Description = lab.Description,
                Location = lab.Location,
                Capacity = lab.Capacity,
                Status = lab.Status.ToString(),
                RoomId = lab.RoomId,
                RoomName = lab.Room?.Name,
                MemberCount = lab.Members.Count,
                EquipmentCount = equipmentCount,
                ActiveBookings = activeBookings,
                CreatedAt = lab.CreatedAt,
                LastUpdatedAt = lab.LastUpdatedAt,
                Room = roomInfo,
                Members = members,
                Equipments = equipments,
                RecentBookings = recentBookings
            };
        }

        public async Task<LabDetail> CreateLabAsync(CreateLabRequest request, Guid adminId)
        {
            // Validation - AC-02: Required fields cannot be null
            if (string.IsNullOrWhiteSpace(request.Name))
                throw new Exception("Lab Name is required");
            
            if (request.Capacity <= 0)
                throw new Exception("Capacity must be greater than 0");

            // Check if room exists if provided
            if (request.RoomId.HasValue)
            {
                var roomExists = await _db.Rooms.AnyAsync(r => r.Id == request.RoomId.Value);
                if (!roomExists)
                    throw new Exception("Room not found");
            }

            // Check if lab name already exists
            var existingLab = await _db.Labs.AnyAsync(l => l.Name == request.Name);
            if (existingLab)
                throw new Exception("Lab with this name already exists");

            var lab = new DomainLayer.Entities.Lab
            {
                Id = Guid.NewGuid(),
                Name = request.Name,
                Description = request.Description,
                Location = request.Location,
                Capacity = request.Capacity,
                RoomId = request.RoomId,
                Status = request.Status,
                CreatedAt = DateTime.UtcNow,
                LastUpdatedAt = DateTime.UtcNow
            };

            _db.Labs.Add(lab);
            await _db.SaveChangesAsync();

            // Log creation event - AC-05
            await LogLabActionAsync(adminId, lab.Id, lab.Name, "Create", null);

            return await GetLabByIdAsync(lab.Id);
        }

        public async Task<LabDetail> UpdateLabAsync(Guid id, UpdateLabRequest request, Guid adminId)
        {
            var lab = await _db.Labs
                .FirstOrDefaultAsync(l => l.Id == id)
                ?? throw new Exception("Lab not found");

            var changes = new List<string>();
            var originalLab = new { lab.Name, lab.Description, lab.Location, lab.Capacity, lab.RoomId, lab.Status };

            // Validation - AC-02: Required fields cannot be null
            if (!string.IsNullOrWhiteSpace(request.Name) && request.Name != lab.Name)
            {
                if (string.IsNullOrWhiteSpace(request.Name))
                    throw new Exception("Lab Name cannot be empty");
                
                // Check if new name already exists
                var existingLab = await _db.Labs.AnyAsync(l => l.Name == request.Name && l.Id != id);
                if (existingLab)
                    throw new Exception("Lab with this name already exists");
                
                changes.Add($"Name: '{lab.Name}' -> '{request.Name}'");
                lab.Name = request.Name;
            }
            
            if (request.Description != null && request.Description != lab.Description)
            {
                changes.Add($"Description: '{lab.Description}' -> '{request.Description}'");
                lab.Description = request.Description;
            }
            
            if (request.Location != null && request.Location != lab.Location)
            {
                changes.Add($"Location: '{lab.Location}' -> '{request.Location}'");
                lab.Location = request.Location;
            }
            
            if (request.Capacity.HasValue && request.Capacity.Value != lab.Capacity)
            {
                if (request.Capacity.Value <= 0)
                    throw new Exception("Capacity must be greater than 0");
                
                changes.Add($"Capacity: {lab.Capacity} -> {request.Capacity.Value}");
                lab.Capacity = request.Capacity.Value;
            }

            if (request.RoomId != lab.RoomId)
            {
                if (request.RoomId.HasValue)
                {
                    var roomExists = await _db.Rooms.AnyAsync(r => r.Id == request.RoomId.Value);
                    if (!roomExists)
                        throw new Exception("Room not found");
                }
                
                changes.Add($"RoomId: {lab.RoomId} -> {request.RoomId}");
                lab.RoomId = request.RoomId;
            }

            if (request.Status.HasValue && request.Status.Value != lab.Status)
            {
                changes.Add($"Status: {lab.Status} -> {request.Status.Value}");
                lab.Status = request.Status.Value;
            }

            lab.LastUpdatedAt = DateTime.UtcNow;
            _db.Labs.Update(lab);
            await _db.SaveChangesAsync();

            // Log edit event - AC-05
            await LogLabActionAsync(adminId, lab.Id, lab.Name, "Update", string.Join("; ", changes));

            return await GetLabByIdAsync(lab.Id);
        }

        public async Task<LabDetail> UpdateLabStatusAsync(Guid id, UpdateLabStatusRequest request)
        {
            var lab = await _db.Labs
                .FirstOrDefaultAsync(l => l.Id == id)
                ?? throw new Exception("Lab not found");

            lab.Status = request.Status;
            lab.LastUpdatedAt = DateTime.UtcNow;

            _db.Labs.Update(lab);
            await _db.SaveChangesAsync();

            return await GetLabByIdAsync(lab.Id);
        }

        public async Task DeleteLabAsync(Guid id, DeleteLabRequest request, Guid adminId)
        {
            var lab = await _db.Labs
                .FirstOrDefaultAsync(l => l.Id == id)
                ?? throw new Exception("Lab not found");

            // AC-01: Check if lab has pending or approved bookings
            var hasActiveBookings = await _db.Bookings
                .AnyAsync(b => b.RoomId == lab.RoomId && 
                              (b.Status == BookingStatus.Pending || b.Status == BookingStatus.Approved));

            if (hasActiveBookings)
                throw new Exception("Cannot delete lab with active bookings");

            // Check if lab has active members
            var hasActiveMembers = await _db.LabMembers
                .AnyAsync(m => m.LabId == id && m.Status == LabMemberStatus.Active);

            if (hasActiveMembers)
                throw new Exception("Cannot delete lab with active members");

            // AC-03: Admin must confirm deletion before removing
            if (!request.ConfirmDeletion)
                throw new Exception("Deletion must be confirmed");

            // Log deletion event - AC-06
            await LogLabActionAsync(adminId, lab.Id, lab.Name, "Delete", null);

            _db.Labs.Remove(lab);
            await _db.SaveChangesAsync();
        }

        public async Task<IReadOnlyList<LabListItem>> GetAvailableLabsAsync()
        {
            var labs = await _db.Labs
                .Include(l => l.Members)
                .Where(l => l.Status == LabStatus.Active)
                .ToListAsync();

            return labs.Select(l => new LabListItem
            {
                Id = l.Id,
                Name = l.Name,
                Description = l.Description,
                Location = l.Location,
                Capacity = l.Capacity,
                Status = l.Status.ToString(),
                MemberCount = l.Members.Count(m => m.Status == LabMemberStatus.Active),
                EquipmentCount = 0,
                ActiveBookings = 0
            }).ToList();
        }

        public async Task<bool> IsLabAvailableAsync(Guid labId)
        {
            var lab = await _db.Labs
                .FirstOrDefaultAsync(l => l.Id == labId);

            return lab != null && lab.Status == LabStatus.Active;
        }

        public async Task<int> GetLabCountAsync()
        {
            return await _db.Labs.CountAsync();
        }

        public async Task<int> GetActiveLabCountAsync()
        {
            return await _db.Labs
                .CountAsync(l => l.Status == LabStatus.Active);
        }

        private async Task LogLabActionAsync(Guid adminId, Guid labId, string labName, string action, string? changes)
        {
            // Get admin name
            var admin = await _db.Users
                .FirstOrDefaultAsync(u => u.Id == adminId);
            
            var adminName = admin?.Fullname ?? "Unknown Admin";

            // Log to console for now (in real app, you might want to log to database or file)
            Console.WriteLine($"[LAB LOG] {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} - " +
                            $"Admin: {adminName} ({adminId}) - " +
                            $"Action: {action} - " +
                            $"Lab: {labName} ({labId}) - " +
                            $"Changes: {changes ?? "N/A"}");
        }
    }
}
