using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Globalization;
using System.Linq;
using Microsoft.Maui.Controls.Maps;
using Microsoft.Maui.Maps;
using Microsoft.Maui.Graphics;
using VinhKhanhTourGuide.Models;
using VinhKhanhTourGuide.Services;
using VinhKhanhTourGuide.Data;
using Microsoft.Maui.Devices.Sensors;
using System.Diagnostics;

namespace VinhKhanhTourGuide.Views
{
    public partial class MapPage : ContentPage
    {
        private readonly TtsService _ttsService;
        private readonly TranslationService _translationService;
        private readonly AppDbContext _dbContext;
        private readonly GeofenceService _geofenceService;

        private List<Poi> _poiList = new();
        private bool _isSpeaking = false;
        private Stopwatch _listenTimer = new Stopwatch();
        private Location _currentLocation;
        private Poi _currentListeningPoi;

        public MapPage(TtsService ttsService, TranslationService translationService, AppDbContext dbContext, GeofenceService geofenceService)
        {
            InitializeComponent();
            _ttsService = ttsService;
            _translationService = translationService;
            _dbContext = dbContext;
            _geofenceService = geofenceService;

            // Đăng ký lắng nghe sự kiện từ Service
            _geofenceService.PoiDetected += OnPoiDetected;
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
                // 1. Kiểm tra và xin quyền vị trí
                var status = await Permissions.CheckStatusAsync<Permissions.LocationWhenInUse>();
                if (status != PermissionStatus.Granted)
                {
                    status = await Permissions.RequestAsync<Permissions.LocationWhenInUse>();
                }

                if (status != PermissionStatus.Granted)
                {
                    GeofenceStatusLabel.Text = "Lỗi: Vui lòng cấp quyền GPS!";
                    return;
                }

                GeofenceStatusLabel.Text = "Đang kết nối API và tải dữ liệu...";

                // 2. Kéo data từ SQL Server (API) hoặc lấy từ SQLite local
                _poiList = await _dbContext.GetPoisAsync();

                await DisplayAlert("Hệ thống", $"Đã tải được {_poiList.Count} địa điểm từ máy chủ.", "OK");

                if (_poiList == null || _poiList.Count == 0)
                {
                    GeofenceStatusLabel.Text = "Không có dữ liệu quán ăn.";
                    return;
                }

                // =========================================================
                // BƯỚC BỔ SUNG: LẤY VỊ TRÍ HIỆN TẠI VÀ SẮP XẾP QUÁN GẦN NHẤT
                // =========================================================
                try
                {
                    GeofenceStatusLabel.Text = "Đang định vị để sắp xếp quán ăn...";

                    // Lấy vị trí hiện tại của user
                    var userLocation = await Geolocation.Default.GetLastKnownLocationAsync()
                                    ?? await Geolocation.Default.GetLocationAsync(new GeolocationRequest(GeolocationAccuracy.Medium, TimeSpan.FromSeconds(3)));

                    if (userLocation != null)
                    {
                        // Sắp xếp danh sách _poiList theo khoảng cách tăng dần
                        _poiList = _poiList.OrderBy(p =>
                            Location.CalculateDistance(userLocation, new Location(p.Latitude, p.Longitude), DistanceUnits.Kilometers)
                        ).ToList();
                    }
                }
                catch (Exception)
                {
                    // Nếu mất sóng GPS hoặc lỗi, cứ bỏ qua và hiển thị list mặc định, không làm crash app
                }
                // =========================================================
                // 3. Hiển thị Pins (Cờ) lên bản đồ
                VinhKhanhMap.Pins.Clear();
                foreach (var poi in _poiList)
                {
                    var pin = new Pin
                    {
                        Label = poi.Name,
                        Address = "Khu Ẩm thực Vĩnh Khánh",
                        Type = PinType.Place,
                        Location = new Location(poi.Latitude, poi.Longitude)
                    };
                    VinhKhanhMap.Pins.Add(pin);
                }

                // 4. Di chuyển camera bản đồ đến quán đầu tiên
                var centerLocation = new Location(_poiList[0].Latitude, _poiList[0].Longitude);
                VinhKhanhMap.MoveToRegion(MapSpan.FromCenterAndRadius(centerLocation, Distance.FromKilometers(0.5)));

                // 5. Đổ dữ liệu vào CollectionView dưới đáy màn hình
                EateryList.ItemsSource = _poiList;

                GeofenceStatusLabel.Text = $"Radar đang quét quanh {_poiList.Count} quán...";

                // Kích hoạt Radar từ tầng Service
                _geofenceService.StartRadar(_poiList);
            }
            catch (Exception ex)
            {
                await DisplayAlert("Lỗi", "Không thể hiển thị bản đồ: " + ex.Message, "Đóng");
            }
        }

