using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using Transportation_System.Hubs;
using Transportation_System.Models.Domain;

namespace Transportation_System.Services;

public class TelemetryProcessor
{
    private BusService _BusService;
    private readonly IHubContext<BusTrackingHub> _hub;
    private readonly ILogger<TelemetryProcessor> _logger;

    public TelemetryProcessor(BusService busService, IHubContext<BusTrackingHub> hub,  ILogger<TelemetryProcessor> logger)
    {
        _BusService = busService;
        _hub = hub;
        _logger = logger;
    }

    public async Task ProcessAsync(Bus bus)
    {
        if (!IsValid(bus))
        {
            _logger.LogError("Bus {Bus} is not valid", bus);
            return;
        }

        try
        {
            await _BusService.UpdateBusAsync(bus);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Error handling MQTT Message on {Bus}", bus);
            return;
        }
        await _hub.Clients.All.SendAsync("BusUpdated", bus);
    }

    private static bool IsValid(Bus bus)
    {
        if (bus.CurrentLatitude is < -90 or > 90) return false;
        if (bus.CurrentLongitude is < -180 or > 180) return false;
        if (bus.CurrentLatitude is 0 && bus.CurrentLongitude is 0) return false;
        return true;
    }
}