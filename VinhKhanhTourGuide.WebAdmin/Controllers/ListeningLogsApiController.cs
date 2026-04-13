using Microsoft.AspNetCore.Mvc;
using VinhKhanhTourGuide.WebAdmin.Data;
using VinhKhanhTourGuide.WebAdmin.Models;

namespace VinhKhanhTourGuide.WebAdmin.Controllers
{
    [Route("api/listeninglogs")]
    [ApiController]
    public class ListeningLogsApiController : ControllerBase
    {
        private readonly TourDbContext _context;

        public ListeningLogsApiController(TourDbContext context)
        {
            _context = context;
        }

        // POST: api/listeninglogs
        [HttpPost]
        public async Task<IActionResult> PostListeningLog([FromBody] ListeningLog log)
        {
            if (log == null) return BadRequest();

            try
            {
                // Đảm bảo thời gian được ghi nhận chính xác lúc server nhận dữ liệu
                log.ListenAt = DateTime.Now;

                _context.ListeningLogs.Add(log);
                await _context.SaveChangesAsync();

                return Ok(new { message = "Lưu log thành công!" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Lỗi hệ thống: {ex.Message}");
            }
        }
    }
}