using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using VinhKhanhTourGuide.Api.Data;
using VinhKhanhTourGuide.Api.Models;

namespace VinhKhanhTourGuide.Api.Controllers
{
    [Route("api/visitoractivity")]
    [ApiController]
    public class VisitorActivityController : ControllerBase
    {
        private readonly TourDbContext _context;

        public VisitorActivityController(TourDbContext context)
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

            VisitorActivity? activity;

            try
            {
                activity = await _context.VisitorActivities.FindAsync(request.AnonymousSessionId);
            }
            catch (SqlException ex) when (ex.Message.Contains("VisitorActivity", StringComparison.OrdinalIgnoreCase))
            {
                await EnsureVisitorActivityTableAsync();
                activity = await _context.VisitorActivities.FindAsync(request.AnonymousSessionId);
            }

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

        private async Task EnsureVisitorActivityTableAsync()
        {
            const string sql = """
IF OBJECT_ID(N'[VisitorActivity]', N'U') IS NULL
BEGIN
    CREATE TABLE [VisitorActivity]
    (
        [AnonymousSessionId] NVARCHAR(450) NOT NULL,
        [Latitude] FLOAT NULL,
        [Longitude] FLOAT NULL,
        [NearestPoiId] NVARCHAR(MAX) NULL,
        [DistanceToNearestPoiMeters] FLOAT NULL,
        [Status] NVARCHAR(MAX) NOT NULL,
        [CurrentListeningPoiId] NVARCHAR(MAX) NULL,
        [LastEvent] NVARCHAR(MAX) NULL,
        [Platform] NVARCHAR(MAX) NULL,
        [LastSeenAt] DATETIME2 NOT NULL,
        CONSTRAINT [PK_VisitorActivity] PRIMARY KEY ([AnonymousSessionId])
    );
END
""";

            await _context.Database.ExecuteSqlRawAsync(sql);
        }
    }
}
