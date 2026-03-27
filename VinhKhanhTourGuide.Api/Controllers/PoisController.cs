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
            var pois = await _context.Poi.ToListAsync();
            return Ok(pois);
        }
    }
}