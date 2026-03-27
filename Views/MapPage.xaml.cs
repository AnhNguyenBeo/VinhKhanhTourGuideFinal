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

namespace VinhKhanhTourGuide.Views
{
    public partial class MapPage : ContentPage
    {
        private readonly TtsService _ttsService;
        private readonly TranslationService _translationService;
        private readonly AppDbContext _dbContext;

        private IDispatcherTimer _radarTimer;
        private List<Poi> _poiList = new();
        private Dictionary<string, DateTime> _spokenPoisDict = new();
        private readonly int _cooldownMinutes = 5;
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

                // 2. Kích hoạt hàm Init() trong AppDbContext để kéo data từ SQL Server (API)
                // Sau đó lấy danh sách quán từ SQLite local
                _poiList = await _dbContext.GetPoisAsync();

                // BƯỚC KIỂM TRA QUAN TRỌNG: Hiện thông báo số lượng quán tải được
                await DisplayAlert("Hệ thống", $"Đã tải được {_poiList.Count} địa điểm từ máy chủ.", "OK");

                if (_poiList == null || _poiList.Count == 0)
                {
                    GeofenceStatusLabel.Text = "Không có dữ liệu quán ăn.";
                    return;
                }

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
                StartGeofenceRadar();
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

        private void StartGeofenceRadar()
        {
            if (_radarTimer != null) return;

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
                // Lấy vị trí thực tế của người dùng
                var userLocation = await Geolocation.Default.GetLocationAsync(new GeolocationRequest(GeolocationAccuracy.High, TimeSpan.FromSeconds(3)));
                if (userLocation == null) return;

                // Tìm các quán trong bán kính Geofence
                var poisInRange = new List<(Poi poi, double distance)>();
                foreach (var poi in _poiList)
                {
                    double dist = Location.CalculateDistance(userLocation, new Location(poi.Latitude, poi.Longitude), DistanceUnits.Kilometers) * 1000;
                    if (dist <= poi.GeofenceRadius)
                    {
                        poisInRange.Add((poi, dist));
                    }
                }

                if (poisInRange.Count > 0)
                {
                    // Logic sắp xếp: Ưu tiên Priority (thấp nhất là 1), sau đó mới đến khoảng cách gần nhất
                    var targetPoi = poisInRange.OrderBy(p => p.poi.Priority).ThenBy(p => p.distance).First().poi;

                    // Kiểm tra Cooldown (Chống đọc lặp lại quá nhanh)
                    if (_spokenPoisDict.TryGetValue(targetPoi.Id, out DateTime lastTime))
                    {
                        if ((DateTime.Now - lastTime).TotalMinutes < _cooldownMinutes) return;
                    }

                    _isSpeaking = true;
                    _spokenPoisDict[targetPoi.Id] = DateTime.Now;

                    // Cập nhật giao diện (Nút Stop và Vòng tròn highlight)
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

                    // Xử lý ngôn ngữ và TTS
                    string lang = CultureInfo.CurrentUICulture.Name;
                    var cache = await _dbContext.GetCacheAsync(targetPoi.Id, lang);
                    string speechText = cache?.TranslatedText;

                    if (string.IsNullOrEmpty(speechText))
                    {
                        speechText = await _translationService.TranslateAsync(targetPoi.Description_VN, lang);
                        await _dbContext.SaveCacheAsync(new TranslationCache { PoiId = targetPoi.Id, LanguageCode = lang, TranslatedText = speechText, CreatedAt = DateTime.Now });
                    }

                    await _ttsService.SpeakAsync(speechText);

                    // Kết thúc thuyết minh
                    MainThread.BeginInvokeOnMainThread(() =>
                    {
                        StopAudioBtn.IsVisible = false;
                        VinhKhanhMap.MapElements.Clear();
                        GeofenceStatusLabel.Text = "Radar đang tiếp tục quét...";
                    });
                    _isSpeaking = false;
                }
            }
            catch (Exception) { /* Bỏ qua lỗi ngầm để tránh văng app khi mất GPS tạm thời */ }
        }

        private void OnStopAudioClicked(object sender, EventArgs e)
        {
            _ttsService.Stop();
            StopAudioBtn.IsVisible = false;
            VinhKhanhMap.MapElements.Clear();
            GeofenceStatusLabel.Text = "Đã dừng thuyết minh.";
            _isSpeaking = false;
        }
    }
}