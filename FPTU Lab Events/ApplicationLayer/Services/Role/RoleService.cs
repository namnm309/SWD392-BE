using Application.DTOs.Role;
using DomainLayer.Entities;
using InfrastructureLayer.Data;
using Microsoft.EntityFrameworkCore;

namespace Application.Services.Role;

public class RoleService : IRoleService
{
    private readonly LabDbContext _db;
    public RoleService(LabDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<RoleListItem>> ListAsync(RoleFilterRequest? filter = null)
    {
        var query = _db.Roles.AsQueryable();
        if (filter != null)
        {
            var page = filter.Page ?? 1; // 1-based index
            var pageSize = filter.PageSize ?? 20;
            if (page < 1) page = 1;
            if (pageSize <= 0) pageSize = 20;

            var skip = (page - 1) * pageSize;
            query = query
                .OrderBy(r => r.CreatedAt)
                .ThenBy(r => r.Id)
                .Skip(skip)
                .Take(pageSize);
        }

        var list = await query.ToListAsync();
        return list.Select(r => new RoleListItem
        {
            Id = r.Id,
            Name = r.name,
            Description = r.description
        }).ToList();
    }

    public async Task<RoleDetail> GetByIdAsync(Guid id)
    {
        var role = await _db.Roles.Include(r => r.Users).FirstOrDefaultAsync(r => r.Id == id)
            ?? throw new Exception("Role not found");
        return new RoleDetail
        {
            Id = role.Id,
            Name = role.name,
            Description = role.description,
            UsersCount = role.Users.Count
        };
    }

    public async Task<RoleDetail> CreateAsync(CreateRoleRequest request)
    {
        var name = request.Name.Trim();
        var description = request.Description.Trim();
        var exists = await _db.Roles.AnyAsync(r => r.name.ToLower() == name.ToLower());
        if (exists) throw new Exception("Role name already exists");

        var role = new Roles
        {
            Id = Guid.NewGuid(),
            name = name,
            description = description,
            CreatedAt = DateTime.UtcNow,
            LastUpdatedAt = DateTime.UtcNow
        };

        _db.Roles.Add(role);
        await _db.SaveChangesAsync();
        return await GetByIdAsync(role.Id);
    }

    public async Task<RoleDetail> UpdateAsync(Guid id, UpdateRoleRequest request)
    {
        var role = await _db.Roles.Include(r => r.Users).FirstOrDefaultAsync(r => r.Id == id)
            ?? throw new Exception("Role not found");

        if (!string.IsNullOrWhiteSpace(request.Name))
        {
            var newName = request.Name.Trim();
            var exists = await _db.Roles.AnyAsync(r => r.Id != id && r.name.ToLower() == newName.ToLower());
            if (exists) throw new Exception("Role name already exists");
            role.name = newName;
        }

        if (!string.IsNullOrWhiteSpace(request.Description))
        {
            role.description = request.Description.Trim();
        }

        role.LastUpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return await GetByIdAsync(role.Id);
    }

    public async Task DeleteAsync(Guid id)
    {
        var role = await _db.Roles.FirstOrDefaultAsync(r => r.Id == id)
            ?? throw new Exception("Role not found");
        _db.Roles.Remove(role);
        await _db.SaveChangesAsync();
    }
}


