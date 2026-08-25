using Transportation_System.Models.Domain;
using Simulator.Devices;

namespace Simulator.Simulation;

public class StopSimulator(PassangerCounterDevice stopDevice)
{
    public async Task ProcessAsync(Stop stop, CancellationToken ct)
    {
        await stopDevice.StopTickAsync(stop, ct);
    }
}