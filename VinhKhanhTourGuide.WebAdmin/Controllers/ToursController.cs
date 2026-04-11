using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VinhKhanhTourGuide.WebAdmin.Data;
using VinhKhanhTourGuide.WebAdmin.Models;

namespace VinhKhanhTourGuide.WebAdmin.Controllers
{
    public class ToursController : Controller
    {
        private readonly TourDbContext _context;

        public ToursController(TourDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            return View(await _context.Tours.OrderBy(t => t.Id).ToListAsync());
        }

        public async Task<IActionResult> Details(int id)
        {
            var tour = await _context.Tours.FindAsync(id);
            if (tour == null) return NotFound();
            return View(tour);
        }

        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Tour tour)
        {
            if (!ModelState.IsValid) return View(tour);

            _context.Tours.Add(tour);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Edit(int id)
        {
            var tour = await _context.Tours.FindAsync(id);
            if (tour == null) return NotFound();
            return View(tour);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Tour tour)
        {
            if (id != tour.Id) return NotFound();
            if (!ModelState.IsValid) return View(tour);

            _context.Update(tour);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Delete(int id)
        {
            var tour = await _context.Tours.FindAsync(id);
            if (tour == null) return NotFound();
            return View(tour);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var hasChildren = await _context.TourPois.AnyAsync(x => x.TourId == id);
            if (hasChildren)
            {
                TempData["Error"] = "Tour đang có POI, hãy xóa danh sách điểm dừng trước.";
                return RedirectToAction(nameof(Index));
            }

            var tour = await _context.Tours.FindAsync(id);
            if (tour != null)
            {
                _context.Tours.Remove(tour);
                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Index));
        }
    }
}