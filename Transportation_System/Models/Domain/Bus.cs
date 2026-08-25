using System.ComponentModel.DataAnnotations;

namespace Transportation_System.Models.Domain;

public class Bus
{
    public int Id { get; set; }

    [Required]
    [Display(Name = "Bus Number")]
    [MaxLength(10)]
    public string? BusNumber { get; set; }

    [Display(Name = "Route Id")] public int RouteId { get; set; }
    public Route Route { get; set; }
    
    public bool OnRoute { get; set; }

    [Display(Name = "Current Stop")] public int? StopId { get; set; }
    public Stop? Stop { get; set; }

    [Display(Name = "Stop Queue")] public List<int> StopQueue { get; set; } = [];

    [Display(Name = "Current Latitude")] public double CurrentLatitude { get; set; }

    [Display(Name = "Current Longitude")] public double CurrentLongitude { get; set; }

    [Display(Name = "Speed (km/h)")] public double Speed { get; set; }

    [Display(Name = "Passenger Count")] public int PassengerCount { get; set; }

    public BusStatus Status { get; set; }

    [Display(Name = "Last Update")] public DateTime LastUpdate { get; set; }
}

public enum BusStatus
{
    Starting,
    AtStop,
    Moving,
    Returning,
    Parked
}