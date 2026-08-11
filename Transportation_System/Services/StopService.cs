using Microsoft.EntityFrameworkCore;
using Transportation_System.Data;
using Transportation_System.Models.Domain;
using Transportation_System.Models.Dto;

namespace Transportation_System.Services;

public class StopService
{
    private readonly BusDbContext _dbContext;

    public StopService(BusDbContext dbContext)
    {
        _dbContext = dbContext;
    }
    
    public async Task<IEnumerable<BusStop>> GetStopsAsync()
    {
        return await _dbContext.BusStops.AsNoTracking().ToListAsync();
    }

    public async Task<BusStop?> GetStopIdAsync(int StopId)
    {
        return await  _dbContext.BusStops.FindAsync(StopId);
    }

    public async Task UpdateStopAsync(int stopId, BusStopOccupancyDto occupancy)
    {
        var stop = await _dbContext.BusStops.FindAsync(stopId);

        if (stop == null)
        {
            throw new InvalidOperationException($"BusStop {stopId} does not exist");
        }

        stop.WaitingPassengers = occupancy.WaitingPassengers;

        await _dbContext.SaveChangesAsync();
    }
}