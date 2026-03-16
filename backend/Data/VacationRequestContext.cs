using Microsoft.EntityFrameworkCore;
using VacationRequestApi.Models;

namespace VacationRequestApi.Data
{
    public class VacationRequestContext : DbContext
    {
        public VacationRequestContext(DbContextOptions<VacationRequestContext> options)
            : base(options)
        {
        }

        public DbSet<User> Users { get; set; }
        public DbSet<VacationRequest> VacationRequests { get; set; }
        public DbSet<LeaveType> LeaveTypes { get; set; }
        public DbSet<VacationRequestAttachment> VacationRequestAttachments { get; set; }
        public DbSet<AuditLog> AuditLogs { get; set; }
        public DbSet<BlackoutPeriod> BlackoutPeriods { get; set; }
        public DbSet<NotificationLog> NotificationLogs { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // User configuration
            modelBuilder.Entity<User>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Email).IsRequired();
                entity.HasIndex(e => e.Email).IsUnique();
                entity.Property(e => e.FirstName).IsRequired();
                entity.Property(e => e.LastName).IsRequired();
                entity.Property(e => e.CreatedAt).HasDefaultValueSql("datetime('now')");
                entity.Property(e => e.UpdatedAt).HasDefaultValueSql("datetime('now')");

                // Self-referencing relationship for Manager
                entity.HasOne(e => e.Manager)
                    .WithMany(e => e.DirectReports)
                    .HasForeignKey(e => e.ManagerId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // VacationRequest configuration
            modelBuilder.Entity<VacationRequest>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.UserId).IsRequired();
                entity.Property(e => e.LeaveTypeId).IsRequired();
                entity.Property(e => e.StartDate).IsRequired();
                entity.Property(e => e.EndDate).IsRequired();
                entity.Property(e => e.Comment).HasMaxLength(500);
                entity.Property(e => e.Status).IsRequired().HasDefaultValue(VacationRequestStatus.Pending);
                entity.Property(e => e.AdminComment).HasMaxLength(500);
                entity.Property(e => e.CreatedAt).HasDefaultValueSql("datetime('now')");
                entity.Property(e => e.UpdatedAt).HasDefaultValueSql("datetime('now')");
                entity.Property(e => e.RowVersion).IsRowVersion();

                // Relationships
                entity.HasOne(e => e.User)
                    .WithMany(u => u.VacationRequests)
                    .HasForeignKey(e => e.UserId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(e => e.LeaveType)
                    .WithMany(lt => lt.VacationRequests)
                    .HasForeignKey(e => e.LeaveTypeId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(e => e.ApprovedBy)
                    .WithMany()
                    .HasForeignKey(e => e.ApprovedByUserId)
                    .OnDelete(DeleteBehavior.Restrict);

                // Indexes for performance
                entity.HasIndex(e => e.UserId);
                entity.HasIndex(e => e.Status);
                entity.HasIndex(e => new { e.StartDate, e.EndDate });
            });

