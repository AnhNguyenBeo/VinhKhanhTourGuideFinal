using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using VinhKhanhTourGuide.WebAdmin.Data;

namespace VinhKhanhTourGuide.WebAdmin.Controllers
{
    public class RoutesController : Controller
    {
        private readonly TourDbContext _context;

        public RoutesController(TourDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index(string? sessionId)
        {
            var query = _context.RoutePoints.AsQueryable();

            if (!string.IsNullOrWhiteSpace(sessionId))
            {
                query = query.Where(x => x.AnonymousSessionId == sessionId);
            }

            var data = await query
                .OrderByDescending(x => x.RecordedAt)
                .Take(500)
                .ToListAsync();

            ViewBag.SessionId = sessionId;
            return View(data);
        }

        public async Task<IActionResult> Map(string? sessionId)
        {
            var query = _context.RoutePoints.AsQueryable();

            if (!string.IsNullOrWhiteSpace(sessionId))
            {
                query = query.Where(x => x.AnonymousSessionId == sessionId);
            }

            var data = await query
                .OrderBy(x => x.RecordedAt)
                .Take(1000)
                .Select(x => new
                {
                    x.AnonymousSessionId,
                    x.Latitude,
                    x.Longitude,
                    x.AccuracyMeters,
                    x.RecordedAt
                })
                .ToListAsync();

            ViewBag.SessionId = sessionId;
            ViewBag.RouteJson = JsonSerializer.Serialize(data);

            return View();
        }
        public async Task<IActionResult> MapWithPOI(string? sessionId)
        {
            // Lấy RoutePoint
            var routePoints = await _context.RoutePoints
                .AsQueryable()
                .Where(x => string.IsNullOrWhiteSpace(sessionId) || x.AnonymousSessionId == sessionId)
                .OrderBy(x => x.RecordedAt)
                .Select(x => new { x.Latitude, x.Longitude })
                .ToListAsync();

            // Lấy POI
            var pois = await _context.Poi
                .Select(p => new { p.Id, p.Name, p.Latitude, p.Longitude })
                .ToListAsync();

            ViewBag.SessionId = sessionId;
            ViewBag.RouteJson = JsonSerializer.Serialize(routePoints);
            ViewBag.PoiJson = JsonSerializer.Serialize(pois);

            return View();
        }
    }
}