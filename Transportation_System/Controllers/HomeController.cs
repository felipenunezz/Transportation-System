using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Transportation_System.Data;
using Transportation_System.Models.Domain;
using Transportation_System.Services;

namespace Transportation_System.Controllers;

public class HomeController(BusDbContext context, RoutingService routingService) : Controller
{
    public async Task<IActionResult> Dashboard()
    {
        var activeBuses = await context.Buses
            .Include(b => b.Route)
            .Where(b => b.Status != BusStatus.OffRoute)
            .ToListAsync();

        var busStops = await context.Stops.ToListAsync();
        var routes = await context.Routes.ToListAsync();

        ViewBag.ActiveBuses = activeBuses;
        ViewBag.Stops = busStops;
        ViewBag.Routes = routes;

        return View();
    }

    public IActionResult Index() { return RedirectToAction("Dashboard"); }

    //if the routing fails, the code still works, just not with pathfinding, but with the normal straight lines that i had before
    [HttpGet("/api/mapdata")]
    public async Task<IActionResult> GetMapData()
    {
        var buses = await context.Buses
            .Where(b => b.Status != BusStatus.OffRoute)
            .Select(b => new {
                id = b.Id,
                busNumber = b.BusNumber,
                routeName = b.Route != null ? b.Route.Name : null,
                currentLatitude = b.CurrentLatitude,
                currentLongitude = b.CurrentLongitude,
                speed = b.Speed,
                passengerCount = b.PassengerCount,
                status = b.Status.ToString()
            }).ToListAsync();

        var stops = await context.Stops.Select(s => new {
            id = s.Id,
            name = s.Name,
            latitude = s.Latitude,
            longitude = s.Longitude,
            waitingPassengers = s.WaitingPassengers
        }).ToListAsync();

        var routesRaw = await context.Routes.ToListAsync();

        var referencedStopIds = routesRaw.SelectMany(r => r.RouteStops).Distinct().ToList();
        var stopsById = await context.Stops
            .Where(s => referencedStopIds.Contains(s.Id))
            .ToDictionaryAsync(s => s.Id);

        var routes = new List<object>();
        foreach (var r in routesRaw)
        {
            var orderedStops = r.RouteStops
                .Where(stopId => stopsById.ContainsKey(stopId))
                .Select(stopId => stopsById[stopId])
                .ToList();

            List<object> shapePoints;
            if (orderedStops.Count >= 2)
            {
                var roadShape = await routingService.GetRouteShapeAsync(orderedStops);
                shapePoints = (roadShape ?? orderedStops.Select(s => (s.Latitude, s.Longitude)))
                    .Select(p => (object)new { latitude = p.Item1, longitude = p.Item2 })
                    .ToList();
            }
            else
            {
                shapePoints = orderedStops
                    .Select(s => (object)new { latitude = s.Latitude, longitude = s.Longitude })
                    .ToList();
            }

            routes.Add(new { id = r.Id, name = r.Name, stops = shapePoints });
        }

        return Json(new { buses, stops, routes });
    }
}