using Microsoft.AspNet.SignalR;
using Transportation_System.Hubs;
using Transportation_System.Models.Domain;

namespace Transportation_System.Services;

public class TelemetryProcessor
{
    private BusService _BusService;
    private readonly IHubContext<BusTrackingHub> _hub;

    public TelemetryProcessor(BusService busService, IHubContext<BusTrackingHub> hub)
    {
        _BusService = busService;
        _hub = hub;
    }

    public async Task ProcessAsync(Bus bus)
    {
        await _BusService.UpdateBusAsync(bus);
        await _hub.Clients.All.SendAsync("BusUpdated", bus);
    }
}