using Microsoft.AspNetCore.Mvc;
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

        public IActionResult Index()
        {
            // Lấy data, gom nhóm theo quán và tính tổng số lượt nghe + thời gian trung bình
            var stats = _context.ListeningLogs
                .GroupBy(log => log.PoiId)
                .Select(group => new
                {
                    PoiId = group.Key,
                    TotalListens = group.Count(),
                    AverageDuration = group.Average(l => l.DurationSeconds)
                })
                .ToList();

            // Kết hợp với bảng Poi để lấy tên quán cho đẹp (thay vì hiện mã OC-OANH)
            var allPois = _context.Poi.ToList();

            var viewModelList = new List<AnalyticsViewModel>();
            foreach (var item in stats)
            {
                var poi = allPois.FirstOrDefault(p => p.Id == item.PoiId);
                viewModelList.Add(new AnalyticsViewModel
                {
                    PoiName = poi != null ? poi.Name : item.PoiId,
                    TotalListens = item.TotalListens,
                    AverageDuration = Math.Round(item.AverageDuration, 1) // Làm tròn 1 chữ số
                });
            }

            // Sắp xếp quán nào hot nhất lên đầu
            viewModelList = viewModelList.OrderByDescending(v => v.TotalListens).ToList();

            var rawCoords = _context.ListeningLogs
                .Where(l => l.Latitude != 0 && l.Longitude != 0)
                .Select(l => new double[] { l.Latitude, l.Longitude, 1 }) // Số 1 ở cuối là 'cường độ nhiệt'
                .ToList();

            ViewBag.HeatmapData = System.Text.Json.JsonSerializer.Serialize(rawCoords);
            return View(viewModelList);
        }
    }
}