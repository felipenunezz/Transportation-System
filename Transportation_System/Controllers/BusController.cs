using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Transportation_System.Data;
using Transportation_System.Models.Domain;

namespace Transportation_System.Controllers;

public class BusController(BusDbContext context, ILogger<BusController> logger) : Controller
{
    private SelectList HubSelectList(int? selected = null) =>
        new(context.Stops.Where(s =>s.Type == StopType.Depot), "Id", "Name", selected);

    public async Task<IActionResult> Index()
    {
        ViewData["RouteId"] = new SelectList(context.Routes, "Id", "Name");
        ViewData["StopId"] = HubSelectList();
        return View(await context.Buses.Include(b => b.Route).Include(b => b.Stop).ToListAsync());
    }

    public async Task<IActionResult> Details(int? id)
    {
        if (id == null) return NotFound();

        var bus = await context.Buses
            .Include(b => b.Route)
            .Include(b => b.Stop)
            .FirstOrDefaultAsync(m => m.Id == id);
        if (bus == null) return NotFound();

        return View("_Details", bus);
    }

    public IActionResult Create()
    {
        ViewData["RouteId"] = new SelectList(context.Routes, "Id", "Name");
        ViewData["StopId"] = HubSelectList();
        return PartialView("_Create", new Bus());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(
        [Bind("RouteId,BusNumber,Speed,PassengerCount,StopId")]
        Bus bus)
    {
        if (ModelState.IsValid)
        {
            bus.OnRoute = false;
            bus.Status = BusStatus.Parked;
            bus.LastUpdate = DateTime.UtcNow;

            if (bus.StopId is { } stopId)
            {
                var stop = await context.Stops.FindAsync(stopId);
                if (stop != null)
                {
                    bus.CurrentLatitude = stop.Latitude;
                    bus.CurrentLongitude = stop.Longitude;
                }
            }

            context.Add(bus);

            try
            {
                await context.SaveChangesAsync();

                if (bus.RouteId is not { } routeId) return RedirectToAction(nameof(Index));
                var route = await context.Routes.FindAsync(routeId);
                if (route == null) return RedirectToAction(nameof(Index));
                var queue = new StopQueue(route.RouteStops);
                bus.StopQueue = queue.ToList();
                await context.SaveChangesAsync();

                return RedirectToAction(nameof(Index));
            }
            catch (DbUpdateException ex)
            {
                logger.LogError(ex, "Failed to save Bus {@Bus}", bus);
                ModelState.AddModelError(nameof(bus.RouteId),
                    "Selected route no longer exists. Please choose a valid route.");
            }
        }

        ViewData["RouteId"] = new SelectList(context.Routes, "Id", "Name", bus.RouteId);
        ViewData["StopId"] = HubSelectList(bus.StopId);
        return PartialView("_Create", bus);
    }

    public async Task<IActionResult> Edit(int? id)
    {
        if (id == null) return NotFound();

        var bus = await context.Buses.FindAsync(id);
        if (bus == null) return NotFound();
        ViewData["RouteId"] = new SelectList(context.Routes, "Id", "Name", bus.RouteId);
        ViewData["StopId"] = HubSelectList(bus.StopId);
        return View("_Edit", bus);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id,
        [Bind("Id,RouteId,CurrentLatitude,CurrentLongitude,BusNumber,Speed,PassengerCount,Status,StopId, OnRoute")]
        Bus bus)
    {
        if (id != bus.Id) return NotFound();

        var existing = await context.Buses.AsNoTracking().FirstOrDefaultAsync(b => b.Id == id);
        if (existing == null) return NotFound();

        if (ModelState.IsValid)
        {
            if (bus.StopId is { } stopId && bus.StopId != existing.StopId)
            {
                var stop = await context.Stops.FindAsync(stopId);
                if (stop != null)
                {
                    bus.CurrentLatitude = stop.Latitude;
                    bus.CurrentLongitude = stop.Longitude;
                }
            }

            // Route changed (assigned, cleared, or swapped) -> reset the queue.
            if (bus.RouteId != existing.RouteId)
            {
                var queue = new StopQueue();
                if (bus.RouteId is { } newRouteId)
                {
                    var route = await context.Routes.FindAsync(newRouteId);
                    if (route != null) queue.ReplaceAll(route.RouteStops);
                }

                bus.StopQueue = queue.ToList();
            }
            else bus.StopQueue = existing.StopQueue;

            try
            {
                bus.OnRoute = existing.OnRoute;
                
                if (!bus.OnRoute) bus.Status = BusStatus.Returning;
                
                bus.LastUpdate = DateTime.UtcNow;
                context.Update(bus);
                await context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!BusExists(bus.Id)) return NotFound();
                else throw;
            }

            return RedirectToAction(nameof(Index));
        }

        ViewData["RouteId"] = new SelectList(context.Routes, "Id", "Name", bus.RouteId);
        ViewData["StopId"] = HubSelectList(bus.StopId);
        return View("_Edit", bus);
    }

    public async Task<IActionResult> Delete(int? id)
    {
        if (id == null) return NotFound();

        var bus = await context.Buses
            .Include(b => b.Route)
            .FirstOrDefaultAsync(m => m.Id == id);
        if (bus == null) return NotFound();

        return View("_Delete", bus);
    }

    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var bus = await context.Buses.FindAsync(id);
        if (bus != null) context.Buses.Remove(bus);

        await context.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    private bool BusExists(int id)
    {
        return context.Buses.Any(e => e.Id == id);
    }
}