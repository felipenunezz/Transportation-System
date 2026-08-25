using Microsoft.EntityFrameworkCore;
using Transportation_System.Data;
using Transportation_System.Models.Domain;
using Transportation_System.Models.Dto;

namespace Transportation_System.Services;

public class BusService(BusDbContext dbContext)
{
    private const double ArrivalThresholdMeters = 15;

    private const int
        DefaultHub =
            1; //id of the first ever possible Hub, such that in the case of a null NearestHub in ReasignBusAsync method

    public async Task<IEnumerable<Bus>> GetBusesAsync()
    {
        return await dbContext.Buses.AsNoTracking().ToListAsync();
    }

    public async Task<Bus?> GetBusIdAsync(int busId)
    {
        return await dbContext.Buses.FindAsync(busId);
    }
    //start updates method
    public async Task UpdateBusAsync(int busId, BusStartDto start)
    {
        var bus = await dbContext.Buses.FindAsync(busId);

        if (bus == null) throw new InvalidOperationException($"Bus {busId} does not exist");
        
        bus.Status = start.Status;
        bus.StopQueue = start.StopQueue;
        bus.LastUpdate = DateTime.UtcNow;

        await dbContext.SaveChangesAsync();
    }
    
    //movement updates method
    public async Task UpdateBusAsync(int busId, BusMovementDto movement)
    {
        var bus = await dbContext.Buses.FindAsync(busId);

        if (bus == null) throw new InvalidOperationException($"Bus {busId} does not exist");

        bus.CurrentLatitude = movement.CurrentLatitude;
        bus.CurrentLongitude = movement.CurrentLongitude;
        bus.Speed = movement.Speed;
        bus.Status = movement.Status;
        bus.StopId = movement.CurrentStopId;
        bus.StopQueue = movement.StopQueue;
        bus.LastUpdate = DateTime.UtcNow;

        await dbContext.SaveChangesAsync();
    }
    
    //passengers updates method
    public async Task UpdateBusAsync(int busId, BusPassenger passenger)
    {
        var bus = await dbContext.Buses.FindAsync(busId);
        if (bus == null) throw new InvalidOperationException($"Bus {busId} does not exist");
        
        bus.PassengerCount = passenger.PassengerCount;
        
        await dbContext.SaveChangesAsync();
    }

    public async Task RefreshQueueAsync(int routeId, List<int> orderedStopIds)
    {
        var buses = await dbContext.Buses
            .Where(b => b.RouteId == routeId)
            .ToListAsync();

        if (buses.Count == 0) return;

        foreach (var bus in buses)
        {
            var queue = new StopQueue(bus.StopQueue);
            queue.ReplaceAll(orderedStopIds);
            bus.StopQueue = queue.ToList();
        }

        await dbContext.SaveChangesAsync();
    }

    public async Task ReassignBusAsync(int routeId)
    {
        var affectedBuses = await dbContext.Buses
            .Where(b => b.RouteId == routeId && b.OnRoute)
            .ToListAsync();

        if (affectedBuses.Count == 0) return;

        var hubs = await dbContext.Stops
            .Where(s => s.Type == StopType.Depot)
            .ToListAsync();

        foreach (var bus in affectedBuses)
        {
            var nearestHub = hubs
                .OrderBy(h => DistanceMeters(bus.CurrentLatitude, bus.CurrentLongitude, h.Latitude, h.Longitude))
                .FirstOrDefault();
            
            bus.StopQueue = new StopQueue().ToList();

            if (nearestHub != null) bus.StopId = nearestHub.Id;
            else bus.StopId = DefaultHub;

            bus.Status = BusStatus.Returning;
            bus.OnRoute = false;
            bus.LastUpdate = DateTime.UtcNow;
        }

        await dbContext.SaveChangesAsync();
    }

    private static double DistanceMeters(double lat1, double lon1, double lat2, double lon2)
    {
        const double earthRadiusM = 6371000;
        var dLat = (lat2 - lat1) * Math.PI / 180;
        var dLon = (lon2 - lon1) * Math.PI / 180;
        var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                Math.Cos(lat1 * Math.PI / 180) * Math.Cos(lat2 * Math.PI / 180) *
                Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
        var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
        return earthRadiusM * c;
    }
}