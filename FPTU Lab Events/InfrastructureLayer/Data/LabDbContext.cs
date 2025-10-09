using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DomainLayer.Entities;
using Microsoft.EntityFrameworkCore;

namespace InfrastructureLayer.Data
{
    public class LabDbContext : DbContext
    {
        public LabDbContext(DbContextOptions<LabDbContext> options) : base(options) { }

        //Khai báo các Dbset tương ứng với entity 
        public DbSet<Roles> Roles { get; set; }

        public DbSet<Users> Users { get; set; }

        public DbSet<UserSession> UserSessions { get; set; }

        //Cấu hình mô hình dữ liệu (nếu cần) bằng cách override phương thức OnModelCreating
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Users>()
                .HasIndex(u => u.Email)
                .IsUnique();

            modelBuilder.Entity<Users>()
                .HasIndex(u => u.Username)
                .IsUnique();

            modelBuilder.Entity<Users>()
                .HasMany(u => u.Roles)
                .WithMany(r => r.Users)
                .UsingEntity<Dictionary<string, object>>(
                    "tbl_users_roles",
                    j => j
                        .HasOne<Roles>()
                        .WithMany()
                        .HasForeignKey("RoleId")
                        .HasConstraintName("FK_tbl_users_roles_roles")
                        .OnDelete(DeleteBehavior.Cascade),
                    j => j
                        .HasOne<Users>()
                        .WithMany()
                        .HasForeignKey("UserId")
                        .HasConstraintName("FK_tbl_users_roles_users")
                        .OnDelete(DeleteBehavior.Cascade)
                );

            // Seed roles - use static timestamps matching migration to avoid non-deterministic model
            var roleAdminId = Guid.Parse("00000000-0000-0000-0000-000000000001");
            var roleLecturerId = Guid.Parse("00000000-0000-0000-0000-000000000002");
            var roleStudentId = Guid.Parse("00000000-0000-0000-0000-000000000003");
            var seedTime = new DateTime(2025, 9, 22, 15, 17, 20, 324, DateTimeKind.Utc);

            modelBuilder.Entity<Roles>().HasData(
                new Roles { Id = roleAdminId, name = "Admin", description = "System administrator", CreatedAt = seedTime, LastUpdatedAt = seedTime },
                new Roles { Id = roleLecturerId, name = "Lecturer", description = "Lecturer", CreatedAt = seedTime, LastUpdatedAt = seedTime },
                new Roles { Id = roleStudentId, name = "Student", description = "Student", CreatedAt = seedTime, LastUpdatedAt = seedTime }
            );
        }

    }
}
