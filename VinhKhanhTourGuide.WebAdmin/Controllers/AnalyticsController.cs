using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VinhKhanhTourGuide.WebAdmin.Models;
using VinhKhanhTourGuide.WebAdmin.Data;

namespace VinhKhanhTourGuide.WebAdmin.Controllers
{
    public class AnalyticsController : Controller
    {
        private readonly TourDbContext _context;

        public AnalyticsController(TourDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            // 1. JOIN trước, GROUP BY sau để EF Core dễ dàng dịch sang SQL
            var rawStats = await _context.ListeningLogs
                .Join(
                    _context.Poi,
                    log => log.PoiId,       // Khóa ngoại từ bảng ListeningLog
                    poi => poi.Id,          // Khóa chính từ bảng Poi
                    (log, poi) => new { log.PoiId, poi.Name, log.DurationSeconds } // Chỉ lấy các cột cần thiết
                )
                .GroupBy(x => new { x.PoiId, x.Name }) // Nhóm dữ liệu lại theo Quán
                .Select(g => new AnalyticsViewModel
                {
                    PoiName = g.Key.Name ?? g.Key.PoiId,
                    TotalListens = g.Count(),
                    // Để nguyên hàm Average, KHÔNG dùng Math.Round ở bước này
                    AverageDuration = g.Average(x => x.DurationSeconds)
                })
                .OrderByDescending(v => v.TotalListens)
                .ToListAsync(); // Chạy lệnh SQL và kéo kết quả thống kê (vài chục dòng) về RAM

            // 2. Định dạng lại số thập phân (Math.Round) bằng C#
            foreach (var item in rawStats)
            {
                item.AverageDuration = Math.Round(item.AverageDuration, 1);
            }

            // 3. Xử lý Heatmap: chỉ lấy 2 cột tọa độ cần thiết, bỏ qua tọa độ (0, 0) bị lỗi
            var rawCoords = await _context.ListeningLogs
                .Where(l => l.Latitude != 0 && l.Longitude != 0)
                .Select(l => new double[] { l.Latitude, l.Longitude, 1 })
                .ToListAsync();

            ViewBag.HeatmapData = System.Text.Json.JsonSerializer.Serialize(rawCoords);

            return View(rawStats);
        }
    }
}