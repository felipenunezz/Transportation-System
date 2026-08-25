namespace Simulator.Simulation;

public class SimulationSettings
{
    // Orchestrator loop cadence — also what GpsDevice uses to convert
    // Speedometer's km/h into meters advanced per tick.
    public double PublishInterval { get; set; } = 3;
    public double ArrivalThresholdMeters { get; set; } = 15;

    // Speedometer's triangular target distribution + drift smoothing.
    public double MinSpeed { get; set; } = 50;
    public double MediaSpeed { get; set; } = 65;
    public double MaxSpeed { get; set; } = 80;
    public double SmoothingFactor { get; set; } = 0.15;
    public int Retarget { get; set; } = 12;
}