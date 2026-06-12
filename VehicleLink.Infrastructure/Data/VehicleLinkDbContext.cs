using Microsoft.EntityFrameworkCore;
using VehicleLink.Core.Models;

namespace VehicleLink.Infrastructure.Data;

public class VehicleLinkDbContext : DbContext
{
    public VehicleLinkDbContext(DbContextOptions<VehicleLinkDbContext> options)
        : base(options) { }

    public DbSet<TelemetryLog> TelemetryLogs => Set<TelemetryLog>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<TelemetryLog>(entity =>
        {
            entity.HasKey(t => t.Id);

            entity.HasIndex(t => t.VehicleId);
            entity.HasIndex(t => t.Timestamp);
            entity.HasIndex(t => t.EventType);

            entity.Property(t => t.VehicleId)
                  .HasMaxLength(50)
                  .IsRequired();

            entity.Property(t => t.EventType)
                  .HasMaxLength(20)
                  .IsRequired();
        });
    }
}