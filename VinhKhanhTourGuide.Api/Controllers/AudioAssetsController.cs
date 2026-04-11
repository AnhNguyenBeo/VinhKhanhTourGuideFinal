using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VinhKhanhTourGuide.Api.Data;
using VinhKhanhTourGuide.Api.Models;

namespace VinhKhanhTourGuide.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AudioAssetsController : ControllerBase
    {
        private readonly TourDbContext _context;

        public AudioAssetsController(TourDbContext context)
        {
            _context = context;
        }

        // GET: api/audioassets
        // GET: api/audioassets?poiId=OC-OANH&languageCode=en-US
        [HttpGet]
        public async Task<IActionResult> GetAudioAssets([FromQuery] string? poiId, [FromQuery] string? languageCode)
        {
            var query = _context.AudioAssets.AsQueryable();

            if (!string.IsNullOrWhiteSpace(poiId))
            {
                query = query.Where(a => a.PoiId == poiId);
            }

            if (!string.IsNullOrWhiteSpace(languageCode))
            {
                query = query.Where(a => a.LanguageCode == languageCode);
            }

            var data = await query
                .OrderBy(a => a.PoiId)
                .ThenBy(a => a.LanguageCode)
                .ToListAsync();

            return Ok(data);
        }

        // GET: api/audioassets/5
        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetAudioAssetById(int id)
        {
            var item = await _context.AudioAssets.FindAsync(id);

            if (item == null)
            {
                return NotFound("Không tìm thấy audio asset.");
            }

            return Ok(item);
        }

        // GET: api/audioassets/by-poi/OC-OANH?languageCode=en-US
        [HttpGet("by-poi/{poiId}")]
        public async Task<IActionResult> GetAudioByPoi(string poiId, [FromQuery] string languageCode)
        {
            if (string.IsNullOrWhiteSpace(languageCode))
            {
                return BadRequest("languageCode là bắt buộc.");
            }

            var item = await _context.AudioAssets
                .FirstOrDefaultAsync(a => a.PoiId == poiId && a.LanguageCode == languageCode && a.IsActive);

            if (item == null)
            {
                return NotFound("Không tìm thấy audio phù hợp.");
            }

            return Ok(item);
        }

        // POST: api/audioassets
        [HttpPost]
        public async Task<IActionResult> CreateAudioAsset([FromBody] AudioAsset input)
        {
            if (input == null)
            {
                return BadRequest("Dữ liệu không hợp lệ.");
            }

            if (string.IsNullOrWhiteSpace(input.PoiId) ||
                string.IsNullOrWhiteSpace(input.LanguageCode) ||
                string.IsNullOrWhiteSpace(input.Title) ||
                string.IsNullOrWhiteSpace(input.FilePath))
            {
                return BadRequest("PoiId, LanguageCode, Title và FilePath là bắt buộc.");
            }

            var poi = await _context.Poi.FindAsync(input.PoiId);
            if (poi == null)
            {
                return NotFound("POI không tồn tại.");
            }

            input.CreatedAt = DateTime.Now;

            _context.AudioAssets.Add(input);
            await _context.SaveChangesAsync();

            return Ok(new
            {
                success = true,
                message = "Tạo audio asset thành công.",
                data = input
            });
        }

        // PUT: api/audioassets/5
        [HttpPut("{id:int}")]
        public async Task<IActionResult> UpdateAudioAsset(int id, [FromBody] AudioAsset input)
        {
            if (input == null || id != input.Id)
            {
                return BadRequest("Dữ liệu cập nhật không hợp lệ.");
            }

            var existing = await _context.AudioAssets.FindAsync(id);
            if (existing == null)
            {
                return NotFound("Không tìm thấy audio asset để cập nhật.");
            }

            var poi = await _context.Poi.FindAsync(input.PoiId);
            if (poi == null)
            {
                return NotFound("POI không tồn tại.");
            }

            existing.PoiId = input.PoiId;
            existing.LanguageCode = input.LanguageCode;
            existing.Title = input.Title;
            existing.FilePath = input.FilePath;
            existing.DurationSeconds = input.DurationSeconds;
            existing.IsActive = input.IsActive;

            await _context.SaveChangesAsync();

            return Ok(new
            {
                success = true,
                message = "Cập nhật audio asset thành công.",
                data = existing
            });
        }

        // DELETE: api/audioassets/5
        [HttpDelete("{id:int}")]
        public async Task<IActionResult> DeleteAudioAsset(int id)
        {
            var item = await _context.AudioAssets.FindAsync(id);
            if (item == null)
            {
                return NotFound("Không tìm thấy audio asset để xóa.");
            }

            _context.AudioAssets.Remove(item);
            await _context.SaveChangesAsync();

            return Ok(new
            {
                success = true,
                message = "Xóa audio asset thành công."
            });
        }
    }
}