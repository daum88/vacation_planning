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
                entity.Property(e => e.CreatedAt).HasDefaultValueSql("datetime('now')");
                entity.Property(e => e.UpdatedAt).HasDefaultValueSql("datetime('now')");
            });
        }
    }
}
