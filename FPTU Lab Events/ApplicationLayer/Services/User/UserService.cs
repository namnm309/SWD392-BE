using Application.DTOs.User;
using DomainLayer.Entities;
using DomainLayer.Enum;
using InfrastructureLayer.Data;
using Microsoft.EntityFrameworkCore;

namespace Application.Services.User;

public class UserService : IUserService
{
  private readonly LabDbContext _db;
  public UserService(LabDbContext db) { _db = db; }

  public async Task<IReadOnlyList<UserListItem>> ListAsync(int? page = null, int? pageSize = null)
  {
    var query = _db.Users.Include(u => u.Roles).AsQueryable();
    if (page.HasValue && pageSize.HasValue)
    {
      query = query.Skip(page.Value * pageSize.Value).Take(pageSize.Value);
    }
    var list = await query.ToListAsync();
    return list.Select(u => new UserListItem
    {
      Id = u.Id,
      Email = u.Email,
      Username = u.Username,
      Fullname = u.Fullname,
      Roles = u.Roles.Select(r => r.name).ToArray(),
      Status = u.status.ToString()
    }).ToList();
  }

  public async Task<UserDetail> GetByIdAsync(Guid id)
  {
    var u = await _db.Users.Include(x => x.Roles).FirstOrDefaultAsync(x => x.Id == id)
      ?? throw new Exception("User not found");
    return new UserDetail
    {
      Id = u.Id,
      Email = u.Email,
      Username = u.Username,
      Fullname = u.Fullname,
      MSSV = u.MSSV,
      Roles = u.Roles.Select(r => r.name).ToArray(),
      Status = u.status.ToString()
    };
  }

  public async Task<UserDetail> CreateAsync(CreateUserRequest request)
  {
    var email = request.Email.Trim().ToLowerInvariant();
    var username = request.Username.Trim();
    if (await _db.Users.AnyAsync(u => u.Email.ToLower() == email)) throw new Exception("Email already exists");
    if (await _db.Users.AnyAsync(u => u.Username == username)) throw new Exception("Username already exists");

    var user = new Users
    {
      Id = Guid.NewGuid(),
      Email = email,
      Username = username,
      Fullname = request.Fullname,
      Password = BCrypt.Net.BCrypt.HashPassword(request.Password),
      MSSV = request.MSSV,
      status = UserStatus.Active,
      CreatedAt = DateTime.UtcNow,
      LastUpdatedAt = DateTime.UtcNow
    };

    if (request.Roles != null && request.Roles.Length > 0)
    {
      var roles = await _db.Roles.Where(r => request.Roles.Contains(r.name)).ToListAsync();
      foreach (var r in roles) user.Roles.Add(r);
    }
    else
    {
      var student = await _db.Roles.FirstAsync(r => r.name == "Student");
      user.Roles.Add(student);
    }

    _db.Users.Add(user);
    await _db.SaveChangesAsync();

    return await GetByIdAsync(user.Id);
  }

  public async Task<UserDetail> UpdateAsync(Guid id, UpdateUserRequest request)
  {
    var u = await _db.Users.Include(x => x.Roles).FirstOrDefaultAsync(x => x.Id == id)
      ?? throw new Exception("User not found");

    if (!string.IsNullOrWhiteSpace(request.Fullname)) u.Fullname = request.Fullname.Trim();
    if (!string.IsNullOrWhiteSpace(request.MSSV)) u.MSSV = request.MSSV.Trim();

    if (request.Roles != null)
    {
      var roles = await _db.Roles.Where(r => request.Roles.Contains(r.name)).ToListAsync();
      u.Roles.Clear();
      foreach (var r in roles) u.Roles.Add(r);
    }

    u.LastUpdatedAt = DateTime.UtcNow;
    await _db.SaveChangesAsync();

    return await GetByIdAsync(u.Id);
  }

  public async Task DeleteAsync(Guid id)
  {
    var u = await _db.Users.FirstOrDefaultAsync(x => x.Id == id)
      ?? throw new Exception("User not found");
    _db.Users.Remove(u);
    await _db.SaveChangesAsync();
  }

  public async Task<UserDetail> UpdateStatusAsync(Guid id, UpdateStatusRequest request)
  {
    var u = await _db.Users.FirstOrDefaultAsync(x => x.Id == id)
      ?? throw new Exception("User not found");
    u.status = Enum.Parse<UserStatus>(request.Status, true);
    u.LastUpdatedAt = DateTime.UtcNow;
    await _db.SaveChangesAsync();
    return await GetByIdAsync(u.Id);
  }
}


