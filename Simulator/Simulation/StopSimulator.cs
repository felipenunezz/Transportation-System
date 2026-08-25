using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Simulator.Mqtt;
using Transportation_System.Data;
using Transportation_System.Models.Domain;
using Transportation_System.Models.Dto;

namespace Simulator.Simulation;

public class StopSimulator(
    MqttPublisher mqttPublisher,
    IOptions<SimulationSettings> options,
    ILogger<BusSimulator> logger,
    BusDbContext db
)
{
    private readonly Random _random= new();
    
    public async Task ProcessAsync(Stop stop, CancellationToken ct)
    {
        await GeneratePassengers(stop, ct);
    }

    public async Task LeavingPassangers(Stop stop, int leaving, CancellationToken ct)
    {
        stop.WaitingPassengers -= leaving;
        var telemetry = new StopDto(stop.WaitingPassengers);
        
        await mqttPublisher.PublishAsync($"stops/{stop.Id}/passengers", telemetry.ToString(), ct);
    }

    private async Task GeneratePassengers(Stop stop, CancellationToken ct)
    {
        var Passengers = stop.WaitingPassengers + _random.Next(0, 3);
        
        var telemetry = new StopDto(Passengers);
        
        await mqttPublisher.PublishAsync($"stops/{stop.Id}/passengers", telemetry.ToString(), ct);
    }
}