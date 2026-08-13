using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Transportation_System.Data;
using Transportation_System.Models.Domain;

namespace Transportation_System.Controllers;

public class BusRouteController(BusDbContext context, ILogger<BusRouteController> logger) : Controller {

    public async Task<IActionResult> Index() { return View(await context.BusRoutes.ToListAsync()); }

    public async Task<IActionResult> Details(int? id) {
        if (id == null) return NotFound();

        var busRoute = await context.BusRoutes
            .FirstOrDefaultAsync(m => m.Id == id);
        if (busRoute == null) return NotFound();

        return View("_Details", busRoute);
    }

    public IActionResult Create() {
        return PartialView("_Create", new BusRoute());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([Bind("Name,Description,IsActive")] BusRoute busRoute) {
        if (!ModelState.IsValid) return PartialView("_Create", busRoute);

        busRoute.RouteStops = [];
        context.Add(busRoute);
        await context.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Edit(int? id) {
        if (id == null) return NotFound();

        var busRoute = await context.BusRoutes.FindAsync(id);
        if (busRoute == null) return NotFound();

        await PopulateOrderedStopsViewBagAsync(busRoute);
        return View("_Edit", busRoute);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(
        int id,
        [Bind("Id,Name,Description,IsActive,RouteStops")]
        BusRoute busRoute,
        List<int>? deletedStopId)
    {
        if (id != busRoute.Id) return NotFound();

        if (!ModelState.IsValid)
        {
            await PopulateOrderedStopsViewBagAsync(busRoute);
            return View("_Edit", busRoute);
        }

        var existingRoute = await context.BusRoutes
            .Include(r => r.Stops)
            .FirstOrDefaultAsync(r => r.Id == id);

        if (existingRoute == null) return NotFound();

        existingRoute.Name = busRoute.Name;
        existingRoute.Description = busRoute.Description;
        existingRoute.IsActive = busRoute.IsActive;
        existingRoute.RouteStops = busRoute.RouteStops ?? [];

        if (deletedStopId != null && deletedStopId.Any())
        {
            // Materialize other routes first — Npgsql/EF Core can't translate an
            // array-column-overlaps-client-list check into SQL, so filter in memory.
            var otherRoutes = await context.BusRoutes
                .Where(r => r.Id != id)
                .ToListAsync();

            var affectedRoutes = otherRoutes
                .Where(r => r.RouteStops.Any(deletedStopId.Contains))
                .ToList();

            foreach (var route in affectedRoutes)
            {
                route.RouteStops.RemoveAll(deletedStopId.Contains);
            }

            var stopsToDelete = await context.BusStops
                .Where(s => deletedStopId.Contains(s.Id))
                .ToListAsync();

            context.BusStops.RemoveRange(stopsToDelete);
        }

        try
        {
            await context.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            if (!BusRouteExists(busRoute.Id)) return NotFound();
            throw;
        }

        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Delete(int? id)
    {
        if (id == null) return NotFound();

        var busRoute = await context.BusRoutes
            .FirstOrDefaultAsync(m => m.Id == id);
        if (busRoute == null) return NotFound();

        return View("_Delete", busRoute);
    }

    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var busRoute = await context.BusRoutes.FindAsync(id);
        if (busRoute == null) return RedirectToAction(nameof(Index));

        context.BusRoutes.Remove(busRoute);
        try
        {
            await context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }
        catch (DbUpdateException ex)
        {
            logger.LogError(ex, "Failed to delete BusRoute {Id}", id);
            return Conflict(new
            {
                message =
                    "This route cannot be deleted because it still has buses or stops assigned to it. Reassign or remove them first."
            });
        }
    }

    private bool BusRouteExists(int id) { return context.BusRoutes.Any(e => e.Id == id); }

    private async Task PopulateOrderedStopsViewBagAsync(BusRoute busRoute) {
        if (busRoute.RouteStops?.Any() == true) {
            var stopsById = await context.BusStops
                .Where(s => busRoute.RouteStops.Contains(s.Id))
                .ToDictionaryAsync(s => s.Id);

            ViewBag.OrderedStops = busRoute.RouteStops
                .Where(stopId => stopsById.ContainsKey(stopId))
                .Select(stopId => stopsById[stopId])
                .ToList();
        }
        else {
            ViewBag.OrderedStops = new List<BusStop>();
        }
    }
}