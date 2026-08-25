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
    StopSimulator stopSim,
    IOptions<SimulationSettings> options,
    ILogger<BusSimulator> logger,
    BusDbContext db
)
{
    private readonly SimulationSettings _settings = options.Value;
    private readonly Random _random = new();

    private double CurrentSpeed { get; } = 0;

    public async Task ProcessAsync(RoutingService routing, Bus bus, BusSimulationState state, CancellationToken ct)
    {
        if (await Start(bus, ct)) return;

        var queue = new StopQueue(bus.StopQueue);
        var queueHead = queue.Peek();

        int targetStopId;
        double targetLat, targetLon;
        bool headingToHub;

        if (queueHead != null)
        {
            var targetStop = await db.Stops.FindAsync([queueHead.Value], ct);
            if (targetStop == null) return;

            targetStopId = targetStop.Id;
            targetLat = targetStop.Latitude;
            targetLon = targetStop.Longitude;
            headingToHub = false;
        }
        else
        {
            var hubs = await db.Stops.Where(s => s.Type == StopType.Depot).ToListAsync(ct);
            if (hubs.Count == 0) return;

            var nearestHub = hubs
                .OrderBy(h =>
                    GeoUtils.DistanceMeters(bus.CurrentLatitude, bus.CurrentLongitude, h.Latitude, h.Longitude))
                .First();

            var distanceToHub = GeoUtils.DistanceMeters(bus.CurrentLatitude, bus.CurrentLongitude, nearestHub.Latitude,
                nearestHub.Longitude);
            if (distanceToHub <= _settings.ArrivalThreshold)
            {
                bus.OnRoute = false;
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

        var (point, newIndex, reachedEnd) =
            GeoUtils.WalkPolyline(state.CurrentLegShape, state.ShapeIndex, _settings.MaxSpeed);
        state.ShapeIndex = newIndex;

        // Arrival detection: NOT re-derived here — we rely on the real
        // server (BusService.TryAdvanceQueueAsync) having already popped
        // the queue when it processed our previous published position.
        // We only notice it shrank compared to last tick.
        var arrivedNow = !headingToHub && state.LastKnownQueueCount.HasValue &&
                         state.LastKnownQueueCount.Value > queue.Count;
        state.LastKnownQueueCount = queue.Count;

        var reportedPassengers = bus.PassengerCount;
        var reportedStatus = BusStatus.Moving;
        var onRoute = false;

        if (arrivedNow)
        {
            reportedPassengers = (int)Math.Min(_settings.MaxPassangers, bus.PassengerCount + _random.Next(1, 11));
        }

        var telemetry = new BusDto(
            CurrentLatitude: point.Lat,
            CurrentLongitude: point.Lon,
            Speed: reachedEnd ? 0 : Math.Round(20 + _random.NextDouble() * 25, 1),
            PassengerCount: reportedPassengers,
            CurrentStopId: bus.StopId,
            Status: reportedStatus,
            OnRoute: onRoute
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

    private async Task<bool> Start(Bus bus, CancellationToken ct)
    {
        if (bus is not { OnRoute: false, Status: BusStatus.Parked } ||
            db.Routes.Any(r => db.Buses.Any(b => b.OnRoute && b.RouteId == r.Id))) return false;

        var route = db.Routes.FirstOrDefault(r => r.Id == bus.RouteId);
        if (route?.RouteStops is null || route.RouteStops.Count <= 0) return false;
        
        logger.LogInformation("Bus {BusId} started", bus.Id);

        var busStart = new BusStartDto
        (
            OnRoute: true,
            Status: BusStatus.Starting,
            StopQueue: new StopQueue(route.RouteStops).ToList()
        );

        await mqttPublisher.PublishAsync($"buses/{bus.Id}/start", JsonSerializer.Serialize(busStart), ct);
        return
            true; //if the function actually starts the bus, then the following actions only operate in the next tick.
    }

    private async Task MovingOnRoute(Bus bus, CancellationToken ct)
    {

    }

    private async Task<bool> ArriveAtStop(Bus bus, StopQueue stopQueue, CancellationToken ct)
    {
        var nextStopId = stopQueue.Peek();
        if (nextStopId == null)
        {
            logger.LogInformation("StopId: {StopId} is null", nextStopId);
            return false;
        }

        var nextStop = await db.Stops.FindAsync([nextStopId.Value], ct);
        if (nextStop == null)
        {
            logger.LogInformation("Stop with id: {StopId} is null", nextStopId);
            return false;
        }

        var distance = GeoUtils.DistanceMeters(bus.CurrentLatitude, bus.CurrentLongitude, nextStop.Latitude,
            nextStop.Longitude);
        if (!(distance > 15)) return false; // the distance is bigger than 15 meters.
        logger.LogInformation("Bus {BusId} parked at Stop {StopId}", bus.Id, nextStopId);

        stopQueue.Dequeue(); //removes next stop since the bus is already located in the stop.

        var movement = new BusMovementDto(
            Status: BusStatus.AtStop,
            CurrentLatitude: nextStop.Latitude,
            CurrentLongitude: nextStop.Longitude,
            Speed: 0,
            CurrentStopId: nextStopId,
            StopQueue: stopQueue.ToList()
        );

        await mqttPublisher.PublishAsync($"buses/{bus.Id}/movement", JsonSerializer.Serialize(movement), ct);
        return true; // the bus successfully arrived at the stop and now passengers are boarding. 

    }

    private async Task<bool> AtStop(Bus bus, Stop stop, CancellationToken ct)
    {
        //checks if there is any passengers at the stop, if not immediately skips this tick.
        if (stop.WaitingPassengers == 0) return false;

        var boardingPassengers = _random.Next(0, bus.PassengerCount);
        var currentPassengersCount = bus.PassengerCount + boardingPassengers;

        if (boardingPassengers > 0) await stopSim.LeavingPassangers(stop, boardingPassengers, ct);

        var passenger = new BusPassengerDto(currentPassengersCount);

        await mqttPublisher.PublishAsync($"buses/{bus.Id}/passengers", JsonSerializer.Serialize(passenger), ct);
        return true;
    }
}