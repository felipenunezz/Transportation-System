using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Transportation_System.Data;
using Transportation_System.Models.Domain;

namespace Transportation_System.Controllers;

public class HomeController(BusDbContext context) : Controller
{
    public async Task<IActionResult> Dashboard()
    {
        var activeBuses = await context.Buses
            .Include(b => b.Route)
            .Where(b => b.Status != BusStatus.OffRoute)
            .ToListAsync();

        var stops = await context.Stops.ToListAsync();
        var routes = await context.Routes.ToListAsync();

        ViewBag.ActiveBuses = activeBuses;
        ViewBag.Stops = stops;
        ViewBag.Routes = routes;

        return View();
    }

    public IActionResult Index()
    {
        return RedirectToAction("Dashboard");
    }

    [HttpGet("/api/mapdata")]
    public async Task<IActionResult> GetMapData()
    {
        var buses = await context.Buses
            .Where(b => b.Status != BusStatus.OffRoute)
            .Select(b => new
            {
                id = b.Id,
                busNumber = b.BusNumber,
                routeName = b.Route != null ? b.Route.Name : null,
                currentLatitude = b.CurrentLatitude,
                currentLongitude = b.CurrentLongitude,
                speed = b.Speed,
                passengerCount = b.PassengerCount,
                status = b.Status.ToString()
                
            }).ToListAsync();

        var stops = await context.Stops.Select(s => new
        {
            id = s.Id,
            name = s.Name,
            latitude = s.Latitude,
            longitude = s.Longitude,
            waitingPassengers = s.WaitingPassengers,
            type = s.Type.ToString()
        }).ToListAsync();

        var routesRaw = await context.Routes.ToListAsync();

        var referencedStopIds = routesRaw.SelectMany(r => r.RouteStops).Distinct().ToList();
        var stopsById = await context.Stops
            .Where(s => referencedStopIds.Contains(s.Id))
            .ToDictionaryAsync(s => s.Id);

        var routes = routesRaw.Select(r => new
        {
            id = r.Id,
            name = r.Name,
            stops = r.RouteStops
                .Where(stopId => stopsById.ContainsKey(stopId))
                .Select(stopId => new
                {
                    latitude = stopsById[stopId].Latitude,
                    longitude = stopsById[stopId].Longitude
                })
        }).ToList();

        return Json(new { buses, stops, routes });
    }
}