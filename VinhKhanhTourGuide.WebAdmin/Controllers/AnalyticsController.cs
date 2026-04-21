using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using VinhKhanhTourGuide.WebAdmin.Data;
using VinhKhanhTourGuide.WebAdmin.Models;

namespace VinhKhanhTourGuide.WebAdmin.Controllers
{
    public class AnalyticsController : Controller
    {
        private const int ActiveWindowSeconds = 20;
        private const int FutureHeartbeatToleranceSeconds = 5;
        private readonly TourDbContext _context;

        public AnalyticsController(TourDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var viewModel = new AnalyticsDashboardViewModel();

            await PopulateActiveVisitorsAsync(viewModel, DateTime.Now.AddSeconds(-ActiveWindowSeconds));

            var rawStats = await _context.ListeningLogs
                .Join(
                    _context.Poi,
                    log => log.PoiId,
                    poi => poi.Id,
                    (log, poi) => new { log.PoiId, poi.Name, log.DurationSeconds })
                .GroupBy(x => new { x.PoiId, x.Name })
                .Select(g => new AnalyticsViewModel
                {
                    PoiName = g.Key.Name ?? g.Key.PoiId,
                    TotalListens = g.Count(),
                    AverageDuration = g.Average(x => x.DurationSeconds)
                })
                .OrderByDescending(v => v.TotalListens)
                .ToListAsync();

            foreach (var item in rawStats)
            {
                item.AverageDuration = Math.Round(item.AverageDuration, 1);
            }

            var rawCoords = await _context.ListeningLogs
                .Where(l => l.Latitude != 0 && l.Longitude != 0)
                .Select(l => new double[] { l.Latitude, l.Longitude, 1 })
                .ToListAsync();

            viewModel.PoiStats = rawStats;
            viewModel.HeatmapDataJson = System.Text.Json.JsonSerializer.Serialize(rawCoords);

            return View(viewModel);
        }

        [HttpGet]
        public async Task<IActionResult> Snapshot()
        {
            var viewModel = new AnalyticsDashboardViewModel();
            await PopulateActiveVisitorsAsync(viewModel, DateTime.Now.AddSeconds(-ActiveWindowSeconds));

            return Json(new
            {
                activeWindowSeconds = viewModel.ActiveWindowSeconds,
                snapshotGeneratedAt = viewModel.SnapshotGeneratedAt,
                activeUsersNow = viewModel.ActiveUsersNow,
                activeVisitors = viewModel.ActiveVisitors
            });
        }

        [HttpPost]
        public async Task<IActionResult> ClearActiveVisitors()
        {
            try
            {
                await _context.Database.ExecuteSqlRawAsync("DELETE FROM [VisitorActivity]");
                TempData["AnalyticsMessage"] = "Da xoa toan bo session active cu.";
            }
            catch (SqlException ex) when (ex.Message.Contains("VisitorActivity", StringComparison.OrdinalIgnoreCase))
            {
                TempData["AnalyticsMessage"] = "Bang VisitorActivity chua ton tai, khong co session nao de xoa.";
            }

            return RedirectToAction(nameof(Index));
        }

        private async Task PopulateActiveVisitorsAsync(AnalyticsDashboardViewModel viewModel, DateTime activeCutoff)
        {
            viewModel.ActiveWindowSeconds = ActiveWindowSeconds;
            viewModel.SnapshotGeneratedAt = DateTime.Now;
            var activeUpperBound = viewModel.SnapshotGeneratedAt.AddSeconds(FutureHeartbeatToleranceSeconds);

            try
            {
                var activeVisitorsRaw = await (
                    from activity in _context.VisitorActivities.AsNoTracking()
                    where activity.LastSeenAt >= activeCutoff
                        && activity.LastSeenAt <= activeUpperBound
                    join nearestPoi in _context.Poi.AsNoTracking() on activity.NearestPoiId equals nearestPoi.Id into nearestPoiGroup
                    from nearestPoi in nearestPoiGroup.DefaultIfEmpty()
                    join listeningPoi in _context.Poi.AsNoTracking() on activity.CurrentListeningPoiId equals listeningPoi.Id into listeningPoiGroup
                    from listeningPoi in listeningPoiGroup.DefaultIfEmpty()
                    orderby activity.LastSeenAt descending
                    select new
                    {
                        activity.AnonymousSessionId,
                        activity.Status,
                        activity.DistanceToNearestPoiMeters,
                        activity.LastSeenAt,
                        NearestPoiName = nearestPoi != null ? nearestPoi.Name : null,
                        CurrentListeningPoiName = listeningPoi != null ? listeningPoi.Name : null
                    })
                    .ToListAsync();

                viewModel.ActiveVisitors = activeVisitorsRaw
                    .Select(visitor => new AnalyticsActiveVisitorViewModel
                    {
                        AnonymousSessionId = MaskSessionId(visitor.AnonymousSessionId),
                        Status = visitor.Status,
                        NearestPoiName = visitor.NearestPoiName,
                        DistanceToNearestPoiMeters = visitor.DistanceToNearestPoiMeters.HasValue
                            ? Math.Round(visitor.DistanceToNearestPoiMeters.Value, 1)
                            : null,
                        CurrentListeningPoiName = visitor.CurrentListeningPoiName,
                        LastSeenAt = visitor.LastSeenAt
                    })
                    .ToList();

                viewModel.ActiveUsersNow = viewModel.ActiveVisitors.Count;
            }
            catch (SqlException ex) when (ex.Message.Contains("VisitorActivity", StringComparison.OrdinalIgnoreCase))
            {
                viewModel.ActiveUsersNow = 0;
                viewModel.ActiveVisitors = new();
            }
        }

        private static string MaskSessionId(string? sessionId)
        {
            if (string.IsNullOrWhiteSpace(sessionId))
            {
                return "N/A";
            }

            if (sessionId.Length <= 8)
            {
                return sessionId;
            }

            return $"{sessionId[..4]}...{sessionId[^4..]}";
        }
    }
}
