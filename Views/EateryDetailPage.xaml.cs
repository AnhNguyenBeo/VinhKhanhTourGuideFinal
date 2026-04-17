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

    public EateryDetailPage(Poi poi, TranslationService translationService, TtsService ttsService, AppDbContext dbContext)
    {
        InitializeComponent();

        _poi = poi;
        _translationService = translationService;
        _ttsService = ttsService;
        _dbContext = dbContext;

        BindingContext = _poi;
    }

    private async void OnBackClicked(object sender, EventArgs e)
    {
        await Navigation.PopAsync();
    }

    private void OnStopAudioClicked(object sender, EventArgs e)
    {
        _ttsService.Stop();
        StatusLabel.Text = "Đã dừng phát âm thanh.";
    }

    private async void OnPlayAudioClicked(object sender, EventArgs e)
    {
        StatusLabel.Text = "Đang chuẩn bị...";
        PlayAudioButton.IsVisible = false;
        StopAudioButton.IsVisible = true;

        try
        {
            string deviceLang = CultureInfo.CurrentUICulture.Name;

            var cache = await _dbContext.GetCacheAsync(_poi.Id, deviceLang);
            string audioText = cache?.TranslatedText;

            if (string.IsNullOrWhiteSpace(audioText))
            {
                bool needsTranslation = !deviceLang.StartsWith("vi", StringComparison.OrdinalIgnoreCase);

                if (needsTranslation)
                {
                    StatusLabel.Text = $"Đang dịch sang {deviceLang}...";

                    var (translated, success) = await _translationService.TranslateWithStatusAsync(
                        _poi.Description_VN, deviceLang);

                    if (success)
                    {
                        audioText = translated;

                        await _dbContext.SaveCacheAsync(new TranslationCache
                        {
                            PoiId = _poi.Id,
                            LanguageCode = deviceLang,
                            TranslatedText = audioText,
                            CreatedAt = DateTime.Now
                        });
                    }
                    else
                    {
                        StatusLabel.Text = "⚠️ Không dịch được, sẽ đọc tiếng Việt...";
                        await Task.Delay(1500);
                        audioText = _poi.Description_VN;
                    }
                }
                else
                {
                    audioText = _poi.Description_VN;
                }
            }

            if (string.IsNullOrWhiteSpace(audioText))
                audioText = _poi.Description_VN;

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