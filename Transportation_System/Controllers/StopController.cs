using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Transportation_System.Data;
using Transportation_System.Models.Domain;
using Transportation_System.Services;
using Route = Transportation_System.Models.Domain.Route;

namespace Transportation_System.Controllers
{
    public class StopController(BusDbContext context, BusService busService, ILogger<StopController> logger)
        : Controller
    {
        public async Task<IActionResult> Index()
        {
            ViewData["RouteId"] = new SelectList(context.Routes, "Id", "Name");
            return View(await context.Stops.Include(b => b.Route).ToListAsync());
        }

        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var stop = await context.Stops
                .Include(b => b.Route)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (stop == null) return NotFound();

            return View("_Details", stop);
        }

        public IActionResult Create()
        {
            ViewData["RouteId"] = new SelectList(context.Routes, "Id", "Name");
            return PartialView("_Create", new Stop());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
            [Bind("Name,Latitude,Longitude,WaitingPassengers,Address,RouteId")] Stop stop)
        {
            if (ModelState.IsValid)
            {
                context.Add(stop);

                try
                {
                    stop.Type = StopType.RouteStop;
                    await context.SaveChangesAsync();

                    var route = await context.Routes.FindAsync(stop.RouteId);
                    if (route == null)
                    {
                        logger.LogWarning("Route {RouteId} not found when linking new Stop {StopId}", stop.RouteId,
                            stop.Id);
                        ModelState.AddModelError(nameof(stop.RouteId),
                            "Selected route no longer exists. Please choose a valid route.");
                        ViewData["RouteId"] = new SelectList(context.Routes, "Id", "Name", stop.RouteId);
                        return PartialView("_Create", stop);
                    }

                    var queue = new StopQueue(route.RouteStops);
                    queue.Enqueue(stop.Id);
                    route.RouteStops = queue.ToList();
                    await context.SaveChangesAsync();

                    await busService.RefreshQueueAsync(route.Id, route.RouteStops);

                    return RedirectToAction(nameof(Index));
                }
                catch (DbUpdateException ex)
                {
                    logger.LogError(ex, "Failed to save Stop {@Stop}", stop);
                    ModelState.AddModelError(nameof(stop.RouteId),
                        "Selected route no longer exists. Please choose a valid route.");
                }
            }

            ViewData["RouteId"] = new SelectList(context.Routes, "Id", "Name", stop.RouteId);
            return PartialView("_Create", stop);
        }

        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var stop = await context.Stops.FindAsync(id);
            if (stop == null) return NotFound();
            ViewData["RouteId"] = new SelectList(context.Routes, "Id", "Name", stop.RouteId);
            return View("_Edit", stop);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id,
            [Bind("Id,Name,Latitude,Longitude,WaitingPassengers,Address,RouteId,Type")] Stop stop)
        {
            if (id != stop.Id) return NotFound();

            var existing = await context.Stops.AsNoTracking().FirstOrDefaultAsync(s => s.Id == id);
            if (existing == null) return NotFound();

            Route? oldRoute = null;
            Route? newRoute = null;

            if (ModelState.IsValid)
            {
                if (existing.RouteId != stop.RouteId)
                {
                    if (existing.RouteId is { } oldRouteId)
                    {
                        oldRoute = await context.Routes.FindAsync(oldRouteId);
                        if (oldRoute != null)
                        {
                            var oldQueue = new StopQueue(oldRoute.RouteStops);
                            oldQueue.Remove(id);
                            oldRoute.RouteStops = oldQueue.ToList();
                        }
                    }

                    if (stop.RouteId is { } newRouteId)
                    {
                        newRoute = await context.Routes.FindAsync(newRouteId);
                        if (newRoute != null)
                        {
                            var newQueue = new StopQueue(newRoute.RouteStops);
                            newQueue.Enqueue(id); // joins at the tail of its new route too
                            newRoute.RouteStops = newQueue.ToList();
                        }
                    }
                }

                try
                {
                    context.Update(stop);
                    await context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!StopExists(stop.Id)) return NotFound();
                    throw;
                }

                if (oldRoute != null) await busService.RefreshQueueAsync(oldRoute.Id, oldRoute.RouteStops);
                if (newRoute != null) await busService.RefreshQueueAsync(newRoute.Id, newRoute.RouteStops);

                return RedirectToAction(nameof(Index));
            }

            ViewData["RouteId"] = new SelectList(context.Routes, "Id", "Name", stop.RouteId);
            return View("_Edit", stop);
        }

        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var stop = await context.Stops
                .Include(b => b.Route)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (stop == null) return NotFound();

            return View("_Delete", stop);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var stop = await context.Stops.FindAsync(id);
            Route? route = null;

            if (stop != null)
            {
                route = await context.Routes.FindAsync(stop.RouteId);
                if (route != null)
                {
                    var queue = new StopQueue(route.RouteStops);
                    queue.Remove(id);
                    route.RouteStops = queue.ToList();
                }

                context.Stops.Remove(stop);
            }
            await using var transaction = await context.Database.BeginTransactionAsync();

            try
            {
                await context.SaveChangesAsync();
                if (route != null) await busService.RefreshQueueAsync(route.Id, route.RouteStops);
                await transaction.CommitAsync();
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to delete Bus {@Stop}", stop);
                return Conflict(new
                {
                    message =
                        "This Stop cannot be deleted because the Route refresh or the bus reassignment failed"
                });
            }
        }

        private bool StopExists(int id)
        {
            return context.Stops.Any(e => e.Id == id);
        }
    }
}