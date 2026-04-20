using System;
using System.Linq;
using System.Text.Json;
using Microsoft.Data.SqlClient;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VinhKhanhTourGuide.WebAdmin.Data;
using VinhKhanhTourGuide.WebAdmin.Models;

namespace VinhKhanhTourGuide.WebAdmin.Controllers
{
    public class MonitoringController : Controller
    {
        private readonly TourDbContext _context;

        public MonitoringController(TourDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var viewModel = await BuildDashboardAsync();
            return View(viewModel);
        }

        [HttpGet]
        public async Task<IActionResult> Snapshot()
        {
            var viewModel = await BuildDashboardAsync();

            return Json(new
            {
                activeUsersNow = viewModel.ActiveUsersNow,
                nearPoiUsersNow = viewModel.NearPoiUsersNow,
                listeningUsersNow = viewModel.ListeningUsersNow,
                totalPois = viewModel.TotalPois,
                totalListens = viewModel.TotalListens,
                listensToday = viewModel.ListensToday,
                uniqueVisitorsToday = viewModel.UniqueVisitorsToday,
                averageDurationToday = viewModel.AverageDurationToday,
                shortListensToday = viewModel.ShortListensToday,
                lastListenAt = viewModel.LastListenAt,
                alerts = viewModel.Alerts,
                activeVisitors = viewModel.ActiveVisitors,
                topPoisToday = viewModel.TopPoisToday,
                recentLogs = viewModel.RecentLogs
            });
        }

