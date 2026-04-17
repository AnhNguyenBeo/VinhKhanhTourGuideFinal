using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace VinhKhanhTourGuide.Services
{
    public class TranslationService
    {
        private readonly HttpClient _httpClient;

        private const string GeminiApiKey = "";

        public TranslationService()
        {
            _httpClient = new HttpClient();
        }

        public async Task<string> TranslateAsync(string textOriginal, string targetLanguageCode)
        {
            var (result, _) = await TranslateWithStatusAsync(textOriginal, targetLanguageCode);
            return result;
        }

        public async Task<(string text, bool success)> TranslateWithStatusAsync(
            string textOriginal, string targetLanguageCode)
        {
            // Tiếng Việt → trả về luôn, coi là "thành công"
            if (targetLanguageCode.StartsWith("vi", StringComparison.OrdinalIgnoreCase))
                return (textOriginal, true);

            try
            {
                string prompt = $"Translate the following Vietnamese culinary description to {targetLanguageCode}. " +
                                $"Preserve the cultural nuance. " +
                                $"Only return the translated text without any quotes or extra explanations: {textOriginal}";

                var requestBody = new { contents = new[] { new { parts = new[] { new { text = prompt } } } } };
                string jsonContent = JsonSerializer.Serialize(requestBody);
                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

                string url = $"https://generativelanguage.googleapis.com/v1beta/models/gemini-2.5-flash:generateContent?key={GeminiApiKey}";

                var response = await _httpClient.PostAsync(url, content);

                if (!response.IsSuccessStatusCode)
                {
                    string errorBody = await response.Content.ReadAsStringAsync();
                    System.Diagnostics.Debug.WriteLine(
                        $"[Translation] API lỗi {(int)response.StatusCode}: {errorBody}");
                    return (textOriginal, false); // ← báo thất bại rõ ràng
                }

                var responseJson = await response.Content.ReadAsStringAsync();
                using JsonDocument doc = JsonDocument.Parse(responseJson);

                var translatedText = doc.RootElement
                    .GetProperty("candidates")[0]
                    .GetProperty("content")
                    .GetProperty("parts")[0]
                    .GetProperty("text")
                    .GetString();

                if (string.IsNullOrWhiteSpace(translatedText))
                {
                    System.Diagnostics.Debug.WriteLine("[Translation] Gemini trả về rỗng.");
                    return (textOriginal, false);
                }

                return (translatedText.Trim(), true);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[Translation] Exception: {ex.Message}");
                return (textOriginal, false);
            }
        }
    }
}