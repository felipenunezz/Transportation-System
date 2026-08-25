using Simulator.Simulation;
using Transportation_System.Models.Domain;

namespace Simulator.Devices;

public interface  IDevice
{
    Task TickAsync(Bus bus, BusDeviceState state, CancellationToken ct);
    
}