            // LeaveType configuration
            modelBuilder.Entity<LeaveType>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Name).IsRequired();
                entity.Property(e => e.Color).IsRequired().HasDefaultValue("#007AFF");
                entity.Property(e => e.CreatedAt).HasDefaultValueSql("datetime('now')");
                entity.Property(e => e.UpdatedAt).HasDefaultValueSql("datetime('now')");
            });

            // VacationRequestAttachment configuration
            modelBuilder.Entity<VacationRequestAttachment>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.FileName).IsRequired();
                entity.Property(e => e.ContentType).IsRequired();
                entity.Property(e => e.FilePath).IsRequired();
                entity.Property(e => e.UploadedAt).HasDefaultValueSql("datetime('now')");

                entity.HasOne(e => e.VacationRequest)
                    .WithMany(vr => vr.Attachments)
                    .HasForeignKey(e => e.VacationRequestId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // AuditLog configuration
            modelBuilder.Entity<AuditLog>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Action).IsRequired();
                entity.Property(e => e.Timestamp).HasDefaultValueSql("datetime('now')");

                entity.HasOne(e => e.VacationRequest)
                    .WithMany(vr => vr.AuditLogs)
                    .HasForeignKey(e => e.VacationRequestId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(e => e.User)
                    .WithMany()
                    .HasForeignKey(e => e.UserId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasIndex(e => e.VacationRequestId);
                entity.HasIndex(e => e.Timestamp);
            });

            // BlackoutPeriod configuration
            modelBuilder.Entity<BlackoutPeriod>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Name).IsRequired();
                entity.Property(e => e.StartDate).IsRequired();
                entity.Property(e => e.EndDate).IsRequired();
                entity.Property(e => e.CreatedAt).HasDefaultValueSql("datetime('now')");

                entity.HasOne(e => e.CreatedBy)
                    .WithMany()
                    .HasForeignKey(e => e.CreatedByUserId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // NotificationLog configuration
            modelBuilder.Entity<NotificationLog>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.ToEmail).IsRequired();
                entity.Property(e => e.SentAt).HasDefaultValueSql("datetime('now')");
                entity.HasIndex(e => e.SentAt);
            });

            // Seed data
            SeedData(modelBuilder);
        }

        private void SeedData(ModelBuilder modelBuilder)
        {
            var now = DateTime.UtcNow;

            // Seed Leave Types
            modelBuilder.Entity<LeaveType>().HasData(
                new LeaveType
                {
                    Id = 1,
                    Name = "Puhkus",
                    Description = "Tasuline puhkus",
                    Color = "#007AFF",
                    RequiresApproval = true,
                    RequiresAttachment = false,
                    MaxDaysPerYear = 28,
                    IsPaid = true,
                    IsActive = true,
                    DisplayOrder = 1,
                    CreatedAt = now,
                    UpdatedAt = now
                },
                new LeaveType
                {
                    Id = 2,
                    Name = "Haigusleht",
                    Description = "Haiguse tõttu",
                    Color = "#FF3B30",
                    RequiresApproval = false,
                    RequiresAttachment = true,
                    MaxDaysPerYear = 365,
                    IsPaid = true,
                    IsActive = true,
                    DisplayOrder = 2,
                    CreatedAt = now,
                    UpdatedAt = now
                },
                new LeaveType
                {
                    Id = 3,
                    Name = "Isiklik päev",
                    Description = "Isiklikel põhjustel",
                    Color = "#34C759",
                    RequiresApproval = true,
                    RequiresAttachment = false,
                    MaxDaysPerYear = 5,
                    IsPaid = true,
                    IsActive = true,
                    DisplayOrder = 3,
                    CreatedAt = now,
                    UpdatedAt = now
                },
                new LeaveType
                {
                    Id = 4,
                    Name = "Tasustamata puhkus",
                    Description = "Palgata vaba aeg",
                    Color = "#FF9500",
                    RequiresApproval = true,
                    RequiresAttachment = false,
                    MaxDaysPerYear = 30,
                    IsPaid = false,
                    IsActive = true,
                    DisplayOrder = 4,
                    CreatedAt = now,
                    UpdatedAt = now
                },
                new LeaveType
                {
                    Id = 5,
                    Name = "Leinaleht",
                    Description = "Pereliikmete kaotuse korral",
                    Color = "#8E8E93",
                    RequiresApproval = false,
                    RequiresAttachment = false,
                    MaxDaysPerYear = 10,
                    IsPaid = true,
                    IsActive = true,
                    DisplayOrder = 5,
                    CreatedAt = now,
                    UpdatedAt = now
                }
            );

            // Seed Users
            modelBuilder.Entity<User>().HasData(
                new User
                {
                    Id = 1,
                    FirstName = "Mari",
                    LastName = "Maasikas",
                    Email = "mari.maasikas@example.com",
                    Department = "IT",
                    Position = "Arendaja",
                    ManagerId = 2,
                    IsActive = true,
                    IsAdmin = false,
                    AnnualLeaveDays = 28,
                    UsedLeaveDays = 0,
                    CarryOverDays = 2,
                    HireDate = new DateTime(2022, 1, 15),
                    CreatedAt = now,
                    UpdatedAt = now
                },
                new User
                {
                    Id = 2,
                    FirstName = "Jüri",
                    LastName = "Juurikas",
                    Email = "juri.juurikas@example.com",
                    Department = "IT",
                    Position = "Meeskonnajuht",
                    ManagerId = null,
                    IsActive = true,
                    IsAdmin = true,
                    AnnualLeaveDays = 28,
                    UsedLeaveDays = 5,
                    CarryOverDays = 0,
                    HireDate = new DateTime(2018, 3, 1),
                    CreatedAt = now,
                    UpdatedAt = now
                },
                new User
                {
                    Id = 3,
                    FirstName = "Kati",
                    LastName = "Kask",
                    Email = "kati.kask@example.com",
                    Department = "HR",
                    Position = "Personalijuht",
                    ManagerId = null,
                    IsActive = true,
                    IsAdmin = true,
                    AnnualLeaveDays = 28,
                    UsedLeaveDays = 3,
                    CarryOverDays = 5,
                    HireDate = new DateTime(2020, 6, 1),
                    CreatedAt = now,
                    UpdatedAt = now
                },
                new User
                {
                    Id = 4,
                    FirstName = "Peeter",
                    LastName = "Pihlakas",
                    Email = "peeter.pihlakas@example.com",
                    Department = "IT",
                    Position = "Arendaja",
                    ManagerId = 2,
                    IsActive = true,
                    IsAdmin = false,
                    AnnualLeaveDays = 28,
                    UsedLeaveDays = 10,
                    CarryOverDays = 0,
                    HireDate = new DateTime(2021, 9, 1),
                    CreatedAt = now,
                    UpdatedAt = now
                },
                new User
                {
                    Id = 5,
                    FirstName = "Liisa",
                    LastName = "Lepp",
                    Email = "liisa.lepp@example.com",
                    Department = "Müük",
                    Position = "Müügijuht",
                    ManagerId = null,
                    IsActive = true,
                    IsAdmin = false,
                    AnnualLeaveDays = 28,
                    UsedLeaveDays = 7,
                    CarryOverDays = 3,
                    HireDate = new DateTime(2019, 11, 15),
                    CreatedAt = now,
                    UpdatedAt = now
                }
            );
            // Seed BlackoutPeriods (example company-wide blocked periods)
            modelBuilder.Entity<BlackoutPeriod>().HasData(
                new BlackoutPeriod
                {
                    Id = 1,
                    Name = "Aastalõpu sulgemisperiood",
                    Description = "Ettevõte on suletud aasta lõpus",
                    StartDate = new DateTime(2026, 12, 27),
                    EndDate = new DateTime(2026, 12, 31),
                    IsActive = true,
                    CreatedByUserId = 3,
                    CreatedAt = now
                },
                new BlackoutPeriod
                {
                    Id = 2,
                    Name = "Kvartaliaruande periood",
                    Description = "Kõigi töötajate kohalolu nõutud",
                    StartDate = new DateTime(2026, 3, 30),
                    EndDate = new DateTime(2026, 4, 1),
                    IsActive = true,
                    CreatedByUserId = 3,
                    CreatedAt = now
                }
            );
        }
    }
}
