using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Transportation_System.Data;
using Transportation_System.Models.Domain;
using Transportation_System.Models.Dto;
using Transportation_System.Services;
using Simulator.Mqtt;

namespace Simulator.Simulation;

public class BusSimulator(
    MqttPublisher mqttPublisher,
    IOptions<SimulationSettings> options,
    ILogger<BusSimulator> logger)
{
    private readonly SimulationSettings _settings = options.Value;
    private readonly Random _random = new();

    public async Task ProcessAsync(BusDbContext db, RoutingService routing, Bus bus, BusSimulationState state, CancellationToken ct)
    {
        // Idle bus with no route: pick one to run.
        if (bus.Status == BusStatus.OffRoute && bus.RouteId == null)
        {
            var activeRoutes = await db.Routes.Where(r => r.IsActive).ToListAsync(ct);
            // Filter for non-empty RouteStops client-side — Npgsql/EF Core
            // can't reliably translate .Count on an integer[] column.
            var runnable = activeRoutes.Where(r => r.RouteStops.Count > 0).ToList();
            if (runnable.Count == 0) return; // nothing available to simulate yet

            var chosen = runnable[_random.Next(runnable.Count)];
            bus.RouteId = chosen.Id;
            bus.StopQueue = new StopQueue(chosen.RouteStops).ToList();
            bus.Status = BusStatus.OnRoute;
            await db.SaveChangesAsync(ct);

            state.Reset();
            logger.LogInformation("Bus {BusId} assigned to route {RouteName}", bus.Id, chosen.Name);
            return; // start actually moving next tick
        }

        var queue = new StopQueue(bus.StopQueue);
        var queueHead = queue.Peek();

        int targetStopId;
        double targetLat, targetLon;
        bool headingToHub;

        if (queueHead != null)
        {
            var targetStop = await db.Stops.FindAsync([queueHead.Value], ct);
            if (targetStop == null) return; // stale id — let server-side cleanup handle it

            targetStopId = targetStop.Id;
            targetLat = targetStop.Latitude;
            targetLon = targetStop.Longitude;
            headingToHub = false;
        }
        else
        {
            var hubs = await db.Stops.Where(s => s.Type == StopType.Hub).ToListAsync(ct);
            if (hubs.Count == 0) return;

            var nearestHub = hubs
                .OrderBy(h => GeoUtils.DistanceMeters(bus.CurrentLatitude, bus.CurrentLongitude, h.Latitude, h.Longitude))
                .First();

            var distanceToHub = GeoUtils.DistanceMeters(bus.CurrentLatitude, bus.CurrentLongitude, nearestHub.Latitude, nearestHub.Longitude);
            if (distanceToHub <= _settings.ArrivalThreshold)
            {
                bus.Status = BusStatus.OffRoute;
                bus.RouteId = null;
                bus.StopId = nearestHub.Id;
                bus.StopQueue = [];
                await db.SaveChangesAsync(ct);

                state.Reset();
                logger.LogInformation("Bus {BusId} parked at hub {HubName} — trip complete", bus.Id, nearestHub.Name);
                return;
            }

            targetStopId = nearestHub.Id;
            targetLat = nearestHub.Latitude;
            targetLon = nearestHub.Longitude;
            headingToHub = true;
        }

        // (Re)request a road-following leg if the target changed or we've
        // fully walked the previous one.
        if (state.TargetStopId != targetStopId || state.CurrentLegShape.Count == 0)
        {
            var shape = await routing.GetRouteShapeAsync(new List<(double, double)>
            {
                (bus.CurrentLatitude, bus.CurrentLongitude),
                (targetLat, targetLon)
            });

            state.CurrentLegShape = shape ?? [(bus.CurrentLatitude, bus.CurrentLongitude), (targetLat, targetLon)];
            state.ShapeIndex = 0;
            state.TargetStopId = targetStopId;
            state.HeadingToHub = headingToHub;
        }

        var (point, newIndex, reachedEnd) = GeoUtils.WalkPolyline(state.CurrentLegShape, state.ShapeIndex, _settings.MaxSpeed);
        state.ShapeIndex = newIndex;

        // Arrival detection: NOT re-derived here — we rely on the real
        // server (BusService.TryAdvanceQueueAsync) having already popped
        // the queue when it processed our previous published position.
        // We only notice it shrank compared to last tick.
        var arrivedNow = !headingToHub && state.LastKnownQueueCount.HasValue && state.LastKnownQueueCount.Value > queue.Count;
        state.LastKnownQueueCount = queue.Count;

        var reportedPassengers = bus.PassengerCount;
        var reportedStatus = BusStatus.OnRoute;

        if (arrivedNow)
        {
            reportedPassengers = (int)Math.Min(_settings.MaxPassangers, bus.PassengerCount + _random.Next(1, 11));
            reportedStatus = BusStatus.AtStop;
        }

        var telemetry = new BusDto(
            CurrentLatitude: point.Lat,
            CurrentLongitude: point.Lon,
            Speed: reachedEnd ? 0 : Math.Round(20 + _random.NextDouble() * 25, 1),
            PassengerCount: reportedPassengers,
            Status: reportedStatus
        );

        await mqttPublisher.PublishAsync($"buses/{bus.Id}/telemetry", JsonSerializer.Serialize(telemetry), ct);

        if (reachedEnd)
        {
            // Force a fresh Valhalla leg next tick, even before the server
            // confirms arrival — avoids the bus sitting dead-stopped for a
            // full extra tick once it's visually reached the target.
            state.CurrentLegShape = [];
        }
    }
}