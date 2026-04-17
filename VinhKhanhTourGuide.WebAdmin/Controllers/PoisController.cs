using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using VinhKhanhTourGuide.WebAdmin.Data;
using VinhKhanhTourGuide.WebAdmin.Models;

namespace VinhKhanhTourGuide.WebAdmin.Controllers
{
    public class PoisController : Controller
    {
        private readonly TourDbContext _context;
        private readonly IWebHostEnvironment _hostEnvironment;

        public PoisController(TourDbContext context, IWebHostEnvironment hostEnvironment)
        {
            _context = context;
            _hostEnvironment = hostEnvironment;
        }

        // Xóa file ảnh vật lý trong wwwroot/images (nếu tồn tại)
        // Bỏ qua các ảnh placeholder/seed để tránh xóa nhầm asset mặc định
        private void DeleteImageFile(string? imageName)
        {
            if (string.IsNullOrWhiteSpace(imageName)) return;

            // Bảo vệ ảnh seed mặc định, không xóa
            if (imageName.StartsWith("placeholder_", StringComparison.OrdinalIgnoreCase)) return;

            string imagePath = Path.Combine(_hostEnvironment.WebRootPath, "images", imageName);

            if (System.IO.File.Exists(imagePath))
            {
                System.IO.File.Delete(imagePath);
                System.Diagnostics.Debug.WriteLine($"🗑️ Đã xóa ảnh vật lý: {imageName}");
            }
        }

        // Lưu file upload mới vào wwwroot/images, trả về tên file đã lưu
        private async Task<string> SaveUploadedImageAsync(IFormFile uploadFile)
        {
            string wwwRootPath = _hostEnvironment.WebRootPath;
            string fileName = Path.GetFileNameWithoutExtension(uploadFile.FileName);
            string extension = Path.GetExtension(uploadFile.FileName);
            string finalFileName = fileName + "_" + DateTime.Now.ToString("yymmssfff") + extension;

            string imageFolder = Path.Combine(wwwRootPath, "images");
            if (!Directory.Exists(imageFolder))
            {
                Directory.CreateDirectory(imageFolder);
            }

            string path = Path.Combine(imageFolder, finalFileName);

            using (var fileStream = new FileStream(path, FileMode.Create))
            {
                await uploadFile.CopyToAsync(fileStream);
            }

            return finalFileName;
        }

        // GET: Pois
        public async Task<IActionResult> Index()
        {
            return View(await _context.Poi.ToListAsync());
        }

        // GET: Pois/Details/5
        public async Task<IActionResult> Details(string id)
        {
            if (id == null) return NotFound();

            var poi = await _context.Poi.FirstOrDefaultAsync(m => m.Id == id);
            if (poi == null) return NotFound();

            return View(poi);
        }

        // GET: Pois/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Pois/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
            [Bind("Id,Name,ImageName,Latitude,Longitude,Description_VN,GeofenceRadius,Priority")] Poi poi,
            IFormFile uploadFile)
        {
            ModelState.Remove("ImageName");
            ModelState.Remove("uploadFile");

            if (ModelState.IsValid)
            {
                if (uploadFile != null && uploadFile.Length > 0)
                {
                    poi.ImageName = await SaveUploadedImageAsync(uploadFile);
                }

                _context.Add(poi);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }

            return View(poi);
        }

        // GET: Pois/Edit/5
        public async Task<IActionResult> Edit(string id)
        {
            if (id == null) return NotFound();

            var poi = await _context.Poi.FindAsync(id);
            if (poi == null) return NotFound();

            return View(poi);
        }

        // POST: Pois/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(
            string id,
            [Bind("Id,Name,ImageName,Latitude,Longitude,Description_VN,GeofenceRadius,Priority")] Poi poi,
            IFormFile uploadFile)
        {
            if (id != poi.Id) return NotFound();

            ModelState.Remove("ImageName");
            ModelState.Remove("uploadFile");

            if (ModelState.IsValid)
            {
                try
                {
                    if (uploadFile != null && uploadFile.Length > 0)
                    {
                        // Lấy tên ảnh cũ trực tiếp từ DB trước khi ghi đè
                        var existingPoi = await _context.Poi.AsNoTracking()
                                                            .FirstOrDefaultAsync(p => p.Id == id);

                        // Xóa file ảnh cũ trên ổ cứng
                        DeleteImageFile(existingPoi?.ImageName);

                        // Lưu ảnh mới và cập nhật tên vào model
                        poi.ImageName = await SaveUploadedImageAsync(uploadFile);
                    }

                    _context.Update(poi);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!PoiExists(poi.Id)) return NotFound();
                    else throw;
                }

                return RedirectToAction(nameof(Index));
            }

            return View(poi);
        }

        // GET: Pois/Delete/5
        public async Task<IActionResult> Delete(string id)
        {
            if (id == null) return NotFound();

            var poi = await _context.Poi.FirstOrDefaultAsync(m => m.Id == id);
            if (poi == null) return NotFound();

            return View(poi);
        }

        // POST: Pois/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(string id)
        {
            var poi = await _context.Poi.FindAsync(id);

            if (poi != null)
            {
                // Xóa file ảnh vật lý trước khi xóa DB
                DeleteImageFile(poi.ImageName);

                _context.Poi.Remove(poi);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool PoiExists(string id)
        {
            return _context.Poi.Any(e => e.Id == id);
        }
    }
}