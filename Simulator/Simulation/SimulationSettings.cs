namespace Simulator.Simulation;

public class SimulationSettings
{
    public double PublishInterval {get; set;} = 3;
    public double MaxSpeed { get; set; } = 80;
    public double MaxPassangers { get; set; } = 60;
    public double ArrivalThreshold { get; set; } = 15;
}