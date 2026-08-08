using Transportation_System.Models.Domain;
namespace Transportation_System.Models.Dto;

public record BusTelemetryDto
(
    double CurrentLatitude,
    double CurrentLongitude,
    double Speed,
    int PassengerCount,
    BusStatus Status
);