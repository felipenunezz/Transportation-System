using Microsoft.AspNetCore.SignalR;
using Transportation_System.Hubs;
using Transportation_System.Models.Dto;

namespace Transportation_System.Services;

public class TelemetryProcessor
{
    private readonly BusService _busService;
    private readonly StopService _StopService;
    private readonly IHubContext<BusTrackingHub> _hub;
    private readonly ILogger<TelemetryProcessor> _logger;

    public TelemetryProcessor(
        BusService busService,
        StopService StopService,
        IHubContext<BusTrackingHub> hub,
        ILogger<TelemetryProcessor> logger)
    {
        _busService = busService;
        _StopService = StopService;
        _hub = hub;
        _logger = logger;
    }

    public async Task ProcessBusAsync(int busId, BusTelemetryDto telemetry)
    {
        if (!IsValidBus(telemetry))
        {
            _logger.LogError("Telemetry for bus {BusId} is not valid: {@Telemetry}", busId, telemetry);
            return;
        }

        try
        {
            await _busService.UpdateBusAsync(busId, telemetry);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Error saving telemetry for bus {BusId}", busId);
            return;
        }

        await _hub.Clients.All.SendAsync("BusUpdated", busId);
    }

    public async Task ProcessStopAsync(int stopId, BusStopOccupancyDto occupancy)
    {
        if (!IsValidStop(occupancy))
        {
            _logger.LogError("Occupancy for stop {StopId} is not valid: {@Occupancy}", stopId, occupancy);
            return;
        }

        try
        {
            await _StopService.UpdateStopAsync(stopId, occupancy);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Error saving occupancy for stop {StopId}", stopId);
            return;
        }

        await _hub.Clients.All.SendAsync("BusStopUpdated", stopId);
    }

    private static bool IsValidBus(BusTelemetryDto t)
    {
        if (t.CurrentLatitude is < -90 or > 90) return false;
        if (t.CurrentLongitude is < -180 or > 180) return false;
        if (t.CurrentLatitude is 0 && t.CurrentLongitude is 0) return false;
        if (t.PassengerCount < 0) return false;
        return true;
    }

    private static bool IsValidStop(BusStopOccupancyDto o)
    {
        return o.WaitingPassengers >= 0;
    }
}