using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Transportation_System.Data;
using Transportation_System.Models.Domain;

namespace Transportation_System.Controllers
{
    public class BusStopController(BusDbContext context, ILogger<BusStopController> logger) : Controller
    {
        public async Task<IActionResult> Index()
        {
            ViewData["BusRouteId"] = new SelectList(context.BusRoutes, "Id", "Name");
            return View(await context.BusStops
                .Include(b => b.BusRoute)
                .ToListAsync());
        }
        
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var busStop = await context.BusStops
                .Include(b => b.BusRoute)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (busStop == null) return NotFound();

            return View("_Details", busStop);
        }
        
        public IActionResult Create()
        {
            ViewData["BusRouteId"] = new SelectList(context.BusRoutes, "Id", "Name");
            return PartialView("_Create", new BusStop());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Name,Latitude,Longitude,WaitingPassengers,Address,BusRouteId")] BusStop busStop)
        {
            if (ModelState.IsValid)
            {
                context.Add(busStop);

                try
                {
                    busStop.Type = StopType.RouteStop;
                    await context.SaveChangesAsync();
                    
                    var route = await context.BusRoutes.FindAsync(busStop.BusRouteId);
                    route!.RouteStops.Add(busStop.Id);
                    await context.SaveChangesAsync();

                    return RedirectToAction(nameof(Index));
                }
                catch (DbUpdateException ex)
                {
                    logger.LogError(ex, "Failed to save BusStop {@BusStop}", busStop);
                    ModelState.AddModelError(nameof(busStop.BusRouteId), "Selected route no longer exists. Please choose a valid route.");
                }
            }
            ViewData["BusRouteId"] = new SelectList(context.BusRoutes, "Id", "Name", busStop.BusRouteId);
            return PartialView("_Create", busStop);
        }
        
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var busStop = await context.BusStops.FindAsync(id);
            if (busStop == null) return NotFound();
            ViewData["BusRouteId"] = new SelectList(context.BusRoutes, "Id", "Name", busStop.BusRouteId);
            return View("_Edit", busStop);
        }
        
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Name,Latitude,Longitude,WaitingPassengers,Address,BusRouteId,Type")] BusStop busStop)
        {
            if (id != busStop.Id) return NotFound();

            if (ModelState.IsValid)
            {
                var existing = await context.BusStops.AsNoTracking().FirstOrDefaultAsync(s => s.Id == id);

                if (existing != null && existing.BusRouteId != busStop.BusRouteId)
                {
                    var oldRoute = await context.BusRoutes.FindAsync(existing.BusRouteId);
                    oldRoute?.RouteStops.Remove(id);

                    var newRoute = await context.BusRoutes.FindAsync(busStop.BusRouteId);
                    newRoute?.RouteStops.Add(id);
                }

                try
                {
                    context.Update(busStop);
                    await context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!BusStopExists(busStop.Id)) return NotFound();
                    throw;
                }
                return RedirectToAction(nameof(Index));
            }
            ViewData["BusRouteId"] = new SelectList(context.BusRoutes, "Id", "Name", busStop.BusRouteId);
            return View("_Edit", busStop);
        }
        
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var busStop = await context.BusStops
                .Include(b => b.BusRoute)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (busStop == null) return NotFound();

            return View("_Delete", busStop);
        }
        
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var busStop = await context.BusStops.FindAsync(id);
            if (busStop != null)
            {
                var route = await context.BusRoutes.FindAsync(busStop.BusRouteId);
                route?.RouteStops.Remove(id);
                context.BusStops.Remove(busStop);
            }
            await context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool BusStopExists(int id)
        {
            return context.BusStops.Any(e => e.Id == id);
        }
    }
}
