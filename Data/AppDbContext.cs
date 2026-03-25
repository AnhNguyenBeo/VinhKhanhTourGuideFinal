using SQLite;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using VinhKhanhTourGuide.Models;

namespace VinhKhanhTourGuide.Data
{
    public class AppDbContext
    {
        private SQLiteAsyncConnection _database;

        // Hàm khởi tạo DB
        private async Task Init()
        {
            if (_database is not null) return;

            // ĐỔI THÀNH v4 để nạp cột Priority mới
            var dbPath = Path.Combine(FileSystem.AppDataDirectory, "VinhKhanhGuide_v4.db3");
            _database = new SQLiteAsyncConnection(dbPath);

            await _database.CreateTableAsync<Poi>();
            await _database.CreateTableAsync<TranslationCache>();

            var count = await _database.Table<Poi>().CountAsync();
            if (count == 0)
            {
                var pois = new List<Poi>
                {
                    // Ốc Oanh nổi tiếng nhất -> Priority = 1
                    new Poi { Id = "OC-OANH", Name = "Ốc Oanh", ImageName = "placeholder_ocoanh.png", Priority = 1, Latitude = 10.7607099, Longitude = 106.7032565, GeofenceRadius = 30, Description_VN = "Ốc Oanh là biểu tượng ẩm thực của phố Vĩnh Khánh..." },
                    // Ốc Vũ -> Priority = 2
                    new Poi { Id = "OC-VU", Name = "Ốc Vũ", ImageName = "placeholder_ocvu.png", Priority = 2, Latitude = 10.7613801, Longitude = 106.7026756, GeofenceRadius = 30, Description_VN = "Nằm ngay đoạn đầu đường, Ốc Vũ nổi bật..." },
                    new Poi { Id = "OC-NHI", Name = "Ốc Nhi", ImageName = "placeholder_ocnhi.png", Priority = 3, Latitude = 10.761266, Longitude = 106.7059247, GeofenceRadius = 30, Description_VN = "Ốc Nhi thu hút giới trẻ Sài Thành..." },
                    new Poi { Id = "CHILLI", Name = "Chilli Quán", ImageName = "placeholder_chilli.png", Priority = 4, Latitude = 10.7606724, Longitude = 106.7035361, GeofenceRadius = 30, Description_VN = "Nếu bạn hơi ngán hải sản..." },
                    new Poi { Id = "OC-THAO", Name = "Ốc Thảo", ImageName = "placeholder_octhao.png", Priority = 5, Latitude = 10.7616745, Longitude = 106.7023583, GeofenceRadius = 30, Description_VN = "Quán ốc lâu đời gắn liền với nhiều thế hệ..." },
                };
                await _database.InsertAllAsync(pois);
            }
        }
        // ---------- CÁC HÀM TRUY VẤN DỮ LIỆU ----------

        public async Task<List<Poi>> GetPoisAsync()
        {
            await Init();
            return await _database.Table<Poi>().ToListAsync();
        }

        public async Task<Poi> GetPoiByIdAsync(string id)
        {
            await Init();
            return await _database.Table<Poi>().Where(p => p.Id == id).FirstOrDefaultAsync();
        }

        // ---------- CÁC HÀM XỬ LÝ CACHE (Dành cho Option B: Gọi AI) ----------

        public async Task<TranslationCache> GetCacheAsync(string poiId, string langCode)
        {
            await Init();
            return await _database.Table<TranslationCache>()
                                  .Where(c => c.PoiId == poiId && c.LanguageCode == langCode)
                                  .FirstOrDefaultAsync();
        }

        public async Task SaveCacheAsync(TranslationCache cache)
        {
            await Init();
            await _database.InsertAsync(cache);
        }
    }
}