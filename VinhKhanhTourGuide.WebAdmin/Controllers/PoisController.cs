using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using VinhKhanhTourGuide.WebAdmin.Data;
using VinhKhanhTourGuide.WebAdmin.Models;

namespace VinhKhanhTourGuide.WebAdmin.Controllers
{
    public class PoisController : Controller
    {
        private readonly TourDbContext _context;

        public PoisController(TourDbContext context)
        {
            _context = context;
        }

        // GET: Pois
        public async Task<IActionResult> Index()
        {
            return View(await _context.Poi.ToListAsync());
        }

        // GET: Pois/Details/5
        public async Task<IActionResult> Details(string id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var poi = await _context.Poi
                .FirstOrDefaultAsync(m => m.Id == id);
            if (poi == null)
            {
                return NotFound();
            }

            return View(poi);
        }

        // GET: Pois/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Pois/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,Name,ImageName,Latitude,Longitude,Description_VN,GeofenceRadius,Priority")] Poi poi)
        {
            if (ModelState.IsValid)
            {
                _context.Add(poi);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(poi);
        }

        // GET: Pois/Edit/5
        public async Task<IActionResult> Edit(string id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var poi = await _context.Poi.FindAsync(id);
            if (poi == null)
            {
                return NotFound();
            }
            return View(poi);
        }

        // POST: Pois/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(string id, [Bind("Id,Name,ImageName,Latitude,Longitude,Description_VN,GeofenceRadius,Priority")] Poi poi)
        {
            if (id != poi.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(poi);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!PoiExists(poi.Id))
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
            return View(poi);
        }

        // GET: Pois/Delete/5
        public async Task<IActionResult> Delete(string id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var poi = await _context.Poi
                .FirstOrDefaultAsync(m => m.Id == id);
            if (poi == null)
            {
                return NotFound();
            }

            return View(poi);
        }

        // POST: Pois/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(string id)
        {
            var poi = await _context.Poi.FindAsync(id);
            if (poi != null)
            {
                _context.Poi.Remove(poi);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool PoiExists(string id)
        {
            return _context.Poi.Any(e => e.Id == id);
        }
    }
}