        private async Task<MonitoringDashboardViewModel> BuildDashboardAsync()
        {
            var now = DateTime.Now;
            var startOfToday = now.Date;
            var startOfTomorrow = startOfToday.AddDays(1);
            var activeCutoff = now.AddSeconds(-60);

            var viewModel = new MonitoringDashboardViewModel
            {
                TotalPois = await _context.Poi.AsNoTracking().CountAsync(),
                TotalListens = await _context.ListeningLogs.AsNoTracking().CountAsync(),
                LastListenAt = await _context.ListeningLogs
                    .AsNoTracking()
                    .OrderByDescending(x => x.ListenAt)
                    .Select(x => (DateTime?)x.ListenAt)
                    .FirstOrDefaultAsync()
            };

            var todayDurations = await _context.ListeningLogs
                .AsNoTracking()
                .Where(x => x.ListenAt >= startOfToday && x.ListenAt < startOfTomorrow)
                .Select(x => x.DurationSeconds)
                .ToListAsync();

            viewModel.ListensToday = todayDurations.Count;
            viewModel.AverageDurationToday = todayDurations.Count == 0
                ? 0
                : Math.Round(todayDurations.Average(), 1);
            viewModel.ShortListensToday = todayDurations.Count(x => x < 5);

            viewModel.UniqueVisitorsToday = await _context.ListeningLogs
                .AsNoTracking()
                .Where(x => x.ListenAt >= startOfToday && x.ListenAt < startOfTomorrow)
                .Select(x => x.AnonymousSessionId)
                .Distinct()
                .CountAsync();

            await PopulateActiveVisitorsAsync(viewModel, activeCutoff);

            viewModel.TopPoisToday = await (
                from log in _context.ListeningLogs.AsNoTracking()
                where log.ListenAt >= startOfToday && log.ListenAt < startOfTomorrow
                join poi in _context.Poi.AsNoTracking() on log.PoiId equals poi.Id into poiGroup
                from poi in poiGroup.DefaultIfEmpty()
                group log by new { log.PoiId, PoiName = poi != null ? poi.Name : null } into g
                orderby g.Count() descending
                select new MonitoringTopPoiViewModel
                {
                    PoiName = g.Key.PoiName ?? g.Key.PoiId,
                    TotalListens = g.Count(),
                    AverageDuration = g.Average(x => x.DurationSeconds)
                })
                .Take(5)
                .ToListAsync();

            foreach (var item in viewModel.TopPoisToday)
            {
                item.AverageDuration = Math.Round(item.AverageDuration, 1);
            }

            var hourlyCountsRaw = await _context.ListeningLogs
                .AsNoTracking()
                .Where(x => x.ListenAt >= startOfToday && x.ListenAt < startOfTomorrow)
                .GroupBy(x => x.ListenAt.Hour)
                .Select(g => new { Hour = g.Key, Count = g.Count() })
                .ToListAsync();

            var hourlyLookup = hourlyCountsRaw.ToDictionary(x => x.Hour, x => x.Count);
            var hourlyLabels = Enumerable.Range(0, 24)
                .Select(hour => $"{hour:00}:00")
                .ToList();
            var hourlyCounts = Enumerable.Range(0, 24)
                .Select(hour => hourlyLookup.TryGetValue(hour, out var count) ? count : 0)
                .ToList();

            viewModel.HourlyLabelsJson = JsonSerializer.Serialize(hourlyLabels);
            viewModel.HourlyCountsJson = JsonSerializer.Serialize(hourlyCounts);

            var recentLogs = await (
                from log in _context.ListeningLogs.AsNoTracking()
                orderby log.ListenAt descending
                join poi in _context.Poi.AsNoTracking() on log.PoiId equals poi.Id into poiGroup
                from poi in poiGroup.DefaultIfEmpty()
                select new
                {
                    log.AnonymousSessionId,
                    log.DurationSeconds,
                    log.ListenAt,
                    PoiName = poi != null ? poi.Name : log.PoiId
                })
                .Take(20)
                .ToListAsync();

            viewModel.RecentLogs = recentLogs
                .Select(log => new MonitoringRecentLogViewModel
                {
                    PoiName = log.PoiName ?? "Unknown",
                    AnonymousSessionId = MaskSessionId(log.AnonymousSessionId),
                    DurationSeconds = Math.Round(log.DurationSeconds, 1),
                    ListenAt = log.ListenAt
                })
                .ToList();

            if (viewModel.TotalPois == 0)
            {
                viewModel.Alerts.Add("No POI is configured yet. Mobile app will not have content to load.");
            }

            if (!viewModel.LastListenAt.HasValue)
            {
                viewModel.Alerts.Add("No listening log has been received yet.");
            }
            else if (viewModel.LastListenAt.Value < now.AddHours(-2))
            {
                viewModel.Alerts.Add("No new listening log has arrived in the last 2 hours. Check API, network, or traffic volume.");
            }

            if (viewModel.ListensToday == 0)
            {
                viewModel.Alerts.Add("There is no listening activity today.");
            }
            else if ((double)viewModel.ShortListensToday / viewModel.ListensToday >= 0.4)
            {
                viewModel.Alerts.Add("Short listening sessions are high today. Content, TTS, or geofence behavior may need checking.");
            }

            if (viewModel.ActiveUsersNow == 0)
            {
                viewModel.Alerts.Add("There is no active visitor heartbeat in the last 60 seconds.");
            }

            return viewModel;
        }

        private async Task PopulateActiveVisitorsAsync(MonitoringDashboardViewModel viewModel, DateTime activeCutoff)
        {
            try
            {
                var activeVisitorsRaw = await (
                    from activity in _context.VisitorActivities.AsNoTracking()
                    where activity.LastSeenAt >= activeCutoff
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
                    .Select(visitor => new MonitoringActiveVisitorViewModel
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
                viewModel.NearPoiUsersNow = viewModel.ActiveVisitors.Count(x => x.Status == "near_poi");
                viewModel.ListeningUsersNow = viewModel.ActiveVisitors.Count(x => x.Status == "listening");
            }
            catch (SqlException ex) when (ex.Message.Contains("VisitorActivity", StringComparison.OrdinalIgnoreCase))
            {
                viewModel.ActiveVisitors = new();
                viewModel.ActiveUsersNow = 0;
                viewModel.NearPoiUsersNow = 0;
                viewModel.ListeningUsersNow = 0;
                viewModel.Alerts.Add("Live monitoring table VisitorActivity chưa sẵn sàng. Hãy chạy migration hoặc khởi động lại WebAdmin để hệ thống tự tạo bảng.");
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
