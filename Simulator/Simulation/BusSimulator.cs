using Simulator.Devices;
using Transportation_System.Models.Domain;

namespace Simulator.Simulation;

public class BusSimulator(
    FleetTerminal terminal,
    SpeedometerDevice speedometer,
    GpsDevice gps,
    PassangerCounterDevice passengerCounter)
{
    public async Task ProcessAsync(Bus bus, BusDeviceState state, CancellationToken ct)
    {
        await terminal.TickAsync(bus, state, ct);
        await speedometer.TickAsync(bus, state, ct);
        await gps.TickAsync(bus, state, ct);
        await passengerCounter.TickAsync(bus, state, ct);
    }
}