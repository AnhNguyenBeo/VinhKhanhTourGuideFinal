using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using System.Diagnostics;
using Microsoft.Maui.ApplicationModel; // Thêm thư viện này để dùng AppInfo
using Microsoft.Maui.Controls.Maps;
using Microsoft.Maui.Devices.Sensors;
using Microsoft.Maui.Graphics;
using Microsoft.Maui.Maps;
using VinhKhanhTourGuide.Data;
using VinhKhanhTourGuide.Models;
using VinhKhanhTourGuide.Services;

namespace VinhKhanhTourGuide.Views
{
    public partial class MapPage : ContentPage
    {
        private readonly TtsService _ttsService;
        private readonly TranslationService _translationService;
        private readonly AppDbContext _dbContext;
        private readonly GeofenceService _geofenceService;
        private readonly PremiumService _premiumService;
        private readonly PurchaseService _purchaseService;
        private readonly VisitorActivityService _visitorActivityService;

        private List<Poi> _poiList = new();
        private bool _isSpeaking = false;
        private readonly Stopwatch _listenTimer = new();

        private Location? _currentLocation;
        private Poi? _currentListeningPoi;

        private CancellationTokenSource? _waveCts;

        private double _bottomSheetStartTranslationY = 320;
        private const double BottomSheetExpandedY = 0;
        private const double BottomSheetCollapsedY = 320;
        private const double BottomSheetVisibleCollapsedHeight = 300;

        public MapPage(
                 TtsService ttsService,
                 TranslationService translationService,
                 AppDbContext dbContext,
                 GeofenceService geofenceService,
                 PremiumService premiumService,
                 PurchaseService purchaseService,
                 VisitorActivityService visitorActivityService)
        {
            InitializeComponent();

            _ttsService = ttsService;
            _translationService = translationService;
            _dbContext = dbContext;
            _geofenceService = geofenceService;
            _premiumService = premiumService;
            _purchaseService = purchaseService;
            _visitorActivityService = visitorActivityService;

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
                // 1. Kiểm tra trạng thái quyền hiện tại
                var status = await Permissions.CheckStatusAsync<Permissions.LocationWhenInUse>();

                // 2. Nếu chưa có quyền thì bật popup của hệ điều hành lên để xin
                if (status != PermissionStatus.Granted)
                {
                    status = await Permissions.RequestAsync<Permissions.LocationWhenInUse>();
                }

                // 3. Nếu người dùng vẫn nhất quyết từ chối
                if (status != PermissionStatus.Granted)
                {
                    // Hiện hộp thoại giải thích và dẫn họ vào Cài đặt
                    bool openSettings = await DisplayAlert(
                        "Thiếu quyền Vị trí",
                        "App cần quyền định vị GPS để quét radar và tìm các quán ăn gần bạn nhất. Bản đồ sẽ không hoạt động nếu thiếu quyền này. Bạn có muốn mở Cài đặt để cấp quyền không?",
                        "Mở Cài đặt",
                        "Để sau");

                    if (openSettings)
                    {
                        // Gọi thẳng API của MAUI để mở màn hình Settings của điện thoại
                        AppInfo.ShowSettingsUI();
                    }

                    GeofenceStatusLabel.Text = "Bản đồ đang tạm khóa vì thiếu quyền GPS.";
                    LocationCountLabel.Text = "Chưa có dữ liệu";

                    // Dừng tại đây, không tải map nữa
                    return;
                }

                GeofenceStatusLabel.Text = "Đang kết nối API và tải dữ liệu...";

                _poiList = await _dbContext.GetPoisAsync();

                LocationCountLabel.Text = $"Tìm thấy {_poiList.Count} quán ăn";
                PremiumBanner.IsVisible = !_premiumService.IsPremium();
                NowPlayingCard.IsVisible = false;
                StopAudioBtn.IsVisible = false;
                StatusPill.IsVisible = true;

                StopWaveAnimation();

                // Map chỉ cao đến mép trên của danh sách khi chưa vuốt
                MapContainer.Margin = new Thickness(0, 0, 0, BottomSheetVisibleCollapsedHeight);

                // Bottom sheet ban đầu ở trạng thái thu gọn
                BottomSheet.TranslationY = BottomSheetCollapsedY;

                if (_poiList.Count == 0)
                {
                    GeofenceStatusLabel.Text = "Không có dữ liệu quán ăn.";
                    return;
                }

                try
                {
                    GeofenceStatusLabel.Text = "Đang định vị để sắp xếp quán ăn...";

                    var userLocation = await Geolocation.Default.GetLastKnownLocationAsync()
                                      ?? await Geolocation.Default.GetLocationAsync(
                                          new GeolocationRequest(
                                              GeolocationAccuracy.Medium,
                                              TimeSpan.FromSeconds(3)));

                    if (userLocation != null)
                    {
                        foreach (var poi in _poiList)
                        {
                            double distanceKm = Location.CalculateDistance(
                                userLocation,
                                new Location(poi.Latitude, poi.Longitude),
                                DistanceUnits.Kilometers);

                            poi.Distance = distanceKm * 1000; // mét
                        }

                        // sort theo khoảng cách gần nhất
                        _poiList = _poiList
                            .OrderBy(p => p.Distance)
                            .ToList();
                    }
                }
                catch
                {
                    // bỏ qua lỗi GPS tạm thời
                }

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

                var centerLocation = new Location(_poiList[0].Latitude, _poiList[0].Longitude);
                VinhKhanhMap.MoveToRegion(
                    MapSpan.FromCenterAndRadius(centerLocation, Distance.FromKilometers(0.5)));

                EateryList.ItemsSource = _poiList;

                GeofenceStatusLabel.Text = $"Radar đang quét {_poiList.Count} quán...";

                // Warm cache dịch nền để lần bấm nghe đầu tiên phản hồi nhanh hơn.
                _ = Task.Run(() => _translationService.PrefetchNarrationsAsync(
                    _poiList,
                    CultureInfo.CurrentUICulture.Name,
                    maxCount: 8));

                _visitorActivityService.StartTracking(_poiList);
                await _visitorActivityService.SendImmediateHeartbeatAsync("map_ready");
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

            if (selectedPoi == null) return;

            await Navigation.PushAsync(
                new EateryDetailPage(selectedPoi, _translationService, _ttsService, _dbContext, _visitorActivityService));
        }

