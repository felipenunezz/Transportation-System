using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Transportation_System.Data;
using Transportation_System.Models.Domain;

namespace Transportation_System.Controllers;

public class HomeController : Controller
{
    private readonly BusDbContext _context;
        
    public HomeController(BusDbContext context)
    {
        _context = context;
    }
        
    public async Task<IActionResult> Dashboard()
    {
        var activeBuses = await _context.Buses
            .Include(b => b.BusRoute)
            .Where(b => b.Status != BusStatus.OutOfService)
            .ToListAsync();
                
        var busStops = await _context.BusStops.ToListAsync();
        var routes = await _context.BusRoutes.ToListAsync();
            
        ViewBag.ActiveBuses = activeBuses;
        ViewBag.BusStops = busStops;
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
        var buses = await _context.Buses
            .Where(b => b.Status != BusStatus.OutOfService)
            .Select(b => new
            {
                id = b.Id,
                busNumber = b.BusNumber,
                routeName = b.BusRoute != null ? b.BusRoute.Name : null,
                currentLatitude = b.CurrentLatitude,
                currentLongitude = b.CurrentLongitude,
                speed = b.Speed,
                passengerCount = b.PassengerCount,
                status = b.Status.ToString()
            }).ToListAsync();

        var stops = await _context.BusStops.Select(s => new
        {
            id = s.Id,
            name = s.Name,
            latitude = s.Latitude,
            longitude = s.Longitude,
            waitingPassengers = s.WaitingPassengers
        }).ToListAsync();
        
        var routesRaw = await _context.BusRoutes.ToListAsync();

        var referencedStopIds = routesRaw.SelectMany(r => r.RouteStops).Distinct().ToList();
        var stopsById = await _context.BusStops
            .Where(s => referencedStopIds.Contains(s.Id))
            .ToDictionaryAsync(s => s.Id);

        var routes = routesRaw.Select(r => new
        {
            id = r.Id,
            name = r.Name,
            stops = r.RouteStops
                .Where(stopId => stopsById.ContainsKey(stopId)) // guards against a stale id left in the queue
                .Select(stopId => new
                {
                    latitude = stopsById[stopId].Latitude,
                    longitude = stopsById[stopId].Longitude
                })
        }).ToList();

        return Json(new { buses, stops, routes });
    }
}