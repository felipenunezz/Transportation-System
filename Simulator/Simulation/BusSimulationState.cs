namespace Simulator.Simulation;
public class BusSimulationState
{
    public List<(double Lat, double Lon)> CurrentLegShape { get; set; } = [];
    public int ShapeIndex { get; set; }
    public int? TargetStopId { get; set; }
    public bool HeadingToHub { get; set; }
    public int? LastKnownQueueCount { get; set; }

    public void Reset()
    {
        CurrentLegShape = [];
        ShapeIndex = 0;
        TargetStopId = null;
        HeadingToHub = false;
        LastKnownQueueCount = null;
    }
}