        private async void OnPoiDetected(object? sender, (Poi targetPoi, Location userLocation) e)
        {
            if (_isSpeaking) return;

            _isSpeaking = true;
            _geofenceService.SetProcessingState(true);

            var targetPoi = e.targetPoi;
            _currentLocation = e.userLocation;
            _currentListeningPoi = targetPoi;
            _visitorActivityService.SetListeningState(true, targetPoi.Id);

            MainThread.BeginInvokeOnMainThread(() =>
            {
                StopAudioBtn.IsVisible = true;
                NowPlayingCard.IsVisible = true;
                StatusPill.IsVisible = false;

                NowPlayingNameLabel.Text = targetPoi.Name ?? "";
                StartWaveAnimation();

                VinhKhanhMap.MapElements.Clear();
                VinhKhanhMap.MapElements.Add(new Circle
                {
                    Center = new Location(targetPoi.Latitude, targetPoi.Longitude),
                    Radius = new Distance(targetPoi.GeofenceRadius),
                    StrokeColor = Colors.Red,
                    StrokeWidth = 5,
                    FillColor = Color.FromArgb("#44FF0000")
                });
            });

            string lang = CultureInfo.CurrentUICulture.Name;
            var (speechText, translationSuccess) = await _translationService.ResolvePoiNarrationAsync(targetPoi, lang);

            bool needsTranslation = !lang.StartsWith("vi", StringComparison.OrdinalIgnoreCase);
            if (needsTranslation && !translationSuccess)
            {
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    GeofenceStatusLabel.Text = "Không dịch được thuyết minh cho ngôn ngữ hiện tại.";
                    StopAudioBtn.IsVisible = false;
                    NowPlayingCard.IsVisible = false;
                    StatusPill.IsVisible = true;
                    VinhKhanhMap.MapElements.Clear();
                    StopWaveAnimation();
                });

                _isSpeaking = false;
                _visitorActivityService.SetListeningState(false);
                _geofenceService.SetProcessingState(false);
                return;
            }

            if (string.IsNullOrWhiteSpace(speechText))
            {
                speechText = targetPoi.Description_VN;
            }

            string speechLanguage = needsTranslation ? lang : "vi";

            _listenTimer.Restart();

            await _ttsService.SpeakAsync(speechText, speechLanguage);

            SendAnalyticsData();

            MainThread.BeginInvokeOnMainThread(() =>
            {
                StopAudioBtn.IsVisible = false;
                NowPlayingCard.IsVisible = false;
                StatusPill.IsVisible = true;

                VinhKhanhMap.MapElements.Clear();
                GeofenceStatusLabel.Text = "Quét điểm gần bạn...";

                StopWaveAnimation();
            });

