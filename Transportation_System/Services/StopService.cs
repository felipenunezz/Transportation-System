using Microsoft.EntityFrameworkCore;
using Transportation_System.Data;
using Transportation_System.Models.Domain;
using Transportation_System.Models.Dto;

namespace Transportation_System.Services;

public class StopService(BusDbContext dbContext)
{
    public async Task<IEnumerable<Stop>> GetStopsAsync()
    {
        return await dbContext.Stops.AsNoTracking().ToListAsync();
    }

    public async Task<Stop?> GetStopIdAsync(int stopId)
    {
        return await dbContext.Stops.FindAsync(stopId);
    }

    public async Task UpdateStopAsync(int stopId, StopDto occupancy)
    {
        var stop = await dbContext.Stops.FindAsync(stopId);

        if (stop == null) throw new InvalidOperationException($"Stop {stopId} does not exist");

        stop.WaitingPassengers = occupancy.WaitingPassengers;

        await dbContext.SaveChangesAsync();
    }
}