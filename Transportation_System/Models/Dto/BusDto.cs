using Transportation_System.Models.Domain;

namespace Transportation_System.Models.Dto;

public record BusDto(
    double CurrentLatitude,
    double CurrentLongitude,
    double Speed,
    int PassengerCount,
    int? CurrentStopId,
    BusStatus Status,
    bool OnRoute
);

public record BusStartDto(
    bool OnRoute,
    BusStatus Status,
    List<int>  StopQueue
    );

public record BusFinishDto(
    bool OnRoute,
    BusStatus Status,
    List<int> StopQueue
    );

public record BusMovementDto(
    BusStatus Status,
    double CurrentLatitude,
    double CurrentLongitude,
    double Speed,
    int? CurrentStopId,
    List<int>  StopQueue
);

public record BusPassengerDto( int  PassengerCount );