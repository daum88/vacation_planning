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
        public DbSet<DepartmentCapacity> DepartmentCapacities { get; set; }
        public DbSet<RequestComment> RequestComments { get; set; }
        public DbSet<Organization> Organizations { get; set; }
        public DbSet<JoinRequest> JoinRequests { get; set; }
        public DbSet<PublicHoliday> PublicHolidays { get; set; }
        public DbSet<ManagerDelegation> ManagerDelegations { get; set; }
        public DbSet<RequestHistory> RequestHistories { get; set; }

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
            // AuditLog configuration (NEW security audit logging)
            modelBuilder.Entity<AuditLog>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.EventType).IsRequired();
                entity.Property(e => e.CreatedAt).HasDefaultValueSql("datetime('now')");

                entity.HasOne(e => e.User)
                    .WithMany(u => u.AuditLogs)
                    .HasForeignKey(e => e.UserId)
                    .OnDelete(DeleteBehavior.SetNull);

                entity.HasIndex(e => e.UserId);
                entity.HasIndex(e => e.EventType);
                entity.HasIndex(e => e.CreatedAt);
            });

            // JoinRequest configuration — two FK's to User, must be explicit
            modelBuilder.Entity<JoinRequest>(entity =>
            {
                entity.HasKey(e => e.Id);

                entity.HasOne(e => e.User)
                    .WithMany(u => u.JoinRequests)
                    .HasForeignKey(e => e.UserId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(e => e.ReviewedBy)
                    .WithMany()
                    .HasForeignKey(e => e.ReviewedByUserId)
                    .OnDelete(DeleteBehavior.SetNull);

                entity.HasOne(e => e.Organization)
                    .WithMany(o => o.JoinRequests)
                    .HasForeignKey(e => e.OrganizationId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasIndex(e => e.UserId);
                entity.HasIndex(e => e.Status);
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

            // DepartmentCapacity configuration
            modelBuilder.Entity<DepartmentCapacity>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Department).IsRequired();
                entity.HasIndex(e => e.Department).IsUnique();
                entity.Property(e => e.CreatedAt).HasDefaultValueSql("datetime('now')");
            });

            // RequestComment configuration
            modelBuilder.Entity<RequestComment>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Text).IsRequired();
                entity.Property(e => e.CreatedAt).HasDefaultValueSql("datetime('now')");

                entity.HasOne(e => e.VacationRequest)
                    .WithMany()
                    .HasForeignKey(e => e.VacationRequestId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(e => e.Author)
                    .WithMany()
                    .HasForeignKey(e => e.AuthorUserId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasIndex(e => e.VacationRequestId);
            });

            // Seed data
            SeedData(modelBuilder);
        }

        private void SeedData(ModelBuilder modelBuilder)
        {
            var now = DateTime.UtcNow;
            
            // BCrypt hash for "Password123" (workFactor 12)
            // NOTE: In production, users should change their passwords immediately
            var defaultPasswordHash = "$2b$12$5YSNXVcBlBqPtvGE4yzyuO4ZGku.53tpOxhhj5zvxpcUXWZqQtaou";

            // Seed Organizations
            modelBuilder.Entity<Organization>().HasData(
                new Organization
                {
                    Id = 1,
                    Name = "Näidis OÜ",
                    Description = "Näidis organisatsioon testimiseks",
                    Address = "Tallinn, Estonia",
                    ContactEmail = "info@naidisoü.ee",
                    ContactPhone = "+372 123 4567",
                    IsActive = true,
                    CreatedAt = now
                }
            );

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
                    AdvanceNoticeDays = 14,
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
                    AdvanceNoticeDays = 0,
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
                    AdvanceNoticeDays = 3,
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
                    AdvanceNoticeDays = 7,
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
                    AdvanceNoticeDays = 0,
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
                    PasswordHash = defaultPasswordHash,
                    OrganizationId = 1,
                    Department = "IT",
                    Position = "Arendaja",
                    ManagerId = 2,
                    IsActive = true,
                    IsAdmin = false,
                    IsTemporaryPassword = false,
                    IsProfileComplete = true,
                    LastPasswordChangeAt = now,
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
                    PasswordHash = defaultPasswordHash,
                    OrganizationId = 1,
                    Department = "IT",
                    Position = "Meeskonnajuht",
                    ManagerId = null,
                    IsActive = true,
                    IsAdmin = true,
                    IsTemporaryPassword = false,
                    IsProfileComplete = true,
                    LastPasswordChangeAt = now,
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
                    PasswordHash = defaultPasswordHash,
                    OrganizationId = 1,
                    Department = "HR",
                    Position = "Personalijuht",
                    ManagerId = null,
                    IsActive = true,
                    IsAdmin = true,
                    IsTemporaryPassword = false,
                    IsProfileComplete = true,
                    LastPasswordChangeAt = now,
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
                    PasswordHash = defaultPasswordHash,
                    OrganizationId = 1,
                    Department = "IT",
                    Position = "Arendaja",
                    ManagerId = 2,
                    IsActive = true,
                    IsAdmin = false,
                    IsTemporaryPassword = false,
                    IsProfileComplete = true,
                    LastPasswordChangeAt = now,
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
                    PasswordHash = defaultPasswordHash,
                    OrganizationId = 1,
                    Department = "Müük",
                    Position = "Müügijuht",
                    ManagerId = null,
                    IsActive = true,
                    IsAdmin = false,
                    IsTemporaryPassword = false,
                    IsProfileComplete = true,
                    LastPasswordChangeAt = now,
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
            // Seed DepartmentCapacities
            modelBuilder.Entity<DepartmentCapacity>().HasData(
                new DepartmentCapacity { Id = 1, Department = "IT", MaxConcurrent = 2, IsActive = true, CreatedAt = now },
                new DepartmentCapacity { Id = 2, Department = "HR", MaxConcurrent = 1, IsActive = true, CreatedAt = now },
                new DepartmentCapacity { Id = 3, Department = "Müük", MaxConcurrent = 2, IsActive = true, CreatedAt = now }
            );

            // Seed Estonian public holidays (recurring fixed-date + Easter-based computed for 2025-2027)
            modelBuilder.Entity<PublicHoliday>().HasData(
                // Fixed recurring
                new PublicHoliday { Id = 1,  Date = new DateTime(2000, 1, 1),  Name = "Uusaasta",                        IsRecurring = true, CreatedAt = now },
                new PublicHoliday { Id = 2,  Date = new DateTime(2000, 2, 24), Name = "Eesti Vabariigi aastapäev",        IsRecurring = true, CreatedAt = now },
                new PublicHoliday { Id = 3,  Date = new DateTime(2000, 5, 1),  Name = "Kevadpüha",                       IsRecurring = true, CreatedAt = now },
                new PublicHoliday { Id = 4,  Date = new DateTime(2000, 6, 23), Name = "Võidupüha",                       IsRecurring = true, CreatedAt = now },
                new PublicHoliday { Id = 5,  Date = new DateTime(2000, 6, 24), Name = "Jaanipäev",                       IsRecurring = true, CreatedAt = now },
                new PublicHoliday { Id = 6,  Date = new DateTime(2000, 8, 20), Name = "Taasiseseisvumispäev",             IsRecurring = true, CreatedAt = now },
                new PublicHoliday { Id = 7,  Date = new DateTime(2000, 12, 24), Name = "Jõululaupäev",                   IsRecurring = true, CreatedAt = now },
                new PublicHoliday { Id = 8,  Date = new DateTime(2000, 12, 25), Name = "Esimene jõulupüha",              IsRecurring = true, CreatedAt = now },
                new PublicHoliday { Id = 9,  Date = new DateTime(2000, 12, 26), Name = "Teine jõulupüha",                IsRecurring = true, CreatedAt = now },
                // Easter-based 2025
                new PublicHoliday { Id = 10, Date = new DateTime(2025, 4, 18), Name = "Suur reede",                      IsRecurring = false, Year = 2025, CreatedAt = now },
                new PublicHoliday { Id = 11, Date = new DateTime(2025, 4, 20), Name = "Ülestõusmispühade 1. püha",       IsRecurring = false, Year = 2025, CreatedAt = now },
                new PublicHoliday { Id = 12, Date = new DateTime(2025, 6, 8),  Name = "Nelipühade 1. püha",              IsRecurring = false, Year = 2025, CreatedAt = now },
                // Easter-based 2026
                new PublicHoliday { Id = 13, Date = new DateTime(2026, 4, 3),  Name = "Suur reede",                      IsRecurring = false, Year = 2026, CreatedAt = now },
                new PublicHoliday { Id = 14, Date = new DateTime(2026, 4, 5),  Name = "Ülestõusmispühade 1. püha",       IsRecurring = false, Year = 2026, CreatedAt = now },
                new PublicHoliday { Id = 15, Date = new DateTime(2026, 5, 24), Name = "Nelipühade 1. püha",              IsRecurring = false, Year = 2026, CreatedAt = now },
                // Easter-based 2027
                new PublicHoliday { Id = 16, Date = new DateTime(2027, 3, 26), Name = "Suur reede",                      IsRecurring = false, Year = 2027, CreatedAt = now },
                new PublicHoliday { Id = 17, Date = new DateTime(2027, 3, 28), Name = "Ülestõusmispühade 1. püha",       IsRecurring = false, Year = 2027, CreatedAt = now },
                new PublicHoliday { Id = 18, Date = new DateTime(2027, 5, 16), Name = "Nelipühade 1. püha",              IsRecurring = false, Year = 2027, CreatedAt = now }
            );

            modelBuilder.Entity<ManagerDelegation>()
                .HasOne(d => d.Manager)
                .WithMany()
                .HasForeignKey(d => d.ManagerId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<ManagerDelegation>()
                .HasOne(d => d.Delegate)
                .WithMany()
                .HasForeignKey(d => d.DelegateId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<RequestHistory>()
                .HasOne(h => h.VacationRequest)
                .WithMany()
                .HasForeignKey(h => h.VacationRequestId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<RequestHistory>()
                .HasOne(h => h.Actor)
                .WithMany()
                .HasForeignKey(h => h.ActorUserId)
                .OnDelete(DeleteBehavior.SetNull);
        }
    }
}
