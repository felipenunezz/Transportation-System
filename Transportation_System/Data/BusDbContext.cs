using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Transportation_System.Models.Domain;
using Route = Transportation_System.Models.Domain.Route;

namespace Transportation_System.Data;

public class BusDbContext(DbContextOptions<BusDbContext> options) : DbContext(options)
{
    public DbSet<Bus> Buses { get; set; }
    public DbSet<Stop> Stops { get; set; }
    public DbSet<Route> Routes { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        //value comparer.
        var valueComparer = new ValueComparer<List<int>>(
            (a, b) => a!.SequenceEqual(b!),
            v => v.Aggregate(0, (h, x) => HashCode.Combine(h, x)),
            v => v.ToList());
        base.OnModelCreating(modelBuilder);

        //foreign keys.   
        modelBuilder.Entity<Stop>()
            .HasOne(s => s.Route)
            .WithMany(r => r.Stops)
            .HasForeignKey(s => s.RouteId)
            .IsRequired(false);

        modelBuilder.Entity<Bus>()
            .HasOne(b => b.Route)
            .WithMany()
            .HasForeignKey(b => b.RouteId)
            .OnDelete(DeleteBehavior.Restrict)
            .IsRequired();

        modelBuilder.Entity<Bus>()
            .HasOne(b => b.Stop)
            .WithMany()
            .HasForeignKey(b => b.StopId)
            .OnDelete(DeleteBehavior.Restrict)
            .IsRequired(false);

        //conversion string for enums.
        
        modelBuilder.Entity<Stop>()
            .Property(s => s.Type)
            .HasConversion<string>()
            .HasMaxLength(20);

        modelBuilder.Entity<Bus>()
            .Property(b => b.Status)
            .HasConversion<string>()
            .HasMaxLength(20);

        //constraints for the db, work in unison with IValidatableObject interface.
        modelBuilder.Entity<Stop>()
            .ToTable(t => t.HasCheckConstraint(
                "CK_Stop_RouteRequired", "\"Type\" = 'Depot' OR \"RouteId\" IS NOT NULL"
            ));

        //Parsing the List<Int> into a integer[] such that postgres can store the data, since List<int> is not valid.

        modelBuilder.Entity<Route>()
            .Property(r => r.RouteStops)
            .HasColumnType("integer[]")
            .Metadata.SetValueComparer(valueComparer);

        modelBuilder.Entity<Bus>()
            .Property(b => b.StopQueue)
            .HasColumnType("integer[]")
            .Metadata.SetValueComparer(valueComparer);
    }
}