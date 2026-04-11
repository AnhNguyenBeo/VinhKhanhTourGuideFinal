using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using VinhKhanhTourGuide.WebAdmin.Data;
using VinhKhanhTourGuide.WebAdmin.Models;

namespace VinhKhanhTourGuide.WebAdmin.Controllers
{
    public class TranslationsController : Controller
    {
        private readonly TourDbContext _context;

        public TranslationsController(TourDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index(string? poiId, string? languageCode)
        {
            var query = _context.TranslationEntries.AsQueryable();

            if (!string.IsNullOrWhiteSpace(poiId))
                query = query.Where(x => x.PoiId == poiId);

            if (!string.IsNullOrWhiteSpace(languageCode))
                query = query.Where(x => x.LanguageCode == languageCode);

            ViewBag.PoiList = _context.Poi.OrderBy(p => p.Name).ToList();

            return View(await query
                .OrderBy(x => x.PoiId)
                .ThenBy(x => x.LanguageCode)
                .ToListAsync());
        }

        public IActionResult Create()
        {
            ViewBag.PoiList = new SelectList(_context.Poi.OrderBy(p => p.Name).ToList(), "Id", "Name");
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(TranslationEntry model)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.PoiList = new SelectList(_context.Poi.OrderBy(p => p.Name).ToList(), "Id", "Name");
                return View(model);
            }

            _context.TranslationEntries.Add(model);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Edit(int id)
        {
            var item = await _context.TranslationEntries.FindAsync(id);
            if (item == null) return NotFound();

            ViewBag.PoiList = new SelectList(_context.Poi.OrderBy(p => p.Name).ToList(), "Id", "Name", item.PoiId);
            return View(item);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, TranslationEntry model)
        {
            if (id != model.Id) return NotFound();

            if (!ModelState.IsValid)
            {
                ViewBag.PoiList = new SelectList(_context.Poi.OrderBy(p => p.Name).ToList(), "Id", "Name", model.PoiId);
                return View(model);
            }

            model.UpdatedAt = DateTime.Now;
            _context.Update(model);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Delete(int id)
        {
            var item = await _context.TranslationEntries.FindAsync(id);
            if (item == null) return NotFound();
            return View(item);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var item = await _context.TranslationEntries.FindAsync(id);
            if (item != null)
            {
                _context.TranslationEntries.Remove(item);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }
        
    }
}