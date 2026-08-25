using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Simulator.Mqtt;
using Simulator.Simulation;
using Transportation_System.Data;
using Transportation_System.Models.Domain;
using Transportation_System.Models.Dto;

namespace Simulator.Devices;

public class FleetTerminal(MqttPublisher mqttPublisher, BusDbContext context, SimulationSettings settings): IDevice
{
    public async Task TickAsync(Bus bus, BusDeviceState state, CancellationToken ct)
    {
        state.JustArrivedStopId = null;

        if (!bus.OnRoute)
        {
            await TryDispatchAsync(bus, state, ct);
            return;
        }

        var queue = new StopQueue(bus.StopQueue);
        var headStopId = queue.Peek();
        if (headStopId != null)
        {
            var stop = await context.Stops.FindAsync([headStopId.Value], ct);
            if (stop == null)
            {
                queue.Dequeue(); //stale id, dropped. 
                bus.StopQueue = queue.ToList();
                return;
            }
            
            var distance = GeoUtils.DistanceMeters(bus.CurrentLatitude , bus.CurrentLongitude, stop.Latitude, stop.Longitude);
            if (distance <= settings.ArrivalThresholdMeters)
            {
                queue.Dequeue();
                bus.StopQueue = queue.ToList();
                bus.StopId = stop.Id;
                bus.Status = BusStatus.AtStop;
                state.JustArrivedStopId = stop.Id;
                state.ResetLeg();

                await PublishAsync(bus, ct);
            }

            return;
        }

        var depot = await context.Stops.Where(s => s.Type == StopType.Depot)
            .ToListAsync(ct);
        if (depot.Count == 0) return;
        
        var nearestDepot = depot.OrderBy(d => GeoUtils.DistanceMeters(bus.CurrentLatitude, bus.CurrentLongitude, d.Latitude, d.Longitude))
            .First();

        if (nearestDepot != null)
        {
            queue.Enqueue(nearestDepot.Id);
            bus.StopQueue = queue.ToList(); 
            bus.Status = BusStatus.Returning;
            
            await PublishAsync(bus, ct);
        }

        var distanceToDepot = GeoUtils.DistanceMeters(bus.CurrentLatitude, bus.CurrentLongitude, nearestDepot.Latitude, nearestDepot.Longitude);

        if (distanceToDepot <= settings.ArrivalThresholdMeters)
        {
            bus.StopQueue = new StopQueue(bus.StopQueue).ToList(); //creates empty Stopqueue, since when on a depot, the Stopqueue is Empty.
            bus.Status = BusStatus.Parked;
            bus.StopId = nearestDepot.Id;
            bus.OnRoute = false;
            state.ResetLeg();
            
            await PublishAsync(bus, ct);
        }
    }
    private async Task TryDispatchAsync(Bus bus, BusDeviceState state, CancellationToken ct)
    {
        var activeRoutes = await context.Routes.Where(r => r.IsActive).ToListAsync(ct);
        var runnable = activeRoutes.Where(r => r.RouteStops.Count > 0).ToList();
        if (runnable.Count == 0) return;

        var takenRouteIds = await context.Buses
            .Where(b => b.Id != bus.Id && b.OnRoute)
            .Select(b => b.RouteId)
            .ToListAsync(ct);

        // Resume its own route if free if not, returns and checks again next tick.
        var chosen = runnable.FirstOrDefault(r => r.Id == bus.RouteId && !takenRouteIds.Contains(r.Id));
        if (chosen == null) return;
        
        bus.OnRoute = true;
        bus.Status = BusStatus.Starting;
        bus.StopQueue = new StopQueue(chosen.RouteStops).ToList();
        state.ResetLeg();

        await PublishAsync(bus, ct);
    }

    protected async Task PublishAsync(Bus bus, CancellationToken ct)
    {
        var payload = new Terminal(
            BusStatus: bus.Status,
            RouteId: bus.RouteId,
            OnRoute: bus.OnRoute,
            StopQueue: bus.StopQueue,
            CurrentStopId: bus.StopId
        );

        await mqttPublisher.PublishAsync($"buses/{bus.Id}/terminal", JsonSerializer.Serialize(payload), ct);
    }

}