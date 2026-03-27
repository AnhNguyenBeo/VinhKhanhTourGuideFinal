using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Globalization;
using System.Linq; // Bắt buộc phải có để dùng thuật toán sắp xếp độ ưu tiên OrderBy
using Microsoft.Maui.Controls.Maps;
using Microsoft.Maui.Maps;
using Microsoft.Maui.Graphics; // Bắt buộc phải có để dùng màu sắc (Colors.Red) vẽ vòng tròn
using VinhKhanhTourGuide.Models;
using VinhKhanhTourGuide.Services;
using VinhKhanhTourGuide.Data;
using Microsoft.Maui.Devices.Sensors;

namespace VinhKhanhTourGuide.Views
{
    public partial class MapPage : ContentPage
    {
        private readonly TtsService _ttsService;
        private readonly TranslationService _translationService;
        private readonly AppDbContext _dbContext;

        private IDispatcherTimer _radarTimer;
        private List<Poi> _poiList = new();
        private HashSet<string> _spokenPois = new();
        private bool _isSpeaking = false;

        public MapPage(TtsService ttsService, TranslationService translationService, AppDbContext dbContext)
        {
            InitializeComponent();
            _ttsService = ttsService;
            _translationService = translationService;
            _dbContext = dbContext;
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            await SetupMainUiAsync();
        }

        private async Task SetupMainUiAsync()
        {
            try
            {
                // 1. Xin quyền GPS
                var status = await Permissions.CheckStatusAsync<Permissions.LocationWhenInUse>();
                if (status != PermissionStatus.Granted)
                {
                    status = await Permissions.RequestAsync<Permissions.LocationWhenInUse>();
                }

                if (status != PermissionStatus.Granted)
                {
                    GeofenceStatusLabel.Text = "Lỗi GPS: Xin cấp quyền vị trí!";
                    return;
                }

                GeofenceStatusLabel.Text = "Đang tải dữ liệu quán ăn...";

                // 2. Lấy danh sách quán từ SQLite
                _poiList = await _dbContext.GetPoisAsync();

                // 3. CẮM CỜ LÊN BẢN ĐỒ NGẦM
                foreach (var poi in _poiList)
                {
                    var pin = new Pin
                    {
                        Label = poi.Name,
                        Address = "Khu Ẩm thực Quận 4",
                        Type = PinType.Place,
                        Location = new Location(poi.Latitude, poi.Longitude)
                    };
                    VinhKhanhMap.Pins.Add(pin);
                }

                if (_poiList.Count > 0)
                {
                    var centerLocation = new Location(_poiList[0].Latitude, _poiList[0].Longitude);
                    var mapSpan = MapSpan.FromCenterAndRadius(centerLocation, Distance.FromKilometers(0.8));
                    VinhKhanhMap.MoveToRegion(mapSpan);
                }

                // 4. NẠP DỮ LIỆU VÀO DANH SÁCH TRÊN MÀN HÌNH
                EateryList.ItemsSource = _poiList;

                GeofenceStatusLabel.Text = $"Radar đang quét vị trí quanh {_poiList.Count} quán...";
                StartGeofenceRadar();
            }
            catch (Exception ex)
            {
                GeofenceStatusLabel.Text = $"Lỗi giao diện: {ex.Message}";
            }
        }

        // ---------- SỰ KIỆN KHI NGƯỜI DÙNG CHỌN MỘT QUÁN TRÊN DANH SÁCH ----------
        private async void OnEaterySelected(object sender, SelectionChangedEventArgs e)
        {
            if (e.CurrentSelection.Count == 0) return;

            var selectedPoi = e.CurrentSelection[0] as Poi;
            ((CollectionView)sender).SelectedItem = null;

            await Navigation.PushAsync(new EateryDetailPage(selectedPoi, _translationService, _ttsService, _dbContext));
        }

        // ---------- CÁC HÀM GEOFENCING RADAR ----------
        private Dictionary<string, DateTime> _spokenPoisDict = new();
        private readonly int _cooldownMinutes = 5;

        private void StartGeofenceRadar()
        {
            _radarTimer = Dispatcher.CreateTimer();
            _radarTimer.Interval = TimeSpan.FromSeconds(5);
            _radarTimer.Tick += async (s, e) => await CheckGeofenceAsync();
            _radarTimer.Start();
        }

