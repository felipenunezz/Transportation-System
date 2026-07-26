using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Transportation_System.Models.Domain;

    public class BusRoute
    {
        public int Id { get; set; }
        
        [Required]
        [Display(Name = "Route Name")]
        public string Name { get; set; }
        
        [Display(Name = "Description")]
        public string Description { get; set; }
        
        [Display(Name = "Active")]
        public bool IsActive { get; set; }
        
        // Navigation property
        [JsonIgnore]
        public List<BusStop> Stops { get; set; } = new List<BusStop>();
    }
