using VinhKhanhTourGuide.Models;
using VinhKhanhTourGuide.Services;
using VinhKhanhTourGuide.Data;
using System.Globalization;
using System.Net.Http;

namespace VinhKhanhTourGuide.Views;

public partial class EateryDetailPage : ContentPage
{
    private Poi _poi;
    private TranslationService _translationService;
    private TtsService _ttsService;
    private AppDbContext _dbContext;
    private VisitorActivityService _visitorActivityService;

    public EateryDetailPage(Poi poi, TranslationService translationService, TtsService ttsService, AppDbContext dbContext, VisitorActivityService visitorActivityService)
    {
        InitializeComponent();

        _poi = poi;
        _translationService = translationService;
        _ttsService = ttsService;
        _dbContext = dbContext;
        _visitorActivityService = visitorActivityService;

        BindingContext = _poi;
    }

    private async void OnBackClicked(object sender, EventArgs e)
    {
        await Navigation.PopAsync();
    }

    private void OnStopAudioClicked(object sender, EventArgs e)
    {
        _ttsService.Stop();
        _visitorActivityService.SetListeningState(false);
        StatusLabel.Text = "Đã dừng phát âm thanh.";
    }

    private async void OnPlayAudioClicked(object sender, EventArgs e)
    {
        StatusLabel.Text = "Đang chuẩn bị...";
        PlayAudioButton.IsVisible = false;
        StopAudioButton.IsVisible = true;

        try
        {
            _visitorActivityService.SetListeningState(true, _poi.Id);
            string deviceLang = CultureInfo.CurrentUICulture.Name;
            bool needsTranslation = !deviceLang.StartsWith("vi", StringComparison.OrdinalIgnoreCase);

            if (needsTranslation)
            {
                StatusLabel.Text = $"Đang lấy bản dịch {deviceLang}...";
            }

            var (audioText, success) = await _translationService.ResolvePoiNarrationAsync(_poi, deviceLang);

            if (needsTranslation && !success)
            {
                StatusLabel.Text = "⚠️ Không dịch được, sẽ đọc tiếng Việt...";
                await Task.Delay(1500);
            }

            if (string.IsNullOrWhiteSpace(audioText))
            {
                audioText = _poi.Description_VN;
            }

            StatusLabel.Text = "🔊 Đang phát âm thanh...";
            await _ttsService.SpeakAsync(audioText);

            StatusLabel.Text = "Đã phát xong.";
        }
        catch (HttpRequestException)
        {
            StatusLabel.Text = "Lỗi kết nối, bạn kiểm tra lại mạng nhé!";
        }
        catch (TaskCanceledException)
        {
            StatusLabel.Text = "Đã dừng."; 
        }
        catch (Exception ex) when (
            ex.Message.Contains("network", StringComparison.OrdinalIgnoreCase) ||
            ex.Message.Contains("connect", StringComparison.OrdinalIgnoreCase))
        {
            StatusLabel.Text = "Lỗi kết nối, bạn kiểm tra lại mạng nhé!";
        }
        catch (Exception)
        {
            StatusLabel.Text = "Có lỗi xảy ra, bạn thử lại sau nhé!";
        }
        finally
        {
            _visitorActivityService.SetListeningState(false);
            PlayAudioButton.IsVisible = true;
            StopAudioButton.IsVisible = false;
        }
    }

    private async void OnNavigateClicked(object sender, EventArgs e)
    {
        try
        {
            var location = new Location(_poi.Latitude, _poi.Longitude);
            var options = new MapLaunchOptions
            {
                Name = _poi.Name,
                NavigationMode = NavigationMode.Walking
            };

            await Microsoft.Maui.ApplicationModel.Map.Default.OpenAsync(location, options);
        }
        catch (Exception)
        {
            await DisplayAlert("Không mở được bản đồ", "Bạn kiểm tra lại kết nối hoặc thử lại sau nhé!", "OK");
        }
    }
}
