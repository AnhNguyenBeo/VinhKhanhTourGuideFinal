using SQLite;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using VinhKhanhTourGuide.Models;
using VinhKhanhTourGuide.Services;
using Microsoft.Maui.Devices; // Cần thiết để lấy DeviceInfo

namespace VinhKhanhTourGuide.Data
{
    public class AppDbContext
    {
        private SQLiteAsyncConnection? _database;
        private readonly PremiumService _premiumService;
        private readonly SemaphoreSlim _initLock = new SemaphoreSlim(1, 1);

        private const string DatabaseFileName = "VinhKhanhGuide.db3";

        // =================================================================
        // CẤU HÌNH API URL ĐỘNG THEO MÔI TRƯỜNG
        // =================================================================
#if DEBUG
        // Đang chạy test (Debug): Tự động đổi IP dựa theo nền tảng
        private static readonly string BaseApiUrl = DeviceInfo.Platform == DevicePlatform.Android
            ? "http://10.0.2.2:5099"
            : "http://localhost:5099";
#else
        // Khi build bản Release (Production): Điền tên miền server thật của bạn vào đây
        // VD: "https://api.vinhkhanhtour.com"
        private static readonly string BaseApiUrl = "http://vinhkhanh.somee.com";
#endif

        private readonly string PoisApiUrl = $"{BaseApiUrl}/api/pois";
        private readonly string AnalyticsApiUrl = $"{BaseApiUrl}/api/listeninglogs";


        public AppDbContext(PremiumService premiumService)
        {
            _premiumService = premiumService;
        }

        private string GetDbPath()
        {
            return Path.Combine(FileSystem.AppDataDirectory, DatabaseFileName);
        }

        // =================================================================
        // HÀM TẠO HTTP CLIENT AN TOÀN
        // =================================================================
        private HttpClient CreateHttpClient()
        {
#if DEBUG
            // Chỉ bỏ qua kiểm tra chứng chỉ SSL khi đang chạy test (Debug)
            var handler = new HttpClientHandler();
            handler.ServerCertificateCustomValidationCallback = (m, c, ch, e) => true;
            return new HttpClient(handler);
#else
            // Bản thật bắt buộc phải tuân thủ chuẩn bảo mật HTTPS của hệ điều hành
            return new HttpClient();
#endif
        }

        private async Task Init()
        {
            if (_database is not null) return;

            await _initLock.WaitAsync();
            try
            {
                if (_database is not null) return;

                var dbPath = GetDbPath();
                _database = new SQLiteAsyncConnection(dbPath);

                await _database.CreateTableAsync<Poi>();
                await _database.CreateTableAsync<TranslationCache>();

                bool isPremium = _premiumService.IsPremium();
                var localPois = await _database.Table<Poi>().ToListAsync();
                bool hasLocalData = localPois.Count > 0;

                if (isPremium)
                {
                    try
                    {
                        // Dùng hàm CreateHttpClient an toàn vừa tạo
                        using var client = CreateHttpClient();

                        var response = await client.GetStringAsync(PoisApiUrl);

                        var cloudPois = JsonSerializer.Deserialize<List<Poi>>(
                            response,
                            new JsonSerializerOptions
                            {
                                PropertyNameCaseInsensitive = true
                            });

                        if (cloudPois != null && cloudPois.Count > 0)
                        {
                            await _database.DeleteAllAsync<Poi>();
                            await _database.InsertAllAsync(cloudPois);

                            System.Diagnostics.Debug.WriteLine($"✅ PREMIUM SYNC OK: {cloudPois.Count} POI");
                            return;
                        }

                        System.Diagnostics.Debug.WriteLine("⚠️ API premium trả về rỗng.");
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"⚠️ PREMIUM API FAIL: {ex.Message}");
                    }

                    if (hasLocalData)
                    {
                        System.Diagnostics.Debug.WriteLine($"📦 PREMIUM OFFLINE CACHE: giữ {localPois.Count} POI local");
                        return;
                    }

                    System.Diagnostics.Debug.WriteLine("⚠️ PREMIUM nhưng chưa có cache local, fallback về 2 POI seed.");
                }

                await SeedStandardPoisAsync();
            }
            finally
            {
                _initLock.Release();
            }
        }

