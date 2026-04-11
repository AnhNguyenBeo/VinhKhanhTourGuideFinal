using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using VinhKhanhTourGuide.WebAdmin.Data;
using VinhKhanhTourGuide.WebAdmin.Models;

namespace VinhKhanhTourGuide.WebAdmin.Controllers
{
    public class AudioAssetsController : Controller
    {
        private readonly TourDbContext _context;

        public AudioAssetsController(TourDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            return View(await _context.AudioAssets.OrderBy(x => x.PoiId).ThenBy(x => x.LanguageCode).ToListAsync());
        }

        public IActionResult Create()
        {
            ViewBag.PoiList = new SelectList(_context.Poi.OrderBy(p => p.Name).ToList(), "Id", "Name");
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(AudioAsset model)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.PoiList = new SelectList(_context.Poi.OrderBy(p => p.Name).ToList(), "Id", "Name");
                return View(model);
            }

            _context.AudioAssets.Add(model);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Edit(int id)
        {
            var item = await _context.AudioAssets.FindAsync(id);
            if (item == null) return NotFound();

            ViewBag.PoiList = new SelectList(_context.Poi.OrderBy(p => p.Name).ToList(), "Id", "Name", item.PoiId);
            return View(item);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, AudioAsset model)
        {
            if (id != model.Id) return NotFound();

            if (!ModelState.IsValid)
            {
                ViewBag.PoiList = new SelectList(_context.Poi.OrderBy(p => p.Name).ToList(), "Id", "Name", model.PoiId);
                return View(model);
            }

            _context.Update(model);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Delete(int id)
        {
            var item = await _context.AudioAssets.FindAsync(id);
            if (item == null) return NotFound();
            return View(item);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var item = await _context.AudioAssets.FindAsync(id);
            if (item != null)
            {
                _context.AudioAssets.Remove(item);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }
    }
}