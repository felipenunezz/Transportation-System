using Microsoft.EntityFrameworkCore;
using Transportation_System.DataBase;
using Transportation_System.Models.Domain;

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

    public async Task UpdateBusAsync(Bus busUpdate)
    {
        var bus = await _dbContext.Buses.FindAsync(busUpdate.Id);

        if (bus == null)
        {
            throw new InvalidOperationException($"Bus {busUpdate.Id} does not exists");
        }
        
        bus.CurrentLatitude = busUpdate.CurrentLatitude;
        bus.CurrentLongitude = busUpdate.CurrentLongitude;
        bus.Speed = busUpdate.Speed;
        
        await _dbContext.SaveChangesAsync();
    }
}