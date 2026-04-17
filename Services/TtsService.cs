using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Globalization;
using Microsoft.Maui.Media;

namespace VinhKhanhTourGuide.Services
{
    public class TtsService
    {
        private CancellationTokenSource _cts;

        public async Task SpeakAsync(string text, string langCode = null)
        {
            Stop(); 
            _cts = new CancellationTokenSource();

            try
            {
                if (string.IsNullOrEmpty(langCode))
                {
                    langCode = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName;
                }
                else if (langCode.Length >= 2)
                {
                    langCode = langCode.Substring(0, 2);
                }

                var locales = await TextToSpeech.Default.GetLocalesAsync();

                var selectedLocale = locales.FirstOrDefault(l => l.Language.StartsWith(langCode, StringComparison.OrdinalIgnoreCase));

                var options = new SpeechOptions()
                {
                    Locale = selectedLocale
                };

                await TextToSpeech.Default.SpeakAsync(text, options, cancelToken: _cts.Token);
            }
            catch (TaskCanceledException)
            {
                // Bỏ qua lỗi khi người dùng chủ động bấm nút Stop
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[LỖI TTS] {ex.Message}");
            }
        }

        public void Stop()
        {
            if (_cts?.IsCancellationRequested == false)
            {
                _cts.Cancel();
            }
        }
    }
}