using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Transportation_System.Models.Domain;

public class Stop : IValidatableObject
{
    public int Id { get; set; }

    [Required]
    [Display(Name = "Stop Name")]
    [MaxLength(50)]
    public string Name { get; set; }

    [Display(Name = "Latitude")] public double Latitude { get; set; }

    [Display(Name = "Longitude")] public double Longitude { get; set; }

    [Display(Name = "Waiting Passengers")] public int WaitingPassengers { get; set; }

    [Display(Name = "Address")]
    [MaxLength(50)]
    public string Address { get; set; }

    [Display(Name = "Route")] public int? RouteId { get; set; }
    [JsonIgnore] public Route? Route { get; set; }

    [Display(Name = "Stop Type")] public StopType Type { get; set; } = StopType.RouteStop;

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (Type != StopType.Hub && RouteId == null)
        {
            yield return new ValidationResult(
                "Please select a route.",
                new[] { nameof(RouteId) });
        }
    }
}

public enum StopType
{
    RouteStop,
    Hub
}