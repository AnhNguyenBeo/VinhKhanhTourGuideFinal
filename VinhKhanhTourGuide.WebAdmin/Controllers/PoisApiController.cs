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

        // GET: api/pois
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Poi>>> GetPois()
        {
            // Trả về dữ liệu thô (JSON) để Mobile App dễ dàng giải mã
            return await _context.Poi.OrderBy(p => p.Priority).ToListAsync();
        }
    }
}