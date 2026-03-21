using System.Threading.Tasks;

namespace VinhKhanhTourGuide.Services
{
    public class TranslationService
    {
        // Kiểm tra kỹ tên hàm và các tham số truyền vào ở dòng này:
        public async Task<string> TranslateAsync(string textOriginal, string targetLanguageCode)
        {
            await Task.Delay(1000);
            if (textOriginal.Contains("Ốc Oanh")) { return "Welcome to Oc Oanh..."; }
            return "Translation completed.";
        }
    }
}