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
                .Include(l => l.Rooms)
                .AsQueryable();

            if (filter != null)
            {
                if (!string.IsNullOrEmpty(filter.Name))
                    query = query.Where(l => l.Name.Contains(filter.Name));
                
                if (!string.IsNullOrEmpty(filter.Location))
                    query = query.Where(l => l.Location != null && l.Location.Contains(filter.Location));
                
                if (filter.Status.HasValue)
                    query = query.Where(l => l.Status == filter.Status.Value);
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
                Location = l.Location,
                Status = l.Status.ToString(),
                RoomCount = l.Rooms.Count,
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
                .Include(l => l.Rooms)
                .FirstOrDefaultAsync(l => l.Id == id)
                ?? throw new Exception("Lab not found");

            // Get equipment count for this lab (sum of equipment in all rooms)
            var roomIds = lab.Rooms.Select(r => r.Id).ToList();
            var equipmentCount = roomIds.Any() 
                ? await _db.Equipments.CountAsync(e => e.RoomId.HasValue && roomIds.Contains(e.RoomId.Value))
                : 0;

            // Get active bookings for this lab (sum of bookings in all rooms)
            var activeBookings = roomIds.Any()
                ? await _db.Bookings.CountAsync(b => roomIds.Contains(b.RoomId) && 
                                b.Status == BookingStatus.Approved && 
                                b.StartTime <= DateTime.UtcNow && 
                                b.EndTime >= DateTime.UtcNow)
                : 0;

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

            // Get all rooms for this lab
            var rooms = lab.Rooms.Select(r => new RoomInfo
            {
                Id = r.Id,
                Name = r.Name,
                Capacity = r.Capacity,
                Status = r.Status.ToString()
            }).ToList();

            return new LabDetail
            {
                Id = lab.Id,
                Name = lab.Name,
                Location = lab.Location,
                Status = lab.Status.ToString(),
                RoomCount = lab.Rooms.Count,
                MemberCount = lab.Members.Count,
                EquipmentCount = equipmentCount,
                ActiveBookings = activeBookings,
                CreatedAt = lab.CreatedAt,
                LastUpdatedAt = lab.LastUpdatedAt,
                Rooms = rooms,
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

            // Check if lab name already exists
            var existingLab = await _db.Labs.AnyAsync(l => l.Name == request.Name);
            if (existingLab)
                throw new Exception("Lab with this name already exists");

            var lab = new DomainLayer.Entities.Lab
            {
                Id = Guid.NewGuid(),
                Name = request.Name,
                Location = request.Location,
                Status = request.Status,
                CreatedAt = DateTime.UtcNow,
                LastUpdatedAt = DateTime.UtcNow
            };

            _db.Labs.Add(lab);
            
            // Assign rooms to lab if provided
            if (request.RoomIds != null && request.RoomIds.Any())
            {
                // Validate all rooms exist
                var rooms = await _db.Rooms
                    .Where(r => request.RoomIds.Contains(r.Id))
                    .ToListAsync();
                
                if (rooms.Count != request.RoomIds.Count)
                {
                    var foundIds = rooms.Select(r => r.Id).ToList();
                    var notFoundIds = request.RoomIds.Except(foundIds).ToList();
                    throw new Exception($"One or more rooms not found. Room IDs not found: {string.Join(", ", notFoundIds)}");
                }
                
                // Check if any room is already assigned to another lab
                var alreadyAssignedRooms = rooms.Where(r => r.LabId.HasValue && r.LabId != lab.Id).ToList();
                if (alreadyAssignedRooms.Any())
                {
                    var assignedNames = string.Join(", ", alreadyAssignedRooms.Select(r => r.Name));
                    throw new Exception($"One or more rooms are already assigned to another lab. Rooms: {assignedNames}");
                }
                
                // Assign rooms to this lab
                foreach (var room in rooms)
                {
                    room.LabId = lab.Id;
                    room.LastUpdatedAt = DateTime.UtcNow;
                }
                
                _db.Rooms.UpdateRange(rooms);
            }
            
            await _db.SaveChangesAsync();

            // Log creation event - AC-05
            var roomInfo = request.RoomIds != null && request.RoomIds.Any() 
                ? $"Rooms assigned: {string.Join(", ", request.RoomIds)}" 
                : null;
            await LogLabActionAsync(adminId, lab.Id, lab.Name, "Create", roomInfo);

            return await GetLabByIdAsync(lab.Id);
        }

        public async Task<LabDetail> UpdateLabAsync(Guid id, UpdateLabRequest request, Guid adminId)
        {
            var lab = await _db.Labs
                .Include(l => l.Rooms)
                .FirstOrDefaultAsync(l => l.Id == id)
                ?? throw new Exception("Lab not found");

            var changes = new List<string>();
            var originalLab = new { lab.Name, lab.Location, lab.Status };

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
            
            if (request.Location != null && request.Location != lab.Location)
            {
                changes.Add($"Location: '{lab.Location}' -> '{request.Location}'");
                lab.Location = request.Location;
            }

            if (request.Status.HasValue && request.Status.Value != lab.Status)
            {
                changes.Add($"Status: {lab.Status} -> {request.Status.Value}");
                lab.Status = request.Status.Value;
            }

            // Update rooms if provided
            if (request.RoomIds != null)
            {
                var currentRoomIds = lab.Rooms.Select(r => r.Id).OrderBy(x => x).ToList();
                var newRoomIds = request.RoomIds.OrderBy(x => x).ToList();
                
                if (!currentRoomIds.SequenceEqual(newRoomIds))
                {
                    // Validate all rooms exist
                    var rooms = await _db.Rooms
                        .Where(r => request.RoomIds.Contains(r.Id))
                        .ToListAsync();
                    
                    if (rooms.Count != request.RoomIds.Count)
                    {
                        var foundIds = rooms.Select(r => r.Id).ToList();
                        var notFoundIds = request.RoomIds.Except(foundIds).ToList();
                        throw new Exception($"One or more rooms not found. Room IDs not found: {string.Join(", ", notFoundIds)}");
                    }
                    
                    // Check if any room is already assigned to another lab
                    var alreadyAssignedRooms = rooms.Where(r => r.LabId.HasValue && r.LabId != lab.Id).ToList();
                    if (alreadyAssignedRooms.Any())
                    {
                        var assignedNames = string.Join(", ", alreadyAssignedRooms.Select(r => r.Name));
                        throw new Exception($"One or more rooms are already assigned to another lab. Rooms: {assignedNames}");
                    }
                    
                    // Remove current rooms from this lab
                    foreach (var room in lab.Rooms.ToList())
                    {
                        room.LabId = null;
                        room.LastUpdatedAt = DateTime.UtcNow;
                    }
                    
                    // Assign new rooms to this lab
                    foreach (var room in rooms)
                    {
                        room.LabId = lab.Id;
                        room.LastUpdatedAt = DateTime.UtcNow;
                    }
                    
                    _db.Rooms.UpdateRange(lab.Rooms);
                    _db.Rooms.UpdateRange(rooms);
                    
                    changes.Add($"Rooms: [{string.Join(", ", currentRoomIds)}] -> [{string.Join(", ", newRoomIds)}]");
                }
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
                .Include(l => l.Rooms)
                .FirstOrDefaultAsync(l => l.Id == id)
                ?? throw new Exception("Lab not found");

            // AC-01: Check if lab has pending or approved bookings in any of its rooms
            var roomIds = lab.Rooms.Select(r => r.Id).ToList();
            var hasActiveBookings = await _db.Bookings
                .AnyAsync(b => roomIds.Contains(b.RoomId) && 
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
                .Include(l => l.Rooms)
                .Where(l => l.Status == LabStatus.Active)
                .ToListAsync();

            return labs.Select(l => new LabListItem
            {
                Id = l.Id,
                Name = l.Name,
                Location = l.Location,
                Status = l.Status.ToString(),
                RoomCount = l.Rooms.Count,
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
