using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Simulator.Mqtt;
using Transportation_System.Data;
using Transportation_System.Models.Domain;
using Transportation_System.Models.Dto;

namespace Simulator.Devices;

public class PassangerCounterDevice (MqttPublisher mqttPublisher, BusDbContext context) : IDevice
{
    private readonly Random _random = new();

    public async Task TickAsync(Bus bus, BusDeviceState state, CancellationToken ct)
    {
        if (state.JustArrivedStopId is not { } stopId) return; // nothing happened this tick

        var stop = await context.Stops.FindAsync([stopId], ct);
        if (stop is not { WaitingPassengers: > 0 }) return;

        var boarding = _random.Next(1, stop.WaitingPassengers + 1);
        stop.WaitingPassengers -= boarding;
        bus.PassengerCount += boarding;
        
        var unboarding = _random.Next(1, bus.PassengerCount + 1);
        bus.PassengerCount -= unboarding; //pasangers leave the bus at the stop, and leave the stop as well. not really looking into multi boarding.

        await mqttPublisher.PublishAsync($"stops/{stop.Id}/occupancy",
            JsonSerializer.Serialize(new StopDto(stop.WaitingPassengers)), ct);

        await mqttPublisher.PublishAsync($"buses/{bus.Id}/passengers",
            JsonSerializer.Serialize(new BusPassenger(bus.PassengerCount)), ct);
    }
    
    //specific stop tick check.
    public async Task StopTickAsync(Stop stop, CancellationToken ct)
    {
        var arriving = _random.Next(0, 3);
        if (arriving == 0) return; // nothing to report this tick

        var telemetry = new StopDto(stop.WaitingPassengers + arriving);
        await mqttPublisher.PublishAsync($"stops/{stop.Id}/passengers", JsonSerializer.Serialize(telemetry), ct);
    }
}