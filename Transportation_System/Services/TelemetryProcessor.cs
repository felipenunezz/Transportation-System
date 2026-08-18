using Microsoft.AspNetCore.SignalR;
using Transportation_System.Hubs;
using Transportation_System.Models.Dto;

namespace Transportation_System.Services;

public class TelemetryProcessor(
    BusService busService,
    StopService stopService,
    IHubContext<BusTrackingHub> hub,
    ILogger<TelemetryProcessor> logger)
{
    public async Task ProcessBusAsync(int busId, BusDto telemetry)
    {
        if (!IsValidBus(telemetry))
        {
            logger.LogError("Telemetry for bus {BusId} is not valid: {@Telemetry}", busId, telemetry);
            return;
        }

        try
        {
            await busService.UpdateBusAsync(busId, telemetry);
        }
        catch (Exception e)
        {
            logger.LogError(e, "Error saving telemetry for bus {BusId}", busId);
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

    private static bool IsValidBus(BusDto t)
    {
        if (t.CurrentLatitude is < -90 or > 90) return false;
        if (t.CurrentLongitude is < -180 or > 180) return false;
        if (t.CurrentLatitude is 0 && t.CurrentLongitude is 0) return false;
        if (t.PassengerCount < 0) return false;
        return true;
    }

    private static bool IsValidStop(StopDto o)
    {
        return o.WaitingPassengers >= 0;
    }
}