            _isSpeaking = false;
            _visitorActivityService.SetListeningState(false);
            _geofenceService.SetProcessingState(false);
        }

        private void OnStopAudioClicked(object sender, EventArgs e)
        {
            _ttsService.Stop();

            SendAnalyticsData();

            StopAudioBtn.IsVisible = false;
            NowPlayingCard.IsVisible = false;
            StatusPill.IsVisible = true;

            VinhKhanhMap.MapElements.Clear();
            GeofenceStatusLabel.Text = "Quét điểm gần bạn...";
            _isSpeaking = false;

            _visitorActivityService.SetListeningState(false);
            _geofenceService.SetProcessingState(false);
            StopWaveAnimation();
        }

        private void OnStopAudioTapped(object sender, TappedEventArgs e)
        {
            OnStopAudioClicked(sender, EventArgs.Empty);
        }

        private void SendAnalyticsData()
        {
            if (_currentListeningPoi == null || _currentLocation == null || !_listenTimer.IsRunning)
                return;

            _listenTimer.Stop();

            string sessionId = _dbContext.GetOrCreateSessionId();

            var log = new ListeningLog
            {
                PoiId = _currentListeningPoi.Id,
                AnonymousSessionId = sessionId,
                DurationSeconds = Math.Round(_listenTimer.Elapsed.TotalSeconds, 1),
                Latitude = _currentLocation.Latitude,
                Longitude = _currentLocation.Longitude
            };

            _ = _dbContext.SendAnalyticsAsync(log);

            _currentListeningPoi = null;
        }

        private async Task BuyFullPackageAsync()
        {
            bool confirm = await DisplayAlert(
                "Mở khóa toàn bộ khu ẩm thực",
                "Bạn có muốn mua gói thuyết minh tự động cho tất cả quán không?",
                "Mua ngay",
                "Để sau");

            if (!confirm) return;

            GeofenceStatusLabel.Text = "Đang xử lý thanh toán...";

            bool success = await _purchaseService.PurchaseFullPackageAsync();

            if (success)
            {
                await _dbContext.ReloadAsync();
                await SetupMainUiAsync();
                await DisplayAlert("Thành công", "Đã mở khóa toàn bộ quán ăn!", "OK");
            }
            else
            {
                await DisplayAlert("Thất bại", "Thanh toán chưa thành công, vui lòng thử lại.", "OK");
            }
        }

        private async void OnPremiumBannerTapped(object sender, EventArgs e)
        {
            await BuyFullPackageAsync();
        }

        // Bottom sheet drag
        private async void OnBottomSheetPanUpdated(object sender, PanUpdatedEventArgs e)
        {
            switch (e.StatusType)
            {
                case GestureStatus.Started:
                    _bottomSheetStartTranslationY = BottomSheet.TranslationY;
                    break;

                case GestureStatus.Running:
                    double newY = _bottomSheetStartTranslationY + e.TotalY;

                    newY = Math.Max(
                        BottomSheetExpandedY,
                        Math.Min(BottomSheetCollapsedY, newY));

                    BottomSheet.TranslationY = newY;
                    break;

                case GestureStatus.Completed:
                case GestureStatus.Canceled:
                    double middle = (BottomSheetCollapsedY + BottomSheetExpandedY) / 2;

                    double targetY = BottomSheet.TranslationY > middle
                        ? BottomSheetCollapsedY
                        : BottomSheetExpandedY;

                    await BottomSheet.TranslateTo(0, targetY, 180, Easing.SinOut);
                    break;
            }
        }

        // Wave animation
        private void StartWaveAnimation()
        {
            StopWaveAnimation();

            _waveCts = new CancellationTokenSource();
            var token = _waveCts.Token;

            _ = AnimateWaveAsync(Wave1, 0.65, 1.3, 20, token);
            _ = AnimateWaveAsync(Wave2, 0.8, 1.6, 240, token);
            _ = AnimateWaveAsync(Wave3, 0.6, 1.4, 120, token);
            _ = AnimateWaveAsync(Wave4, 0.6, 1.55, 180, token);
        }

        private void StopWaveAnimation()
        {
            if (_waveCts != null)
            {
                _waveCts.Cancel();
                _waveCts.Dispose();
                _waveCts = null;
            }

            ResetWaveBar(Wave1);
            ResetWaveBar(Wave2);
            ResetWaveBar(Wave3);
            ResetWaveBar(Wave4);
        }

        private static void ResetWaveBar(BoxView bar)
        {
            bar.ScaleY = 1;
            bar.Opacity = 1;
        }

        private async Task AnimateWaveAsync(
            VisualElement bar,
            double minScale,
            double maxScale,
            int initialDelayMs,
            CancellationToken token)
        {
            try
            {
                if (initialDelayMs > 0)
                    await Task.Delay(initialDelayMs, token);

                while (!token.IsCancellationRequested)
                {
                    await MainThread.InvokeOnMainThreadAsync(async () =>
                    {
                        await Task.WhenAll(
                            bar.ScaleYTo(maxScale, 220, Easing.SinInOut),
                            bar.FadeTo(0.9, 220, Easing.SinInOut)
                        );

                        await Task.WhenAll(
                            bar.ScaleYTo(minScale, 220, Easing.SinInOut),
                            bar.FadeTo(1.0, 220, Easing.SinInOut)
                        );
                    });

                    await Task.Delay(80, token);
                }
            }
            catch (TaskCanceledException)
            {
            }
        }
    }
}
