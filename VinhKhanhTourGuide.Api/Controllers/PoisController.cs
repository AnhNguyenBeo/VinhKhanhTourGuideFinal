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
        private readonly string? _publicImageBaseUrl;

        public PoisController(TourDbContext context, IConfiguration configuration)
        {
            _context = context;
            _publicImageBaseUrl = configuration["PublicAssets:ImageBaseUrl"]?.TrimEnd('/');
        }

        // Lệnh GET: Lấy toàn bộ danh sách quán ăn
        [HttpGet]
        public async Task<IActionResult> GetAllPois()
        {
            var scheme = Request.Host.Value.Contains("ngrok-free") ? "https" : Request.Scheme;
            var fallbackBaseUrl = $"{scheme}://{Request.Host}{Request.PathBase}".TrimEnd('/');
            var imageBaseUrl = string.IsNullOrWhiteSpace(_publicImageBaseUrl)
                ? fallbackBaseUrl
                : _publicImageBaseUrl;

            var pois = await _context.Poi
                .OrderBy(p => p.Priority)
                .ToListAsync();

            foreach (var poi in pois)
            {
                poi.ImageUrl = string.IsNullOrWhiteSpace(poi.ImageName)
                    ? null
                    : $"{imageBaseUrl}/images/{poi.ImageName}";
            }

            return Ok(pois);
        }
    }
}
