using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VinhKhanhTourGuide.Api.Data;

namespace VinhKhanhTourGuide.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PoisController : ControllerBase
    {
        private readonly TourDbContext _context;

        public PoisController(TourDbContext context)
        {
            _context = context;
        }

        // Lệnh GET: Lấy toàn bộ danh sách quán ăn
        [HttpGet]
        public async Task<IActionResult> GetAllPois()
        {
            var baseUrl = "http://10.0.2.2:5099";

            var pois = await _context.Poi
                .OrderBy(p => p.Priority)
                .ToListAsync();

            foreach (var poi in pois)
            {
                poi.ImageUrl = string.IsNullOrWhiteSpace(poi.ImageName)
                    ? null
                    : $"{baseUrl}/images/{poi.ImageName}";
            }

            return Ok(pois);
        }
    }
}