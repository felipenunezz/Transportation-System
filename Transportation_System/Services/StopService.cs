using Microsoft.EntityFrameworkCore;
using Transportation_System.Data;
using Transportation_System.Models.Domain;
using Transportation_System.Models.Dto;

namespace Transportation_System.Services;

public class StopService(BusDbContext dbContext)
{
    public async Task<IEnumerable<BusStop>> GetStopsAsync() { return await dbContext.BusStops.AsNoTracking().ToListAsync(); }

    public async Task<BusStop?> GetStopIdAsync(int stopId) { return await  dbContext.BusStops.FindAsync(stopId); }

    public async Task UpdateStopAsync(int stopId, BusStopOccupancyDto occupancy) {
        var stop = await dbContext.BusStops.FindAsync(stopId);

        if (stop == null) throw new InvalidOperationException($"BusStop {stopId} does not exist");

        stop.WaitingPassengers = occupancy.WaitingPassengers;

        await dbContext.SaveChangesAsync();
    }
}