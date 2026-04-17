using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VinhKhanhTourGuide.WebAdmin.Data;
using VinhKhanhTourGuide.WebAdmin.Models;

namespace VinhKhanhTourGuide.WebAdmin.Controllers
{
    [Route("api/pois")]
    [ApiController]
    public class PoisApiController : ControllerBase
    {
        private readonly TourDbContext _context;

        public PoisApiController(TourDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<object>>> GetPois()
        {
            // Tự động lấy địa chỉ server (ví dụ: http://10.0.2.2:5099)
            var scheme = Request.Host.Value.Contains("ngrok-free") ? "https" : Request.Scheme;
            var baseUrl = $"{scheme}://{Request.Host}{Request.PathBase}";

            var pois = await _context.Poi.OrderBy(p => p.Priority).ToListAsync();

            // Trả về một danh sách mới có kèm link ảnh đầy đủ
            var result = pois.Select(p => new {
                p.Id,
                p.Name,
                p.Latitude,
                p.Longitude,
                p.Description_VN,
                p.GeofenceRadius,
                p.Priority,
                // Tạo link ảnh hoàn chỉnh
                ImageUrl = $"{baseUrl}/images/{p.ImageName}"
            });

            return Ok(result);
        }
    }
}