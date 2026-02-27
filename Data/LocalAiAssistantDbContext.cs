using LocalAIAssistant.Data.Models;
using Microsoft.EntityFrameworkCore;

namespace LocalAIAssistant.Data;

public class LocalAiAssistantDbContext : DbContext
{
    public DbSet<OfflineQueueItem> OfflineQueue => Set<OfflineQueueItem>();

    public LocalAiAssistantDbContext(DbContextOptions<LocalAiAssistantDbContext> options)
            : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<OfflineQueueItem>(entity =>
        {
            entity.HasKey(x => x.Id);

            entity.HasIndex(x => x.Status);
            entity.HasIndex(x => x.CreatedUtc);

            entity.Property(x => x.SessionId)
                  .IsRequired()
                  .HasMaxLength(200);

            entity.Property(x => x.Input)
                  .IsRequired();

            entity.Property(x => x.Model)
                  .HasMaxLength(100);
        });
    }
}
