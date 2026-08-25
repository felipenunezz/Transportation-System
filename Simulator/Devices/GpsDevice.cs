using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Simulator.Mqtt;
using Simulator.Simulation;
using Transportation_System.Data;
using Transportation_System.Models.Domain;
using Transportation_System.Models.Dto;
using Transportation_System.Services;

namespace Simulator.Devices;

public class GpsDevice (MqttPublisher mqttPublisher, RoutingService routing, BusDbContext context, SimulationSettings settings): IDevice
{
    public async Task TickAsync(Bus bus, BusDeviceState state, CancellationToken ct)
    {
        if (!bus.OnRoute)
        {
            await PublishAsync(bus, state, ct);
            return;
        }

        var queue = new StopQueue(bus.StopQueue);
        var headStopId = queue.Peek();
        
        double targetLat, targetLng;
        int? targetStopId;

        if (headStopId != null)
        {
            var stop = await context.Stops.FindAsync([headStopId.Value],ct);
            if (stop == null) { await  PublishAsync(bus, state, ct); return; }
            (targetLat, targetLng, targetStopId) = (stop.Latitude,  stop.Longitude, stop.Id);
        }
        else
        {
            var depot = await context.Stops.Where(s => s.Type == StopType.Depot).ToListAsync(ct);
            if  (depot.Count == 0) { await PublishAsync(bus, state, ct); return;}
            
            var nearestDepot = depot
                .OrderBy(d => GeoUtils.DistanceMeters(bus.CurrentLatitude, bus.CurrentLongitude,d.Latitude, d.Longitude))
                .First();

            (targetLat, targetLng, targetStopId) = (nearestDepot.Latitude, nearestDepot.Longitude, nearestDepot.Id);
        }

        if (targetStopId != state.TargetStopId || state.CurrentLegShape.Count == 0)
        {
            var shape = await routing.GetRouteShapeAsync(new List<(double, double)>
            {
                (bus.CurrentLatitude, bus.CurrentLongitude),
                (targetLat, targetLng) 
            });
            
            state.CurrentLegShape =shape ?? [(bus.CurrentLatitude, bus.CurrentLongitude), (targetLat, targetLng)];
            state.TargetStopId = targetStopId;

            var metersPerSecond = state.CurrentSpeed * 1000 / 3600;
            var metersThisTick = metersPerSecond * settings.PublishInterval;
            
            var (point, newIndex, reachedEnd) = GeoUtils.WalkPolyline(state.CurrentLegShape, state.ShapeIndex, metersThisTick);
            bus.CurrentLatitude = point.Lat;
            bus.CurrentLongitude = point.Lon;

            if (reachedEnd) state.CurrentLegShape = [];
            
            await PublishAsync(bus, state, ct);
        }
    }
    
    private async Task PublishAsync(Bus bus, BusDeviceState state, CancellationToken ct)
    {
        var now = DateTime.UtcNow;
        var elapsed = state.LastGpsPublish is { } last ? now - last : TimeSpan.Zero;
        state.LastGpsPublish = now;

        var payload = new Gps(bus.CurrentLatitude, bus.CurrentLongitude, elapsed);
        await mqttPublisher.PublishAsync($"buses/{bus.Id}/gps", JsonSerializer.Serialize(payload), ct);
    }
}