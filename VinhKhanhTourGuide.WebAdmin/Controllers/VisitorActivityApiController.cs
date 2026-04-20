using Microsoft.AspNetCore.Mvc;
using VinhKhanhTourGuide.WebAdmin.Data;
using VinhKhanhTourGuide.WebAdmin.Models;

namespace VinhKhanhTourGuide.WebAdmin.Controllers
{
    [Route("api/visitoractivity")]
    [ApiController]
    public class VisitorActivityApiController : ControllerBase
    {
        private readonly TourDbContext _context;

        public VisitorActivityApiController(TourDbContext context)
        {
            _context = context;
        }

        [HttpPost("heartbeat")]
        public async Task<IActionResult> Heartbeat([FromBody] VisitorActivityHeartbeatRequest request)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.AnonymousSessionId))
            {
                return BadRequest(new { success = false, message = "AnonymousSessionId là bắt buộc." });
            }

            var activity = await _context.VisitorActivities.FindAsync(request.AnonymousSessionId);

            if (activity == null)
            {
                activity = new VisitorActivity
                {
                    AnonymousSessionId = request.AnonymousSessionId
                };

                _context.VisitorActivities.Add(activity);
            }

            activity.Latitude = request.Latitude;
            activity.Longitude = request.Longitude;
            activity.NearestPoiId = request.NearestPoiId;
            activity.DistanceToNearestPoiMeters = request.DistanceToNearestPoiMeters;
            activity.Status = string.IsNullOrWhiteSpace(request.Status) ? "app_open" : request.Status;
            activity.CurrentListeningPoiId = request.CurrentListeningPoiId;
            activity.LastEvent = request.LastEvent;
            activity.Platform = request.Platform;
            activity.LastSeenAt = DateTime.Now;

            await _context.SaveChangesAsync();

            return Ok(new { success = true, message = "Heartbeat đã được cập nhật." });
        }
    }
}
