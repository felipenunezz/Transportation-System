using Microsoft.EntityFrameworkCore;
using Transportation_System.Data;
using Transportation_System.Models.Domain;
using Transportation_System.Models.Dto;

namespace Transportation_System.Services;

public class BusService
{
    private readonly BusDbContext _dbContext;

    public BusService(BusDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IEnumerable<Bus>> GetBusesAsync()
    {
        return await _dbContext.Buses.AsNoTracking().ToListAsync();
    }

    public async Task<Bus?> GetBusIdAsync(int busId)
    {
        return await  _dbContext.Buses.FindAsync(busId);
    }

    public async Task UpdateBusAsync(int busId, BusTelemetryDto telemetry)
    {
        var bus = await _dbContext.Buses.FindAsync(busId);

        if (bus == null)
        {
            throw new InvalidOperationException($"Bus {busId} does not exist");
        }

        bus.CurrentLatitude = telemetry.CurrentLatitude;
        bus.CurrentLongitude = telemetry.CurrentLongitude;
        bus.Speed = telemetry.Speed;
        bus.PassengerCount = telemetry.PassengerCount;
        bus.Status = telemetry.Status;
        bus.LastUpdate = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync();
    }
}