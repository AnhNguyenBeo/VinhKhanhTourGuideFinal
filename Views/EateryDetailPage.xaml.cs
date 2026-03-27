using VinhKhanhTourGuide.Models;
using VinhKhanhTourGuide.Services;
using VinhKhanhTourGuide.Data;
using System.Globalization;

namespace VinhKhanhTourGuide.Views;

public partial class EateryDetailPage : ContentPage
{
    private Poi _poi;
    private TranslationService _translationService;
    private TtsService _ttsService;
    private AppDbContext _dbContext;

    public EateryDetailPage(Poi poi, TranslationService translationService, TtsService ttsService, AppDbContext dbContext)
    {
        InitializeComponent();

        _poi = poi;
        _translationService = translationService;
        _ttsService = ttsService;
        _dbContext = dbContext;

        BindingContext = _poi;
    }

    private async void OnPlayAudioClicked(object sender, EventArgs e)
    {
        StatusLabel.Text = "Đang chuẩn bị...";
        PlayAudioButton.IsEnabled = false;

        try
        {
            string deviceLang = CultureInfo.CurrentUICulture.Name;

            StatusLabel.Text = $"Đang dịch sang ngôn ngữ máy ({deviceLang})...";

            var cache = await _dbContext.GetCacheAsync(_poi.Id, deviceLang);
            string audioText = cache?.TranslatedText;

            if (string.IsNullOrEmpty(audioText))
            {
                audioText = await _translationService.TranslateAsync(_poi.Description_VN, deviceLang);
                await _dbContext.SaveCacheAsync(new TranslationCache
                {
                    PoiId = _poi.Id,
                    LanguageCode = deviceLang,
                    TranslatedText = audioText,
                    CreatedAt = DateTime.Now
                });
            }

            StatusLabel.Text = "🔊 Đang phát âm thanh...";
            await _ttsService.SpeakAsync(audioText);

            StatusLabel.Text = "Đã phát xong.";
        }
        catch (Exception ex)
        {
            StatusLabel.Text = "Lỗi: " + ex.Message;
        }
        finally
        {
            PlayAudioButton.IsEnabled = true;
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
        catch (Exception ex)
        {
            await DisplayAlert("Lỗi", "Không thể mở bản đồ: " + ex.Message, "OK");
        }
    }
}