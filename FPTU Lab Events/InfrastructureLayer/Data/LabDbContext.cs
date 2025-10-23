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

        public DbSet<Notification> Notifications { get; set; }

        public DbSet<NotificationRead> NotificationReads { get; set; }

        public DbSet<Report> Reports { get; set; }

        public DbSet<Room> Rooms { get; set; }

        public DbSet<Equipment> Equipments { get; set; }

        public DbSet<Booking> Bookings { get; set; }

        public DbSet<Lab> Labs { get; set; }

        public DbSet<LabMember> LabMembers { get; set; }

        public DbSet<Event> Events { get; set; }

        public DbSet<RoomSlot> RoomSlots { get; set; }

        public DbSet<BookingApply> BookingApplies { get; set; }

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

            // Configure Notification relationships
            modelBuilder.Entity<Notification>()
                .HasOne(n => n.CreatedByUser)
                .WithMany()
                .HasForeignKey(n => n.CreatedBy)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<NotificationRead>()
                .HasOne(nr => nr.Notification)
                .WithMany(n => n.NotificationReads)
                .HasForeignKey(nr => nr.NotificationId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<NotificationRead>()
                .HasOne(nr => nr.User)
                .WithMany()
                .HasForeignKey(nr => nr.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            // Configure Report relationships
            modelBuilder.Entity<Report>()
                .HasOne(r => r.Reporter)
                .WithMany()
                .HasForeignKey(r => r.ReporterId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Report>()
                .HasOne(r => r.ResolvedByUser)
                .WithMany()
                .HasForeignKey(r => r.ResolvedBy)
                .OnDelete(DeleteBehavior.Restrict);

            // Configure Room relationships
            modelBuilder.Entity<Room>()
                .HasMany(r => r.Equipments)
                .WithOne(e => e.Room)
                .HasForeignKey(e => e.RoomId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<Room>()
                .HasMany(r => r.Bookings)
                .WithOne(b => b.Room)
                .HasForeignKey(b => b.RoomId)
                .OnDelete(DeleteBehavior.Cascade);

            // Configure Booking relationships
            modelBuilder.Entity<Booking>()
                .HasOne(b => b.User)
                .WithMany()
                .HasForeignKey(b => b.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Booking>()
                .HasOne(b => b.Event)
                .WithMany(e => e.Bookings)
                .HasForeignKey(b => b.EventId)
                .OnDelete(DeleteBehavior.SetNull);

            // Lab & LabMember
            modelBuilder.Entity<LabMember>()
                .HasIndex(lm => new { lm.LabId, lm.UserId })
                .IsUnique();

            modelBuilder.Entity<Lab>()
                .HasMany(l => l.Members)
                .WithOne(m => m.Lab)
                .HasForeignKey(m => m.LabId)
                .OnDelete(DeleteBehavior.Cascade);

            // Lab & Room
            modelBuilder.Entity<Lab>()
                .HasOne(l => l.Room)
                .WithMany()
                .HasForeignKey(l => l.RoomId)
                .OnDelete(DeleteBehavior.SetNull);

            // Event & User (CreatedBy)
            modelBuilder.Entity<Event>()
                .HasOne(e => e.CreatedByUser)
                .WithMany()
                .HasForeignKey(e => e.CreatedBy)
                .OnDelete(DeleteBehavior.Restrict);

            // RoomSlot
            modelBuilder.Entity<RoomSlot>()
                .HasIndex(rs => new { rs.RoomId, rs.DayOfWeek, rs.StartTime })
                .IsUnique();

            // BookingApply
            modelBuilder.Entity<BookingApply>()
                .HasOne(ba => ba.Booking)
                .WithMany()
                .HasForeignKey(ba => ba.BookingId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<BookingApply>()
                .HasOne(ba => ba.RoomSlot)
                .WithMany()
                .HasForeignKey(ba => ba.RoomSlotId)
                .OnDelete(DeleteBehavior.SetNull);

            // Configure indexes
            modelBuilder.Entity<Notification>()
                .HasIndex(n => new { n.TargetGroup, n.Status, n.StartDate, n.EndDate });

            modelBuilder.Entity<Report>()
                .HasIndex(r => new { r.Type, r.Status, r.ReportedDate });

            modelBuilder.Entity<Room>()
                .HasIndex(r => new { r.Name, r.Location, r.Status });

            modelBuilder.Entity<Equipment>()
                .HasIndex(e => e.SerialNumber)
                .IsUnique();

            modelBuilder.Entity<Equipment>()
                .HasIndex(e => new { e.Type, e.Status, e.RoomId });

            modelBuilder.Entity<Booking>()
                .HasIndex(b => new { b.RoomId, b.StartTime, b.EndTime, b.Status });
        }

    }
}
