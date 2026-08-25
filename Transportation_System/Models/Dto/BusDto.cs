using Transportation_System.Models.Domain;

namespace Transportation_System.Models.Dto;

public record Gps (
    double CurrentLatitude,
    double CurrentLongitude,
    TimeSpan ElapsedTime
    );

public record Speedometer ( double CurrentSpeed );
public record BusPassenger ( int  PassengerCount );

public record Terminal (
    BusStatus BusStatus,
    int RouteId,
    bool OnRoute,
    List<int> StopQueue,
    int? CurrentStopId
    );