using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging; // Thêm thư viện Logger
using VinhKhanhTourGuide.Api.Models;
using VinhKhanhTourGuide.Api.Data;

namespace VinhKhanhTourGuide.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ListeningLogsController : ControllerBase
    {
        private readonly TourDbContext _context;
        private readonly ILogger<ListeningLogsController> _logger; // Khai báo biến Logger

        // Nhúng ILogger vào Constructor
        public ListeningLogsController(TourDbContext context, ILogger<ListeningLogsController> logger)
        {
            _context = context;
            _logger = logger;
        }

        [HttpPost]
        public async Task<IActionResult> PostLog([FromBody] ListeningLog log)
        {
            if (log == null || string.IsNullOrEmpty(log.PoiId))
            {
                // Nên trả về JSON chuẩn thay vì chuỗi trơn để App dễ parse
                return BadRequest(new { success = false, message = "Dữ liệu không hợp lệ." });
            }

            // Ép thời gian lưu theo thời gian thực của Server cho chuẩn xác
            log.ListenAt = DateTime.Now;

            try
            {
                _context.ListeningLogs.Add(log);
                await _context.SaveChangesAsync();
                return Ok(new { success = true, message = "Đã thu thập dữ liệu phân tích!" });
            }
            catch (Exception ex)
            {
                // 1. GHI LOG CHI TIẾT RA MÀN HÌNH CONSOLE / FILE TEXT CHO ADMIN
                string errorDetail = ex.InnerException != null ? ex.InnerException.Message : ex.Message;
                _logger.LogError(ex, "Lỗi nghiêm trọng khi lưu ListeningLog: {ErrorDetail}", errorDetail);

                // 2. TRẢ VỀ CÂU BÁO LỖI AN TOÀN, CHUNG CHUNG CHO APP MOBILE
                return StatusCode(500, new { success = false, message = "Đã xảy ra lỗi hệ thống khi xử lý yêu cầu. Vui lòng thử lại sau." });
            }
        }
    }
}