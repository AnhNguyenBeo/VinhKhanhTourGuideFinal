using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VinhKhanhTourGuide.Api.Data;
using VinhKhanhTourGuide.Api.Models;

namespace VinhKhanhTourGuide.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ToursController : ControllerBase
    {
        private readonly TourDbContext _context;

        public ToursController(TourDbContext context)
        {
            _context = context;
        }

        // GET: api/tours
        [HttpGet]
        public async Task<IActionResult> GetAllTours()
        {
            var tours = await _context.Tours
                .OrderBy(t => t.Id)
                .ToListAsync();

            return Ok(tours);
        }

        // GET: api/tours/5
        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetTourById(int id)
        {
            var tour = await _context.Tours.FindAsync(id);

            if (tour == null)
            {
                return NotFound("Không tìm thấy tour.");
            }

            return Ok(tour);
        }

        // POST: api/tours
        [HttpPost]
        public async Task<IActionResult> CreateTour([FromBody] Tour tour)
        {
            if (tour == null)
            {
                return BadRequest("Dữ liệu tour không hợp lệ.");
            }

            if (string.IsNullOrWhiteSpace(tour.Code) || string.IsNullOrWhiteSpace(tour.Name))
            {
                return BadRequest("Code và Name là bắt buộc.");
            }

            bool codeExists = await _context.Tours.AnyAsync(t => t.Code == tour.Code);
            if (codeExists)
            {
                return BadRequest("Code tour đã tồn tại.");
            }

            tour.CreatedAt = DateTime.Now;

            _context.Tours.Add(tour);
            await _context.SaveChangesAsync();

            return Ok(new
            {
                success = true,
                message = "Tạo tour thành công.",
                data = tour
            });
        }

        // PUT: api/tours/5
        [HttpPut("{id:int}")]
        public async Task<IActionResult> UpdateTour(int id, [FromBody] Tour input)
        {
            if (input == null || id != input.Id)
            {
                return BadRequest("Dữ liệu cập nhật không hợp lệ.");
            }

            var existingTour = await _context.Tours.FindAsync(id);
            if (existingTour == null)
            {
                return NotFound("Không tìm thấy tour để cập nhật.");
            }

            bool codeExists = await _context.Tours
                .AnyAsync(t => t.Code == input.Code && t.Id != id);

            if (codeExists)
            {
                return BadRequest("Code tour đã tồn tại ở tour khác.");
            }

            existingTour.Code = input.Code;
            existingTour.Name = input.Name;
            existingTour.Description = input.Description;
            existingTour.EstimatedMinutes = input.EstimatedMinutes;
            existingTour.IsActive = input.IsActive;

            await _context.SaveChangesAsync();

            return Ok(new
            {
                success = true,
                message = "Cập nhật tour thành công.",
                data = existingTour
            });
        }

        // DELETE: api/tours/5
        [HttpDelete("{id:int}")]
        public async Task<IActionResult> DeleteTour(int id)
        {
            var tour = await _context.Tours.FindAsync(id);
            if (tour == null)
            {
                return NotFound("Không tìm thấy tour để xóa.");
            }

            bool hasChildren = await _context.TourPois.AnyAsync(tp => tp.TourId == id);
            if (hasChildren)
            {
                return BadRequest("Tour đang có POI bên trong. Hãy xóa danh sách điểm dừng trước.");
            }

            _context.Tours.Remove(tour);
            await _context.SaveChangesAsync();

            return Ok(new
            {
                success = true,
                message = "Xóa tour thành công."
            });
        }

        // GET: api/tours/5/pois
        [HttpGet("{id:int}/pois")]
        public async Task<IActionResult> GetTourPois(int id)
        {
            var tour = await _context.Tours.FindAsync(id);
            if (tour == null)
            {
                return NotFound("Không tìm thấy tour.");
            }

            var tourPois = await _context.TourPois
                .Where(tp => tp.TourId == id)
                .OrderBy(tp => tp.SortOrder)
                .ToListAsync();

            var poiIds = tourPois.Select(tp => tp.PoiId).ToList();

            var pois = await _context.Poi
                .Where(p => poiIds.Contains(p.Id))
                .ToListAsync();

            var result = tourPois.Select(tp =>
            {
                var poi = pois.FirstOrDefault(p => p.Id == tp.PoiId);

                return new
                {
                    tp.Id,
                    tp.TourId,
                    tp.PoiId,
                    tp.SortOrder,
                    tp.Note,
                    PoiName = poi?.Name,
                    PoiImageName = poi?.ImageName,
                    poi?.Latitude,
                    poi?.Longitude
                };
            });

            return Ok(result);
        }

        // POST: api/tours/5/pois
        [HttpPost("{id:int}/pois")]
        public async Task<IActionResult> AddPoiToTour(int id, [FromBody] TourPoi input)
        {
            if (input == null)
            {
                return BadRequest("Dữ liệu không hợp lệ.");
            }

            var tour = await _context.Tours.FindAsync(id);
            if (tour == null)
            {
                return NotFound("Không tìm thấy tour.");
            }

            var poi = await _context.Poi.FindAsync(input.PoiId);
            if (poi == null)
            {
                return NotFound("Không tìm thấy POI.");
            }

            bool exists = await _context.TourPois
                .AnyAsync(tp => tp.TourId == id && tp.PoiId == input.PoiId);

            if (exists)
            {
                return BadRequest("POI này đã có trong tour.");
            }

            var tourPoi = new TourPoi
            {
                TourId = id,
                PoiId = input.PoiId,
                SortOrder = input.SortOrder,
                Note = input.Note
            };

            _context.TourPois.Add(tourPoi);
            await _context.SaveChangesAsync();

            return Ok(new
            {
                success = true,
                message = "Đã thêm POI vào tour.",
                data = tourPoi
            });
        }

        // DELETE: api/tours/5/pois/12
        [HttpDelete("{id:int}/pois/{tourPoiId:int}")]
        public async Task<IActionResult> RemovePoiFromTour(int id, int tourPoiId)
        {
            var tourPoi = await _context.TourPois
                .FirstOrDefaultAsync(tp => tp.Id == tourPoiId && tp.TourId == id);

            if (tourPoi == null)
            {
                return NotFound("Không tìm thấy điểm dừng trong tour.");
            }

            _context.TourPois.Remove(tourPoi);
            await _context.SaveChangesAsync();

            return Ok(new
            {
                success = true,
                message = "Đã xóa POI khỏi tour."
            });
        }
    }
}