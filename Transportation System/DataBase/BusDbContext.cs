using Microsoft.EntityFrameworkCore;
using Transportation_System.Models.Domain;

namespace Transportation_System.DataBase;

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
    }
}