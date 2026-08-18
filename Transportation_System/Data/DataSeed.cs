using Microsoft.EntityFrameworkCore;
using Transportation_System.Models.Domain;

namespace Transportation_System.Data;

public class DataSeed
{
    public async Task SeedAsync(BusDbContext context)
    {
        if (await context.Stops.AnyAsync()) return;

        var centralStations = new List<Stop>
        {
            new()
            {
                Name = "NPAT bus depot #2",
                Address = "Marshala Rokossovskogo Street, 2А",
                Latitude = 56.281711,
                Longitude = 44.052708,
                WaitingPassengers = 0,
                RouteId = null,
                Type = StopType.Hub
            },
            new()
            {
                Name = "NPAT bus depot #1",
                Address = "Udmurtskaya Street, 40",
                Latitude = 56.290469,
                Longitude = 43.873070,
                WaitingPassengers = 0,
                RouteId = null,
                Type = StopType.Hub
            }
        };

        context.Stops.AddRange(centralStations);
        await context.SaveChangesAsync();
    }
}