        private async Task CheckGeofenceAsync()
        {
            if (_isSpeaking) return;

            try
            {
                var request = new GeolocationRequest(GeolocationAccuracy.High, TimeSpan.FromSeconds(2));
                var userLocation = await Geolocation.Default.GetLocationAsync(request);
                if (userLocation == null) return;

                var poisInRange = new List<(Poi poi, double distance)>();
                foreach (var poi in _poiList)
                {
                    double distMeters = Location.CalculateDistance(userLocation, new Location(poi.Latitude, poi.Longitude), DistanceUnits.Kilometers) * 1000;
                    if (distMeters <= poi.GeofenceRadius)
                    {
                        poisInRange.Add((poi, distMeters));
                    }
                }

                if (poisInRange.Count > 0)
                {
                    // ƯU TIÊN: Xếp theo Priority (số nhỏ hơn ưu tiên trước), sau đó xét khoảng cách
                    var targetPoi = poisInRange.OrderBy(p => p.poi.Priority).ThenBy(p => p.distance).First().poi;

                    // COOLDOWN (Chống spam)
                    if (_spokenPoisDict.TryGetValue(targetPoi.Id, out DateTime lastSpokenTime))
                    {
                        if ((DateTime.Now - lastSpokenTime).TotalMinutes < _cooldownMinutes)
                        {
                            return;
                        }
                    }

                    // BẮT ĐẦU ĐỌC
                    _isSpeaking = true;
                    _spokenPoisDict[targetPoi.Id] = DateTime.Now;

                    // HIỆN NÚT STOP & VẼ VÒNG TRÒN ĐỎ HIGHLIGHT QUÁN
                    MainThread.BeginInvokeOnMainThread(() =>
                    {
                        StopAudioBtn.IsVisible = true;

                        // Xóa các vòng highlight cũ
                        VinhKhanhMap.MapElements.Clear();

                        // Vẽ vòng tròn highlight mới màu đỏ mờ đè lên quán
                        var highlightCircle = new Circle
                        {
                            Center = new Location(targetPoi.Latitude, targetPoi.Longitude),
                            Radius = new Distance(targetPoi.GeofenceRadius),
                            StrokeColor = Colors.Red,
                            StrokeWidth = 4,
                            FillColor = Color.FromArgb("#44FF0000") // Đỏ trong suốt
                        };
                        VinhKhanhMap.MapElements.Add(highlightCircle);
                    });

                    string targetLang = CultureInfo.CurrentUICulture.Name;

                    GeofenceStatusLabel.Text = $"📍 Đang xử lý: {targetPoi.Name}...";

                    var cache = await _dbContext.GetCacheAsync(targetPoi.Id, targetLang);
                    string audioText = cache?.TranslatedText;

                    if (string.IsNullOrEmpty(audioText))
                    {
                        audioText = await _translationService.TranslateAsync(targetPoi.Description_VN, targetLang);
                        await _dbContext.SaveCacheAsync(new TranslationCache
                        {
                            PoiId = targetPoi.Id,
                            LanguageCode = targetLang,
                            TranslatedText = audioText,
                            CreatedAt = DateTime.Now
                        });
                    }

                    GeofenceStatusLabel.Text = $"🔊 Đang thuyết minh: {targetPoi.Name}...";

                    await _ttsService.SpeakAsync(audioText); // Chờ AI đọc xong

                    // ĐỌC XONG: Tắt nút Stop và Xóa vòng tròn đỏ
                    MainThread.BeginInvokeOnMainThread(() =>
                    {
                        StopAudioBtn.IsVisible = false;
                        VinhKhanhMap.MapElements.Clear(); // Xóa vòng highlight
                        GeofenceStatusLabel.Text = "Radar đang tiếp tục quét...";
                    });
                    _isSpeaking = false;
                }
            }
            catch (Exception) { /* Bỏ qua lỗi ngầm */ }
        }

        // SỰ KIỆN KHI KHÁCH BẤM NÚT DỪNG
        private void OnStopAudioClicked(object sender, EventArgs e)
        {
            _ttsService.Stop();

            // Tắt nút Stop và Xóa vòng tròn đỏ ngay lập tức
            StopAudioBtn.IsVisible = false;
            VinhKhanhMap.MapElements.Clear();

            GeofenceStatusLabel.Text = "Đã dừng thuyết minh.";
            _isSpeaking = false;
        }
    }
}