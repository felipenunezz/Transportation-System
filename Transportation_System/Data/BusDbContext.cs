using Microsoft.EntityFrameworkCore;
using Transportation_System.Models.Domain;

namespace Transportation_System.Data;

public class BusDbContext : DbContext
{
    public BusDbContext(DbContextOptions<BusDbContext> options) : base(options){}
        
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
            .HasColumnType("integer[]");
    }
}