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

        public DbSet<VacationRequest> VacationRequests { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<VacationRequest>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.UserId).IsRequired();
                entity.Property(e => e.StartDate).IsRequired();
                entity.Property(e => e.EndDate).IsRequired();
                entity.Property(e => e.Comment).HasMaxLength(500);
                entity.Property(e => e.Status).IsRequired().HasDefaultValue(VacationRequestStatus.Pending);
                entity.Property(e => e.AdminComment).HasMaxLength(500);
                entity.Property(e => e.CreatedAt).HasDefaultValueSql("datetime('now')");
                entity.Property(e => e.UpdatedAt).HasDefaultValueSql("datetime('now')");
                
                // SQLite doesn't support rowversion, but we can use a simple integer version
                entity.Property(e => e.RowVersion)
                    .IsRowVersion()
                    .HasDefaultValue(new byte[0]);
            });
        }

        public override int SaveChanges()
        {
            UpdateTimestamps();
            return base.SaveChanges();
        }

        public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            UpdateTimestamps();
            return base.SaveChangesAsync(cancellationToken);
        }

        private void UpdateTimestamps()
        {
            var entries = ChangeTracker.Entries<VacationRequest>();
            foreach (var entry in entries)
            {
                if (entry.State == EntityState.Modified)
                {
                    entry.Entity.UpdatedAt = DateTime.UtcNow;
                }
            }
        }
    }
}
