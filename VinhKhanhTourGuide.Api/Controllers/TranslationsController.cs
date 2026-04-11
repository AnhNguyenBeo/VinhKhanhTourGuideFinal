using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VinhKhanhTourGuide.Api.Data;
using VinhKhanhTourGuide.Api.Models;

namespace VinhKhanhTourGuide.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class TranslationsController : ControllerBase
    {
        private readonly TourDbContext _context;

        public TranslationsController(TourDbContext context)
        {
            _context = context;
        }

        // GET: api/translations
        // GET: api/translations?poiId=OC-OANH&languageCode=en-US
        [HttpGet]
        public async Task<IActionResult> GetTranslations([FromQuery] string? poiId, [FromQuery] string? languageCode)
        {
            var query = _context.TranslationEntries.AsQueryable();

            if (!string.IsNullOrWhiteSpace(poiId))
            {
                query = query.Where(t => t.PoiId == poiId);
            }

            if (!string.IsNullOrWhiteSpace(languageCode))
            {
                query = query.Where(t => t.LanguageCode == languageCode);
            }

            var data = await query
                .OrderBy(t => t.PoiId)
                .ThenBy(t => t.LanguageCode)
                .ToListAsync();

            return Ok(data);
        }

        // GET: api/translations/5
        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetTranslationById(int id)
        {
            var item = await _context.TranslationEntries.FindAsync(id);

            if (item == null)
            {
                return NotFound("Không tìm thấy bản dịch.");
            }

            return Ok(item);
        }

        // GET: api/translations/by-poi/OC-OANH?languageCode=en-US
        [HttpGet("by-poi/{poiId}")]
        public async Task<IActionResult> GetTranslationByPoi(string poiId, [FromQuery] string languageCode)
        {
            if (string.IsNullOrWhiteSpace(languageCode))
            {
                return BadRequest("languageCode là bắt buộc.");
            }

            var item = await _context.TranslationEntries
                .FirstOrDefaultAsync(t => t.PoiId == poiId && t.LanguageCode == languageCode && t.IsApproved);

            if (item == null)
            {
                return NotFound("Không tìm thấy bản dịch phù hợp.");
            }

            return Ok(item);
        }

        // POST: api/translations
        [HttpPost]
        public async Task<IActionResult> CreateTranslation([FromBody] TranslationEntry input)
        {
            if (input == null)
            {
                return BadRequest("Dữ liệu không hợp lệ.");
            }

            if (string.IsNullOrWhiteSpace(input.PoiId) ||
                string.IsNullOrWhiteSpace(input.LanguageCode) ||
                string.IsNullOrWhiteSpace(input.TranslatedText))
            {
                return BadRequest("PoiId, LanguageCode và TranslatedText là bắt buộc.");
            }

            var poi = await _context.Poi.FindAsync(input.PoiId);
            if (poi == null)
            {
                return NotFound("POI không tồn tại.");
            }

            bool exists = await _context.TranslationEntries
                .AnyAsync(t => t.PoiId == input.PoiId && t.LanguageCode == input.LanguageCode);

            if (exists)
            {
                return BadRequest("Bản dịch cho POI và ngôn ngữ này đã tồn tại.");
            }

            input.CreatedAt = DateTime.Now;
            input.UpdatedAt = null;

            _context.TranslationEntries.Add(input);
            await _context.SaveChangesAsync();

            return Ok(new
            {
                success = true,
                message = "Tạo bản dịch thành công.",
                data = input
            });
        }

        // PUT: api/translations/5
        [HttpPut("{id:int}")]
        public async Task<IActionResult> UpdateTranslation(int id, [FromBody] TranslationEntry input)
        {
            if (input == null || id != input.Id)
            {
                return BadRequest("Dữ liệu cập nhật không hợp lệ.");
            }

            var existing = await _context.TranslationEntries.FindAsync(id);
            if (existing == null)
            {
                return NotFound("Không tìm thấy bản dịch để cập nhật.");
            }

            var poi = await _context.Poi.FindAsync(input.PoiId);
            if (poi == null)
            {
                return NotFound("POI không tồn tại.");
            }

            bool duplicate = await _context.TranslationEntries
                .AnyAsync(t =>
                    t.Id != id &&
                    t.PoiId == input.PoiId &&
                    t.LanguageCode == input.LanguageCode);

            if (duplicate)
            {
                return BadRequest("Đã có bản dịch khác cùng PoiId và LanguageCode.");
            }

            existing.PoiId = input.PoiId;
            existing.LanguageCode = input.LanguageCode;
            existing.TranslatedText = input.TranslatedText;
            existing.Source = input.Source;
            existing.IsApproved = input.IsApproved;
            existing.UpdatedAt = DateTime.Now;

            await _context.SaveChangesAsync();

            return Ok(new
            {
                success = true,
                message = "Cập nhật bản dịch thành công.",
                data = existing
            });
        }

        // DELETE: api/translations/5
        [HttpDelete("{id:int}")]
        public async Task<IActionResult> DeleteTranslation(int id)
        {
            var item = await _context.TranslationEntries.FindAsync(id);
            if (item == null)
            {
                return NotFound("Không tìm thấy bản dịch để xóa.");
            }

            _context.TranslationEntries.Remove(item);
            await _context.SaveChangesAsync();

            return Ok(new
            {
                success = true,
                message = "Xóa bản dịch thành công."
            });
        }
    }
}