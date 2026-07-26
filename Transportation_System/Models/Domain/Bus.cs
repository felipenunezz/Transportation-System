using System.ComponentModel.DataAnnotations;

namespace Transportation_System.Models.Domain;

    public class Bus
    {
        public int Id { get; set; }
        
        [Required]
        [Display(Name = "Bus Number")]
        public string BusNumber { get; set; }
        
        [Display(Name = "Route Name")]
        public string BusRouteName { get; set; }
        
        [Display(Name = "Current Latitude")]
        public double CurrentLatitude { get; set; }
        
        [Display(Name = "Current Longitude")]
        public double CurrentLongitude { get; set; }
        
        [Display(Name = "Speed (km/h)")]
        public double Speed { get; set; }
        
        [Display(Name = "Passenger Count")]
        public int PassengerCount { get; set; }
        
        public BusStatus Status { get; set; }
        
        [Display(Name = "Last Update")]
        public DateTime LastUpdate { get; set; }
    }
    
    public enum BusStatus
    {
        OnRoute,
        AtStop,
        OutOfService,
        Delayed
    }