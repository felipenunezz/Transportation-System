using System.Configuration;
using System.Text.Json;
using Simulator.Mqtt;
using Transportation_System.Models.Domain;
using Transportation_System.Models.Dto;
using Simulator.Simulation;

namespace Simulator.Devices;

public class SpeedometerDevice (MqttPublisher mqttPublisher, SimulationSettings settings) : IDevice
{
    private readonly Random _random = new();
    
    public async Task TickAsync(Bus bus, BusDeviceState state, CancellationToken ct)
    {
        if (!bus.OnRoute || bus.Status == BusStatus.AtStop)
        {
            bus.Speed = 0;
            state.TargetSpeed = 0;
            await PublishAsync(bus, ct);
            return;
        }

        if (state.TicksSinceRetarget <= 0 || state.TargetSpeed <= 0)
        {
            state.TargetSpeed = SampleTriangular(settings.MinSpeed, settings.MediaSpeed, settings.MaxSpeed);
            state.TicksSinceRetarget = settings.Retarget;
        }

        state.TicksSinceRetarget--;
        state.CurrentSpeed =+ (state.TargetSpeed - state.CurrentSpeed) * settings.SmoothingFactor;
        bus.Speed = state.CurrentSpeed;
        await PublishAsync(bus, ct);
    }

    private async Task PublishAsync(Bus bus, CancellationToken ct)
    {
        var payload = new Speedometer(bus.Speed);
        await mqttPublisher.PublishAsync($"buses/{bus.Id}/speed", JsonSerializer.Serialize(payload), ct);
    }
    
    private double SampleTriangular(double min, double mode, double max)
    {
        var u = _random.NextDouble();
        var c = (mode - min) / (max - min);

        return u < c
            ? min + Math.Sqrt(u * (max - min) * (mode - min))
            : max - Math.Sqrt((1 - u) * (max - min) * (max - mode));
    }
}