using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Simulator.Devices;
using Transportation_System.Data;

namespace Simulator.Simulation;

public class BusSimulationWorker(
    IServiceScopeFactory scopeFactory,
    IOptions<SimulationSettings> options,
    ILogger<BusSimulationWorker> logger) : BackgroundService
{
    private readonly SimulationSettings _settings = options.Value;
    private readonly Dictionary<int, BusDeviceState> _states = new();

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            using var scope = scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<BusDbContext>();
            var busSimulator = scope.ServiceProvider.GetRequiredService<BusSimulator>();
            var stopSimulator = scope.ServiceProvider.GetRequiredService<StopSimulator>();

            var buses = await db.Buses.ToListAsync(stoppingToken);
            var stops = await db.Stops.ToListAsync(stoppingToken);

            foreach (var bus in buses)
            {
                if (!_states.TryGetValue(bus.Id, out var state))
                {
                    state = new BusDeviceState();
                    _states[bus.Id] = state;
                }

                try { await busSimulator.ProcessAsync(bus, state, stoppingToken); }
                catch (Exception ex) { logger.LogError(ex, "Simulation tick failed for bus {BusId}", bus.Id); }
            }

            foreach (var stop in stops)
            {
                try { await stopSimulator.ProcessAsync(stop, stoppingToken); }
                catch (Exception ex) { logger.LogError(ex, "Ambient passenger tick failed for stop {StopId}", stop.Id); }
            }

            foreach (var goneId in _states.Keys.Except(buses.Select(b => b.Id)).ToList())
                _states.Remove(goneId);

            try { await Task.Delay(TimeSpan.FromSeconds(_settings.PublishInterval), stoppingToken); }
            catch (TaskCanceledException) { break; }
        }
    }
}