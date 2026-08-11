using Microsoft.EntityFrameworkCore;
using Transportation_System.Models.Domain;
namespace Transportation_System.Data;

public class DataSeed
{
    public async Task SeedAsync(BusDbContext context)
    {
        if  (await context.BusStops.AnyAsync()) return;
        
        var centralStations = new List<BusStop>
        {
            new()
            {
                Name = "South Central Station",
                Address = "TBD",
                Latitude = 0,
                Longitude = 0,
                WaitingPassengers = 0,
                BusRouteId = null,
                Type = StopType.Hub
            },
            new()
            {
                Name = "North Central Station",
                Address = "TBD",
                Latitude = 0,
                Longitude = 0,
                WaitingPassengers = 0,
                BusRouteId = null,
                Type = StopType.Hub
            }
        };
        
        context.BusStops.AddRange(centralStations);
        await context.SaveChangesAsync();
    }
}