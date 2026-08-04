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
    public class BusRouteController : Controller
    {
        private readonly BusDbContext _context;
        private readonly ILogger<BusRouteController> _logger;

        public BusRouteController(BusDbContext context , ILogger<BusRouteController> logger)
        {
            _logger = logger;
            _context = context;
        }
        
        public async Task<IActionResult> Index()
        {
            return View(await _context.BusRoutes.ToListAsync());
        }
        
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var busRoute = await _context.BusRoutes
                .FirstOrDefaultAsync(m => m.Id == id);
            if (busRoute == null)
            {
                return NotFound();
            }

            return View("_Details", busRoute);
        }
        
        public IActionResult Create()
        {
            return PartialView("_Create", new BusRoute());
        }
        
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Name,Description,IsActive")] BusRoute busRoute)
        {
            if (!ModelState.IsValid)
            {
                return PartialView("_Create", busRoute);
            }

            _context.Add(busRoute);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }
        
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var busRoute = await _context.BusRoutes.FindAsync(id);
            if (busRoute == null)
            {
                return NotFound();
            }
            return View("_Edit", busRoute);
        }
        
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Name,Description,IsActive")] BusRoute busRoute)
        {
            if (id != busRoute.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(busRoute);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!BusRouteExists(busRoute.Id))
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
            return View("_Edit", busRoute);
        }
        
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var busRoute = await _context.BusRoutes
                .FirstOrDefaultAsync(m => m.Id == id);
            if (busRoute == null)
            {
                return NotFound();
            }

            return View("_Delete",busRoute);
        }
        
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var busRoute = await _context.BusRoutes.FindAsync(id);
            if (busRoute != null)
            {
                _context.BusRoutes.Remove(busRoute);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool BusRouteExists(int id)
        {
            return _context.BusRoutes.Any(e => e.Id == id);
        }
    }
}
