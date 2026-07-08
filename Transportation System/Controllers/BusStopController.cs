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

        public BusStopController(BusDbContext context)
        {
            _context = context;
        }

        // GET: BusStop
        public async Task<IActionResult> Index()
        {
            var busDbContext = _context.BusStops.Include(b => b.BusRoute);
            return View(await busDbContext.ToListAsync());
        }

        // GET: BusStop/Details/5
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

            return View(busStop);
        }

        // GET: BusStop/Create
        public IActionResult Create()
        {
            ViewData["BusRouteId"] = new SelectList(_context.BusRoutes, "Id", "Name");
            return View();
        }

        // POST: BusStop/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,Name,Latitude,Longitude,WaitingPassengers,Address,BusRouteId,StopOrder")] BusStop busStop)
        {
            if (ModelState.IsValid)
            {
                _context.Add(busStop);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            ViewData["BusRouteId"] = new SelectList(_context.BusRoutes, "Id", "Name", busStop.BusRouteId);
            return View(busStop);
        }

        // GET: BusStop/Edit/5
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
            return View(busStop);
        }

        // POST: BusStop/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
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
            return View(busStop);
        }

        // GET: BusStop/Delete/5
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

            return View(busStop);
        }

        // POST: BusStop/Delete/5
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
