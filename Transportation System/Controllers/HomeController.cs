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
}