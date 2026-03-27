using SQLite;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using VinhKhanhTourGuide.Models;

namespace VinhKhanhTourGuide.Data
{
    public class AppDbContext
    {
        private SQLiteAsyncConnection _database;

        private async Task Init()
        {
            if (_database is not null) return;

            // Đổi tên file thành v5 để nó tạo database mới tinh, không bị kẹt data cũ
            var dbPath = Path.Combine(FileSystem.AppDataDirectory, "VinhKhanhGuide_v5.db3");
            _database = new SQLiteAsyncConnection(dbPath);

            await _database.CreateTableAsync<Poi>();
            await _database.CreateTableAsync<TranslationCache>();

            // =================================================================
            // BƯỚC MỚI: ĐỒNG BỘ DỮ LIỆU TỪ SQL SERVER (API) XUỐNG SQLITE LOCAL
            // =================================================================
            try
            {
                // ========================================================
                // BÙA CHÚ VƯỢT RÀO BẢO MẬT CỦA ANDROID (BỎ QUA LỖI SSL)
                // ========================================================
                var handler = new HttpClientHandler();
                handler.ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true;
                using var client = new HttpClient(handler);

              
                string apiUrl = "http://10.0.2.2:5099/api/pois";

                var response = await client.GetStringAsync(apiUrl);
                var cloudPois = JsonSerializer.Deserialize<List<Poi>>(response, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                if (cloudPois != null && cloudPois.Count > 0)
                {
                    await _database.DeleteAllAsync<Poi>();
                    await _database.InsertAllAsync(cloudPois);
                    System.Diagnostics.Debug.WriteLine("✅ TẢI DATA TỪ API THÀNH CÔNG!");
                }
            }
            catch (Exception ex)
            {
                // IN LỖI ĐỎ CHÓT RA BẢNG OUTPUT ĐỂ BẮT BỆNH
                System.Diagnostics.Debug.WriteLine($"❌ LỖI GỌI API: {ex.Message}");
            }

            // =================================================================
            // BACKUP: NẾU VỪA CÀI APP, CHƯA CÓ DATA, VÀ CŨNG KHÔNG CÓ MẠNG ĐỂ GỌI API
            // =================================================================
            var count = await _database.Table<Poi>().CountAsync();
            if (count == 0)
            {
                var pois = new List<Poi>
                {
                    new Poi { Id = "OC-OANH", Name = "Ốc Oanh", ImageName = "placeholder_ocoanh.png", Priority = 1, Latitude = 10.7607099, Longitude = 106.7032565, GeofenceRadius = 30, Description_VN = "Ốc Oanh là biểu tượng ẩm thực của phố Vĩnh Khánh..." },
                    new Poi { Id = "OC-VU", Name = "Ốc Vũ", ImageName = "placeholder_ocvu.png", Priority = 2, Latitude = 10.7613801, Longitude = 106.7026756, GeofenceRadius = 30, Description_VN = "Nằm ngay đoạn đầu đường, Ốc Vũ nổi bật..." }
                };
                await _database.InsertAllAsync(pois);
            }
        }

        // ---------- CÁC HÀM TRUY VẤN CŨ GIỮ NGUYÊN ----------
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