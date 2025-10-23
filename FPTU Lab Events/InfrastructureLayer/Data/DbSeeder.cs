using DomainLayer.Entities;
using DomainLayer.Enum;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace InfrastructureLayer.Data;

public static class DbSeeder
{
    public static void EnsureAdminUser(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<LabDbContext>();
        var cfg = scope.ServiceProvider.GetRequiredService<IConfiguration>();

        var adminEmail = (cfg["Admin:Email"] ?? "admin@fpt.edu.vn").Trim().ToLowerInvariant();
        var adminUsername = (cfg["Admin:Username"] ?? "admin").Trim();
        var adminPassword = cfg["Admin:Password"] ?? "admin";

        var adminRole = db.Roles.FirstOrDefault(r => r.name == "Admin");
        if (adminRole == null) return;

        var user = db.Users.Include(u => u.Roles).FirstOrDefault(u => u.Email.ToLower() == adminEmail);
        if (user == null)
        {
            user = new Users
            {
                Id = Guid.NewGuid(),
                Email = adminEmail,
                Username = adminUsername,
                Fullname = "Administrator",
                Password = BCrypt.Net.BCrypt.HashPassword(adminPassword),
                MSSV = null,
                status = UserStatus.Active,
                CreatedAt = DateTime.UtcNow,
                LastUpdatedAt = DateTime.UtcNow
            };
            user.Roles.Add(adminRole);
            db.Users.Add(user);
            db.SaveChanges();
            return;
        }

        if (!user.Roles.Any(r => r.Id == adminRole.Id))
        {
            user.Roles.Add(adminRole);
            user.LastUpdatedAt = DateTime.UtcNow;
            db.SaveChanges();
        }
    }
}


