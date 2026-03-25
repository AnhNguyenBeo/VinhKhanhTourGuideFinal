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

        // ⚠️ Nên chuyển key sang config sau khi test xong
        private const string GeminiApiKey = "AIzaSyDcqDqk7XYtcmyVhxc9WXxSczPuaZLHJh8";

        public TranslationService()
        {
            // HttpClient handler giúp tránh lỗi socket trên Android
            _httpClient = new HttpClient(new HttpClientHandler());
        }

        public async Task<string> TranslateAsync(string textOriginal, string targetLanguageCode)
        {
            try
            {
                string prompt = $"Translate the following Vietnamese culinary description to {targetLanguageCode}. Preserve the cultural nuance. Only return the translated text without any quotes or extra explanations: {textOriginal}";

                var requestBody = new { contents = new[] { new { parts = new[] { new { text = prompt } } } } };
                string jsonContent = JsonSerializer.Serialize(requestBody);
                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

                // Vẫn dùng v1beta và gemini-2.5-flash cho chắc cốp
                string url = $"https://generativelanguage.googleapis.com/v1beta/models/gemini-2.5-flash:generateContent?key={GeminiApiKey}";

                var response = await _httpClient.PostAsync(url, content);

                // NẾU GOOGLE TRẢ VỀ LỖI (Bao gồm cả 404) -> ĐỌC THẲNG LÝ DO TỪ GOOGLE
                if (!response.IsSuccessStatusCode)
                {
                    string googleError = await response.Content.ReadAsStringAsync();
                    return $"Translation error. Google báo lỗi: {googleError}";
                }

                var responseJson = await response.Content.ReadAsStringAsync();
                using JsonDocument doc = JsonDocument.Parse(responseJson);

                var translatedText = doc.RootElement
                    .GetProperty("candidates")[0]
                    .GetProperty("content")
                    .GetProperty("parts")[0]
                    .GetProperty("text").GetString();

                return translatedText.Trim();
            }
            catch (Exception ex)
            {
                return $"Translation error. Exception: {ex.Message}";
            }
        }
    }
}