using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Transportation_System.Models.Domain;

namespace Transportation_System.Data;

public class BusDbContext(DbContextOptions<BusDbContext> options) : DbContext(options)
{
    public DbSet<Bus> Buses { get; set; }
    public DbSet<BusStop> BusStops { get; set; }
    public DbSet<BusRoute> BusRoutes { get; set; }
        
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
            
        modelBuilder.Entity<BusStop>()
            .HasOne(s => s.BusRoute)
            .WithMany(r => r.Stops)
            .HasForeignKey(s => s.BusRouteId);

        modelBuilder.Entity<BusStop>()
            .Property(b => b.Type)
            .HasConversion<string>()
            .HasMaxLength(20);

        modelBuilder.Entity<BusStop>()
            .ToTable(t => t.HasCheckConstraint(
                "CK_BusStop_RouteRequired", "\"Type\" = 'Hub' OR \"BusRouteId\" IS NOT NULL"
                ));

        modelBuilder.Entity<BusRoute>()
            .Property(r => r.RouteStops)
            .HasColumnType("integer[]")
            .Metadata.SetValueComparer(new ValueComparer<List<int>>(
                (a, b) => a!.SequenceEqual(b!),
                v => v.Aggregate(0, (h, x) => HashCode.Combine(h, x)),
                v => v.ToList()));
    }
}