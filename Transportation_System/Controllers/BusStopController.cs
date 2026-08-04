using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Transportation_System.DataBase;
using Transportation_System.Models.Domain;

namespace Transportation_System.Controllers
{
    public class BusStopController : Controller
    {
        private readonly BusDbContext _context;
        private readonly ILogger<BusRouteController> _logger;

        public BusStopController(BusDbContext context, ILogger<BusRouteController> logger)
        {
            _context = context;
            _logger = logger;
        }
        
        public async Task<IActionResult> Index()
        {
            ViewData["BusRouteId"] = new SelectList(_context.BusRoutes, "Id", "Name");
            return View(await _context.BusStops
                .Include(b => b.BusRoute)
                .ToListAsync());
        }
        
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var busStop = await _context.BusStops
                .Include(b => b.BusRoute)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (busStop == null)
            {
                return NotFound();
            }

            return View("_Details", busStop);
        }
        
        public IActionResult Create()
        {
            ViewData["BusRouteId"] = new SelectList(_context.BusRoutes, "Id", "Name");
            return PartialView("_Create", new BusStop());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Name,Latitude,Longitude,WaitingPassengers,Address,BusRouteId,StopOrder")] BusStop busStop)
        {
            if (ModelState.IsValid)
            {
                _context.Add(busStop);

                try
                {
                    await _context.SaveChangesAsync();
                    return RedirectToAction(nameof(Index));
                }
                catch (DbUpdateException ex)
                {
                    _logger.LogError(ex, "Failed to save BusStop {@BusStop}", busStop);
                    ModelState.AddModelError(nameof(busStop.BusRouteId), "Selected route no longer exists. Please choose a valid route.");
                }
            }
            ViewData["BusRouteId"] = new SelectList(_context.BusRoutes, "Id", "Name", busStop.BusRouteId);
            return PartialView("_Create", busStop);
        }
        
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var busStop = await _context.BusStops.FindAsync(id);
            if (busStop == null)
            {
                return NotFound();
            }
            ViewData["BusRouteId"] = new SelectList(_context.BusRoutes, "Id", "Name", busStop.BusRouteId);
            return View("_Edit", busStop);
        }
        
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Name,Latitude,Longitude,WaitingPassengers,Address,BusRouteId,StopOrder")] BusStop busStop)
        {
            if (id != busStop.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(busStop);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!BusStopExists(busStop.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }
            ViewData["BusRouteId"] = new SelectList(_context.BusRoutes, "Id", "Name", busStop.BusRouteId);
            return View("_Edit", busStop);
        }
        
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var busStop = await _context.BusStops
                .Include(b => b.BusRoute)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (busStop == null)
            {
                return NotFound();
            }

            return View("_Delete", busStop);
        }
        
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var busStop = await _context.BusStops.FindAsync(id);
            if (busStop != null)
            {
                _context.BusStops.Remove(busStop);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool BusStopExists(int id)
        {
            return _context.BusStops.Any(e => e.Id == id);
        }
    }
}
