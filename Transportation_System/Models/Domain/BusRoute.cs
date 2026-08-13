using System.ComponentModel.DataAnnotations;

namespace Transportation_System.Models.Domain;

    public class BusRoute {
        public int Id { get; set; }
        
        [Required]
        [Display(Name = "Route Name")]
        [MaxLength(50)]
        public string Name { get; set; }
        
        [Display(Name = "Description")]
        [MaxLength(500)]
        public string Description { get; set; }
        
        [Display(Name = "Active")]
        public bool IsActive { get; set; }
        
        [Display(Name = "Route Stops")]
        public List<int> RouteStops { get; set; } = [];
        public ICollection<BusStop> Stops { get; set; } = new List<BusStop>();
    }
