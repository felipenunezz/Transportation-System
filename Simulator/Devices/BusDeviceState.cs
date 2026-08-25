namespace Simulator.Devices;

public class BusDeviceState
{
    public double CurrentSpeed { get; set; }
    public double TargetSpeed { get; set; }
    public int TicksSinceRetarget { get; set; }

    // Gps
    public List<(double Lat, double Lon)> CurrentLegShape { get; set; } = [];
    public int ShapeIndex { get; set; }
    public int? TargetStopId { get; set; }
    public DateTime? LastGpsPublish { get; set; }

    // FleetTerminal -> PassengerCounter handoff, valid for one tick only
    public int? JustArrivedStopId { get; set; }

    public void ResetLeg()
    {
        CurrentLegShape = [];
        ShapeIndex = 0;
        TargetStopId = null;
    }
}