        private async void OnEaterySelected(object sender, SelectionChangedEventArgs e)
        {
            if (e.CurrentSelection.Count == 0) return;
            var selectedPoi = e.CurrentSelection[0] as Poi;
            ((CollectionView)sender).SelectedItem = null;

            await Navigation.PushAsync(new EateryDetailPage(selectedPoi, _translationService, _ttsService, _dbContext));
        }

        // HÀM NÀY SẼ CHẠY KHI GEOFENCESERVICE PHÁT HIỆN NGƯỜI DÙNG VÀO VÙNG QUÁN ĂN
        private async void OnPoiDetected(object sender, (Poi targetPoi, Location userLocation) e)
        {
            if (_isSpeaking) return;

            _isSpeaking = true;
            _geofenceService.SetProcessingState(true); // Khóa radar lại để tránh spam quét

            var targetPoi = e.targetPoi;
            _currentLocation = e.userLocation;
            _currentListeningPoi = targetPoi;

            // Cập nhật UI (Vẽ vòng tròn đỏ, hiện nút Stop)
            MainThread.BeginInvokeOnMainThread(() =>
            {
                StopAudioBtn.IsVisible = true;
                VinhKhanhMap.MapElements.Clear();
                VinhKhanhMap.MapElements.Add(new Circle
                {
                    Center = new Location(targetPoi.Latitude, targetPoi.Longitude),
                    Radius = new Distance(targetPoi.GeofenceRadius),
                    StrokeColor = Colors.Red,
                    StrokeWidth = 5,
                    FillColor = Color.FromArgb("#44FF0000")
                });
                GeofenceStatusLabel.Text = $"📍 Đang thuyết minh: {targetPoi.Name}...";
            });

            // Xử lý dịch thuật (Gemini)
            string lang = CultureInfo.CurrentUICulture.Name;
            var cache = await _dbContext.GetCacheAsync(targetPoi.Id, lang);
            string speechText = cache?.TranslatedText;

            if (string.IsNullOrEmpty(speechText))
            {
                speechText = await _translationService.TranslateAsync(targetPoi.Description_VN, lang);
                await _dbContext.SaveCacheAsync(new TranslationCache { PoiId = targetPoi.Id, LanguageCode = lang, TranslatedText = speechText, CreatedAt = DateTime.Now });
            }

            _listenTimer.Restart(); // BẮT ĐẦU BẤM GIỜ

            // Đọc âm thanh (TTS)
            await _ttsService.SpeakAsync(speechText);

            SendAnalyticsData();

            // Kết thúc thuyết minh, dọn dẹp UI
            MainThread.BeginInvokeOnMainThread(() =>
            {
                StopAudioBtn.IsVisible = false;
                VinhKhanhMap.MapElements.Clear();
                GeofenceStatusLabel.Text = "Radar đang tiếp tục quét...";
            });

            _isSpeaking = false;
            _geofenceService.SetProcessingState(false); // Mở khóa radar cho quét tiếp
        }

        private void OnStopAudioClicked(object sender, EventArgs e)
        {
            _ttsService.Stop();

            SendAnalyticsData();

            StopAudioBtn.IsVisible = false;
            VinhKhanhMap.MapElements.Clear();
            GeofenceStatusLabel.Text = "Đã dừng thuyết minh.";
            _isSpeaking = false;

            // Đảm bảo mở khóa radar nếu người dùng chủ động bấm Stop
            _geofenceService.SetProcessingState(false);
        }

        private void SendAnalyticsData()
        {
            if (_currentListeningPoi == null || !_listenTimer.IsRunning) return;

            _listenTimer.Stop();

            // Lấy hoặc tạo Mã thiết bị ẩn danh (Guid)
            string sessionId = Preferences.Default.Get("SessionId", "");
            if (string.IsNullOrEmpty(sessionId))
            {
                sessionId = Guid.NewGuid().ToString();
                Preferences.Default.Set("SessionId", sessionId);
            }

            // Đóng gói dữ liệu
            var log = new ListeningLog
            {
                PoiId = _currentListeningPoi.Id,
                AnonymousSessionId = sessionId,
                DurationSeconds = Math.Round(_listenTimer.Elapsed.TotalSeconds, 1),
                Latitude = _currentLocation.Latitude,
                Longitude = _currentLocation.Longitude
            };

            // Gửi ngầm (không dùng await để tránh làm đơ giao diện)
            _ = _dbContext.SendAnalyticsAsync(log);

            _currentListeningPoi = null; // Reset lại
        }
    }
}