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
    public class BusController(BusDbContext context, ILogger<BusController> logger) : Controller
    {
        public async Task<IActionResult> Index()
        {
            ViewData["BusRouteId"] = new SelectList(context.BusRoutes, "Id", "Name");
            
            return View(await context.Buses
                .Include(b => b.BusRoute)
                .ToListAsync());
        }
        
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var bus = await context.Buses
                .Include(b => b.BusRoute) 
                .FirstOrDefaultAsync(m => m.Id == id);
            if (bus == null) return NotFound();

            return View("_Details",bus);
        }
        
        public IActionResult Create()
        {
            ViewData["BusRouteId"] = new SelectList(context.BusRoutes, "Id", "Name");
            return PartialView("_Create", new Bus());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("BusNumber,BusRouteId,Speed,PassengerCount,Status")] Bus bus)
        {
            if (ModelState.IsValid)
            {
                bus.LastUpdate = DateTime.UtcNow;
                context.Add(bus);

                try
                {
                    await context.SaveChangesAsync();
                    return RedirectToAction(nameof(Index));
                }
                catch (DbUpdateException ex)
                {
                    logger.LogError(ex, "Failed to save Bus {@Bus}", bus);
                    ModelState.AddModelError(nameof(bus.BusRouteId), "Selected route no longer exists. Please choose a valid route.");
                }
            }

            ViewData["BusRouteId"] = new SelectList(context.BusRoutes, "Id", "Name", bus.BusRouteId);
            return PartialView("_Create", bus);
        }
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();
            
            var bus = await context.Buses.FindAsync(id);
            if (bus == null) return NotFound();
            ViewData["BusRouteId"] = new SelectList(context.BusRoutes, "Id", "Name", bus.BusRouteId);
            return View("_Edit",bus);
        }
        
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,BusNumber,CurrentLongitude,Speed,PassengerCount,Status")] Bus bus)
        {
            if (id != bus.Id) return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
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
            ViewData["BusRouteId"] = new SelectList(context.BusRoutes, "Id", "Name", bus.BusRouteId);
            return View("_Edit", bus);
        }
        
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var bus = await context.Buses
                .Include(b => b.BusRoute)
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
}
