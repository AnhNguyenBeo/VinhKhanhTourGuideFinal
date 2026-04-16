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
        // Thêm IWebHostEnvironment để truy xuất thư mục wwwroot
        private readonly IWebHostEnvironment _hostEnvironment;

        public PoisController(TourDbContext context, IWebHostEnvironment hostEnvironment)
        {
            _context = context;
            _hostEnvironment = hostEnvironment;
        }

        // GET: Pois
        public async Task<IActionResult> Index()
        {
            return View(await _context.Poi.ToListAsync());
        }

        // GET: Pois/Details/5
        public async Task<IActionResult> Details(string id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var poi = await _context.Poi
                .FirstOrDefaultAsync(m => m.Id == id);
            if (poi == null)
            {
                return NotFound();
            }

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
        // Bổ sung tham số IFormFile uploadFile để hứng file từ View
        public async Task<IActionResult> Create([Bind("Id,Name,ImageName,Latitude,Longitude,Description_VN,GeofenceRadius,Priority")] Poi poi, IFormFile uploadFile)
        {
            // BỎ QUA KIỂM TRA LỖI RỖNG CỦA 2 TRƯỜNG NÀY ĐỂ TRÁNH LỖI FORM
            ModelState.Remove("ImageName");
            ModelState.Remove("uploadFile");

            if (ModelState.IsValid)
            {
                // Xử lý lưu file ảnh nếu có
                if (uploadFile != null && uploadFile.Length > 0)
                {
                    string wwwRootPath = _hostEnvironment.WebRootPath;
                    string fileName = Path.GetFileNameWithoutExtension(uploadFile.FileName);
                    string extension = Path.GetExtension(uploadFile.FileName);

                    // Thêm mốc thời gian để tên file luôn duy nhất, tránh bị ghi đè
                    string finalFileName = fileName + "_" + DateTime.Now.ToString("yymmssfff") + extension;

                    // Tạo thư mục images nếu nó chưa tồn tại
                    string imageFolder = Path.Combine(wwwRootPath, "images");
                    if (!Directory.Exists(imageFolder))
                    {
                        Directory.CreateDirectory(imageFolder);
                    }

                    string path = Path.Combine(imageFolder, finalFileName);

                    // Copy file vào server
                    using (var fileStream = new FileStream(path, FileMode.Create))
                    {
                        await uploadFile.CopyToAsync(fileStream);
                    }

                    // Cập nhật tên file mới vào đối tượng Poi để lưu database
                    poi.ImageName = finalFileName;
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
            if (id == null)
            {
                return NotFound();
            }

            var poi = await _context.Poi.FindAsync(id);
            if (poi == null)
            {
                return NotFound();
            }
            return View(poi);
        }

        // POST: Pois/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(string id, [Bind("Id,Name,ImageName,Latitude,Longitude,Description_VN,GeofenceRadius,Priority")] Poi poi, IFormFile uploadFile)
        {
            if (id != poi.Id)
            {
                return NotFound();
            }

            // BỎ QUA KIỂM TRA LỖI RỖNG
            ModelState.Remove("ImageName");
            ModelState.Remove("uploadFile");

            if (ModelState.IsValid)
            {
                try
                {
                    // Nếu Admin có chọn ảnh mới thì mới xử lý
                    if (uploadFile != null && uploadFile.Length > 0)
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

                        // Ghi đè tên ảnh mới.
                        poi.ImageName = finalFileName;
                    }

                    _context.Update(poi);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!PoiExists(poi.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }
            return View(poi);
        }

        // GET: Pois/Delete/5
        public async Task<IActionResult> Delete(string id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var poi = await _context.Poi
                .FirstOrDefaultAsync(m => m.Id == id);
            if (poi == null)
            {
                return NotFound();
            }

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