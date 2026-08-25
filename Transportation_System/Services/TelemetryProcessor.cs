using Microsoft.AspNetCore.SignalR;
using Transportation_System.Data;
using Transportation_System.Hubs;
using Transportation_System.Models.Dto;

namespace Transportation_System.Services;

public class TelemetryProcessor(
    BusService busService,
    StopService stopService,
    IHubContext<BusTrackingHub> hub,
    ILogger<TelemetryProcessor> logger,
    BusDbContext context)
{
    //start processor
    public async Task ProcessBusAsync(int busId, BusStartDto start)
    {
        //no need to verify, all the elements are properly checked beforehand in Start method in BusSimulator.cs
        try
        {
            await busService.UpdateBusAsync(busId, start);
        }
        catch (Exception e)
        {
            logger.LogError(e, "Error saving telemetry for bus {BusId}", busId);
            return;
        }

        await hub.Clients.All.SendAsync("BusUpdated", busId);
    }

    //movement processor
    public async Task ProcessBusAsync(int busId, BusMovementDto movement)
    {
        if (!IsValidMovement(busId, movement))
        {
            logger.LogError("Movement for bus {BusId} is not valid: {@Movement}", busId, movement);
            return;
        }

        try
        {
            await busService.UpdateBusAsync(busId, movement);
        }
        catch (Exception e)
        {
            logger.LogError(e, "Error saving movement for bus {BusId}", busId);
            return;
        }
        
        await hub.Clients.All.SendAsync("BusUpdated", busId);
    }

    //passengers processor
    public async Task ProcessBusAsync(int busId, BusPassengerDto passenger)
    {
        if (!IsValidPassenger(passenger))
        {
            logger.LogError("Movement for bus {BusId} is not valid: {@Movement}", busId, passenger);
            return;
        }

        try
        {
            await busService.UpdateBusAsync(busId, passenger);
        }
        catch (Exception e)
        {
            logger.LogError(e, "Error saving movement for bus {BusId}", busId);
            return;
        }
        
        await hub.Clients.All.SendAsync("BusUpdated", busId);
    }

    public async Task ProcessStopAsync(int stopId, StopDto occupancy)
    {
        if (!IsValidStop(occupancy))
        {
            logger.LogError("Occupancy for stop {StopId} is not valid: {@Occupancy}", stopId, occupancy);
            return;
        }

        try
        {
            await stopService.UpdateStopAsync(stopId, occupancy);
        }
        catch (Exception e)
        {
            logger.LogError(e, "Error saving occupancy for stop {StopId}", stopId);
            return;
        }

        await hub.Clients.All.SendAsync("StopUpdated", stopId);
    }

    private bool IsValidMovement(int busId, BusMovementDto m)
    {
        //kind of random, after a few calculations, this is the maximum reasonable travel distance the bus
        //could make going at 80kmph at a 45-degree angle.
        
        var maxlat = 0.000599;
        var maxlon = 0.000847;
        
        var bus = context.Buses.Find([busId]);
        if  (bus == null) return false;
        if (Math.Abs(bus.CurrentLatitude - m.CurrentLatitude) > maxlat) return false;
        if (bus.CurrentLongitude - m.CurrentLongitude < maxlon) return false;
        //kinda tricky check, shouldn't break anything, since the only works when the bus is actively on route. MAY REMOVE
        if (!m.StopQueue.Any(s => context.Stops.Any(st => st.Id == s))) return false; 
        return m.Speed is >= 0;
    }

    private static bool IsValidPassenger(BusPassengerDto p)
    {
        return p.PassengerCount >= 0;
    }

    private static bool IsValidStop(StopDto o)
    {
        return o.WaitingPassengers >= 0;
    }
}