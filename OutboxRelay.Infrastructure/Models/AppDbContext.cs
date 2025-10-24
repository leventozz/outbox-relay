using Microsoft.EntityFrameworkCore;

namespace OutboxRelay.Infrastructure.Models
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {

        }

        public DbSet<Transaction> Transactions { get; set; }
        public DbSet<Outbox> Outboxes { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Transaction>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.CreatedAt).HasDefaultValueSql("GETUTCDATE()");
            });

            modelBuilder.Entity<Outbox>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.CreatedAt).HasDefaultValueSql("GETUTCDATE()");
                entity.Property(e => e.RetryCount).HasDefaultValue(0);

                entity.HasIndex(e => e.Status).HasDatabaseName("IX_Outbox_Status");
                entity.HasIndex(e => e.CreatedAt).HasDatabaseName("IX_Outbox_CreatedAt");
            });
        }
    }
}
