using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VinhKhanhTourGuide.Api.Data;
using VinhKhanhTourGuide.Api.Models;

namespace VinhKhanhTourGuide.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class RoutePointsController : ControllerBase
    {
        private readonly TourDbContext _context;

        public RoutePointsController(TourDbContext context)
        {
            _context = context;
        }

        // ================================
        // POST: api/routepoints/batch
        // ================================
        [HttpPost("batch")]
        public async Task<IActionResult> InsertBatch([FromBody] RouteBatchRequest request)
        {
            if (request == null || request.Points == null || !request.Points.Any())
            {
                return BadRequest("Danh sách điểm không hợp lệ.");
            }

            var entities = request.Points.Select(p => new RoutePoint
            {
                AnonymousSessionId = request.AnonymousSessionId,
                Latitude = p.Latitude,
                Longitude = p.Longitude,
                AccuracyMeters = p.AccuracyMeters,
                RecordedAt = p.RecordedAt ?? DateTime.Now
            }).ToList();

            _context.RoutePoints.AddRange(entities);
            await _context.SaveChangesAsync();

            return Ok(new
            {
                success = true,
                count = entities.Count
            });
        }

        // ================================
        // GET: api/routepoints/session/{sessionId}
        // ================================
        [HttpGet("session/{sessionId}")]
        public async Task<IActionResult> GetRouteBySession(string sessionId)
        {
            var data = await _context.RoutePoints
                .Where(x => x.AnonymousSessionId == sessionId)
                .OrderBy(x => x.RecordedAt)
                .ToListAsync();

            return Ok(data);
        }

        // ================================
        // GET: api/routepoints/heatmap
        // ================================
        [HttpGet("heatmap")]
        public async Task<IActionResult> GetHeatmap()
        {
            var data = await _context.RoutePoints
                .Select(x => new
                {
                    x.Latitude,
                    x.Longitude
                })
                .ToListAsync();

            return Ok(data);
        }
    }

    // ================================
    // DTO: batch request
    // ================================
    public class RouteBatchRequest
    {
        public string AnonymousSessionId { get; set; }
        public List<RoutePointDto> Points { get; set; }
    }

    public class RoutePointDto
    {
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public double? AccuracyMeters { get; set; }
        public DateTime? RecordedAt { get; set; }
    }
}