using Microsoft.AspNetCore.SignalR;
using Transportation_System.Models.Domain;
using Transportation_System.Models.Dto;
using Transportation_System.Services;

namespace Transportation_System.Hubs;

public class BusTrackingHub(BusService busService, TelemetryProcessor telemetryProcessor)
    : Hub {
    public async Task SendAsync(string message, Bus bus) {
         await Clients.All.SendAsync("ReceiveMessage", message, bus);
    }

    public async Task RequestBusUpdate(int busId, BusTelemetryDto busDto) {
        await telemetryProcessor.ProcessBusAsync(busId, busDto);
    }

    public async Task<Bus> GetBusAsync(int busId) {
        var bus = await busService.GetBusIdAsync(busId);
        return bus ?? throw new ArgumentException("Bus not found");
    }
}