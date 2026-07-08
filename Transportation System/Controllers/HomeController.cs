using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Transportation_System.DataBase;
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
            .Where(b => b.Status != BusStatus.OutOfService)
            .ToListAsync();
                
        var busStops = await _context.BusStops.ToListAsync();
        var routes = await _context.BusRoutes.Include(r => r.Stops).ToListAsync();
            
        ViewBag.ActiveBuses = activeBuses;
        ViewBag.BusStops = busStops;
        ViewBag.Routes = routes;
            
        return View();
    }

    public IActionResult Index()
    {
        return RedirectToAction("Dashboard");
    }

    // API endpoint for map data
    [HttpGet("/api/mapdata")]
    public async Task<IActionResult> GetMapData()
    {
        var data = new
        {
            buses = await _context.Buses
                .Where(b => b.Status != BusStatus.OutOfService)
                .Select(b => new
                {
                    id = b.Id,
                    busNumber = b.BusNumber,
                    routeName = b.BusRouteName,
                    currentLatitude = b.CurrentLatitude,
                    currentLongitude = b.CurrentLongitude,
                    speed = b.Speed,
                    passengerCount = b.PassengerCount,
                    status = b.Status.ToString()
                }).ToListAsync(),
            
            stops = await _context.BusStops.Select(s => new
            {
                id = s.Id,
                name = s.Name,
                latitude = s.Latitude,
                longitude = s.Longitude,
                waitingPassengers = s.WaitingPassengers
            }).ToListAsync(),
            
            routes = await _context.BusRoutes
                .Include(r => r.Stops.OrderBy(s => s.StopOrder))
                .Select(r => new
                {
                    id = r.Id,
                    name = r.Name,
                    stops = r.Stops.Select(s => new
                    {
                        latitude = s.Latitude,
                        longitude = s.Longitude,
                        stopOrder = s.StopOrder
                    })
                }).ToListAsync()
        };
        
        return Json(data);
    }
}