using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Transportation_System.Models.Domain;

    public class BusStop
    {
        public int Id { get; set; }
        
        [Required]
        [Display(Name = "Stop Name")]
        public string Name { get; set; }
        
        [Display(Name = "Latitude")]
        public double Latitude { get; set; }
        
        [Display(Name = "Longitude")]
        public double Longitude { get; set; }
        
        [Display(Name = "Waiting Passengers")]
        public int WaitingPassengers { get; set; }
        
        [Display(Name = "Address")]
        public string Address { get; set; }
        
        // Foreign key
        [Display(Name = "Route")]
        public int? BusRouteId { get; set; }
        [JsonIgnore]
        public BusRoute? BusRoute { get; set; }
        
        [Display(Name = "Stop Order")]
        public int StopOrder { get; set; }
    }
