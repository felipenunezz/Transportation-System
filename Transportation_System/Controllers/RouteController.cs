using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Transportation_System.Data;
using Transportation_System.Models.Domain;
using Transportation_System.Services;
using Route = Transportation_System.Models.Domain.Route;


namespace Transportation_System.Controllers;

public class RouteController(BusDbContext context, BusService busService, ILogger<RouteController> logger) : Controller
{
    public async Task<IActionResult> Index()
    {
        return View(await context.Routes.ToListAsync());
    }

    public async Task<IActionResult> Details(int? id)
    {
        if (id == null) return NotFound();

        var route = await context.Routes
            .FirstOrDefaultAsync(m => m.Id == id);
        if (route == null) return NotFound();

        await PopulateOrderedStopsViewBagAsync(route);
        return View("_Details", route);
    }

    public IActionResult Create()
    {
        return PartialView("_Create", new Route());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([Bind("Name,Description,IsActive")] Route route)
    {
        if (!ModelState.IsValid) return PartialView("_Create", route);

        route.RouteStops = [];
        context.Add(route);
        await context.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Edit(int? id)
    {
        if (id == null) return NotFound();

        var route = await context.Routes.FindAsync(id);
        if (route == null) return NotFound();

        await PopulateOrderedStopsViewBagAsync(route);
        return View("_Edit", route);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(
        int id,
        [Bind("Id,Name,Description,IsActive,RouteStops")]
        Route route,
        List<int>? deletedStopId)
    {
        if (id != route.Id) return NotFound();

        if (!ModelState.IsValid)
        {
            await PopulateOrderedStopsViewBagAsync(route);
            return View("_Edit", route);
        }

        var existingRoute = await context.Routes
            .Include(r => r.Stops)
            .FirstOrDefaultAsync(r => r.Id == id);

        if (existingRoute == null) return NotFound();

        existingRoute.Name = route.Name;
        existingRoute.Description = route.Description;
        existingRoute.IsActive = route.IsActive;
        existingRoute.RouteStops = route.RouteStops ?? [];

        if (deletedStopId != null && deletedStopId.Count != 0)
        {
            var otherRoutes = await context.Routes
                .Where(r => r.Id != id)
                .ToListAsync();

            var affectedRoutes = otherRoutes
                .Where(r => r.RouteStops.Any(deletedStopId.Contains))
                .ToList();

            foreach (var tempRoute in affectedRoutes)
            {
                tempRoute.RouteStops.RemoveAll(deletedStopId.Contains);
            }

            var stopsToDelete = await context.Stops
                .Where(s => deletedStopId.Contains(s.Id))
                .ToListAsync();

            context.Stops.RemoveRange(stopsToDelete);
        }

        try
        {
            await context.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            if (!RouteExists(route.Id)) return NotFound();
            throw;
        }

        await busService.RefreshQueueAsync(route.Id, existingRoute.RouteStops);
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Delete(int? id)
    {
        if (id == null) return NotFound();

        var route = await context.Routes
            .FirstOrDefaultAsync(m => m.Id == id);
        if (route == null) return NotFound();

        await PopulateOrderedStopsViewBagAsync(route);
        return View("_Delete", route);
    }

    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var route = await context.Routes.FindAsync(id);
        if (route == null) return RedirectToAction(nameof(Index));

        await busService.ReassignBusAsync(id);

        context.Routes.Remove(route);
        try
        {
            await context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }
        catch (DbUpdateException ex)
        {
            logger.LogError(ex, "Failed to delete Route {Id}", id);
            return Conflict(new
            {
                message =
                    "This route cannot be deleted because it still has buses or stops assigned to it. Reassign or remove them first."
            });
        }
    }

    private bool RouteExists(int id)
    {
        return context.Routes.Any(e => e.Id == id);
    }

    private async Task PopulateOrderedStopsViewBagAsync(Route route)
    {
        if (route.RouteStops?.Any() == true)
        {
            var stopsById = await context.Stops
                .Where(s => route.RouteStops.Contains(s.Id))
                .ToDictionaryAsync(s => s.Id);

            ViewBag.OrderedStops = route.RouteStops
                .Where(stopId => stopsById.ContainsKey(stopId))
                .Select(stopId => stopsById[stopId])
                .ToList();
        }
        else
        {
            ViewBag.OrderedStops = new List<Stop>();
        }
    }
}