        private async Task SeedStandardPoisAsync()
        {
            if (_database is null) return;

            await _database.DeleteAllAsync<Poi>();

            var standardPois = new List<Poi>
            {
                new Poi
                {
                    Id = "OC-OANH",
                    Name = "Ốc Oanh",
                    ImageName = "placeholder_ocoanh.png",
                    Priority = 1,
                    Latitude = 10.7607099,
                    Longitude = 106.7032565,
                    GeofenceRadius = 30,
                    Description_VN = "Chào mừng bạn đến với Ốc Oanh – 'trái tim' rực rỡ nhất của phố ẩm thực Vĩnh Khánh. Đây là điểm dừng chân không thể bỏ qua cho những tín đồ yêu thích không gian vỉa hè náo nhiệt đúng chất Sài Gòn. Đừng quên thưởng thức món Ốc hương xào muối ớt huyền thoại, với vị cay nồng và độ giòn sần sật đã làm nên thương hiệu của quán suốt nhiều năm qua. Tại đây, hải sản không chỉ tươi ngon mà còn mang đậm hương vị phóng khoáng của Quận 4."
                },
                new Poi
                {
                    Id = "OC-VU",
                    Name = "Ốc Vũ",
                    ImageName = "placeholder_ocvu.png",
                    Priority = 2,
                    Latitude = 10.7613801,
                    Longitude = 106.7026756,
                    GeofenceRadius = 30,
                    Description_VN = "Nằm ngay lối vào của thiên đường ốc, Ốc Vũ đón chào bạn bằng không gian thoáng đãng và thực đơn hải sản phong phú. Điểm nhấn của quán chính là các món sốt trứng muối béo ngậy và ốc móng tay cháy tỏi thơm lừng, cực kỳ bắt miệng khi dùng kèm bánh mì nóng. Nếu bạn tìm kiếm sự nhanh nhẹn trong phục vụ cùng chất lượng món ăn ổn định để khởi đầu hành trình khám phá ẩm thực đêm, Ốc Vũ chính là lựa chọn hoàn hảo."
                }
            };

            await _database.InsertAllAsync(standardPois);
            System.Diagnostics.Debug.WriteLine("📱 STANDARD MODE: 2 POI local");
        }

        public async Task<List<Poi>> GetPoisAsync()
        {
            await Init();
            var pois = await _database!.Table<Poi>().ToListAsync();

            foreach (var p in pois)
            {
                System.Diagnostics.Debug.WriteLine($"[POI] {p.Name} | ImageUrl = {p.ImageUrl}");
            }

            return pois;
        }

        public async Task<Poi> GetPoiByIdAsync(string id)
        {
            await Init();
            return await _database!.Table<Poi>()
                                   .Where(p => p.Id == id)
                                   .FirstOrDefaultAsync();
        }

        public async Task<TranslationCache> GetCacheAsync(string poiId, string langCode)
        {
            await Init();
            return await _database!.Table<TranslationCache>()
                                   .Where(c => c.PoiId == poiId && c.LanguageCode == langCode)
                                   .FirstOrDefaultAsync();
        }

        public async Task SaveCacheAsync(TranslationCache cache)
        {
            await Init();
            await _database!.InsertAsync(cache);
        }

        public async Task SendAnalyticsAsync(ListeningLog log)
        {
            try
            {
                var json = JsonSerializer.Serialize(log);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                // Dùng hàm CreateHttpClient an toàn vừa tạo
                using var client = CreateHttpClient();
                var response = await client.PostAsync(AnalyticsApiUrl, content);

                if (response.IsSuccessStatusCode)
                {
                    System.Diagnostics.Debug.WriteLine($"✅ THÀNH CÔNG: Đã lưu {log.PoiId} vào Database!");
                }
                else
                {
                    string errorDetail = await response.Content.ReadAsStringAsync();
                    System.Diagnostics.Debug.WriteLine($"❌ API TỪ CHỐI (Mã {response.StatusCode}): {errorDetail}");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ MẤT KẾT NỐI MẠNG HOẶC LỖI: {ex.Message}");
            }
        }

        public async Task ReloadAsync()
        {
            _database = null;
            await Init();
        }

        public async Task ResetDatabaseAsync()
        {
            if (_database != null)
            {
                await _database.CloseAsync();
                _database = null;
            }

            var dbPath = GetDbPath();

            if (File.Exists(dbPath))
            {
                File.Delete(dbPath);
                System.Diagnostics.Debug.WriteLine("🗑️ Đã xóa database local.");
            }
        }

        public async Task ResetAndReloadAsync()
        {
            await ResetDatabaseAsync();
            await ReloadAsync();
        }
    }
}