using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Simulator.Mqtt;
using Transportation_System.Data;
using Transportation_System.Services;

namespace Simulator.Simulation;

public class BusSimulationWorker(
    IServiceScopeFactory scopeFactory,
    MqttPublisher mqttPublisher,
    BusSimulator busSimulator,
    IOptions<SimulationSettings> options,
    ILogger<BusSimulationWorker> logger) : BackgroundService
{
    private readonly SimulationSettings _settings = options.Value;
    private readonly Dictionary<int, BusSimulationState> _states = new();

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await mqttPublisher.ConnectAsync(stoppingToken);
        logger.LogInformation("Connected to MQTT broker.");

        while (!stoppingToken.IsCancellationRequested)
        {
            using var scope = scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<BusDbContext>();
            var routing = scope.ServiceProvider.GetRequiredService<RoutingService>();

            var buses = await db.Buses.ToListAsync(stoppingToken);

            foreach (var bus in buses)
            {
                if (!_states.TryGetValue(bus.Id, out var state))
                {
                    state = new BusSimulationState();
                    _states[bus.Id] = state;
                }

                try
                {
                    await busSimulator.ProcessAsync(db, routing, bus, state, stoppingToken);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Simulation step failed for bus {BusId}", bus.Id);
                }
            }

            foreach (var goneId in _states.Keys.Except(buses.Select(b => b.Id)).ToList())
                _states.Remove(goneId);

            try { await Task.Delay(TimeSpan.FromSeconds(_settings.PublishInterval), stoppingToken); }
            catch (TaskCanceledException) { break; }
        }
    }
}