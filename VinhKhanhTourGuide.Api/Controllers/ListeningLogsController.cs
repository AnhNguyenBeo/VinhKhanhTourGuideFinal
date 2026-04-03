using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;
using VinhKhanhTourGuide.Api.Models;
using VinhKhanhTourGuide.Api.Data;

namespace VinhKhanhTourGuide.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ListeningLogsController : ControllerBase
    {
        private readonly TourDbContext _context;

        public ListeningLogsController(TourDbContext context)
        {
            _context = context;
        }

        [HttpPost]
        public async Task<IActionResult> PostLog([FromBody] ListeningLog log)
        {
            if (log == null || string.IsNullOrEmpty(log.PoiId))
            {
                return BadRequest("Dữ liệu không hợp lệ.");
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
                // Bới móc lỗi tận gốc (Inner Exception) của SQL Server
                string errorDetail = ex.Message;
                if (ex.InnerException != null)
                {
                    errorDetail += " | NGUYÊN NHÂN GỐC: " + ex.InnerException.Message;
                }

                // Trả về cho điện thoại biết để điện thoại in ra Output
                return StatusCode(500, $"Lỗi server: {errorDetail}");
            }
        }
    }
}