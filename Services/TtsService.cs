using System.Threading.Tasks;
using Microsoft.Maui.Media;

namespace VinhKhanhTourGuide.Services
{
    public class TtsService
    {
        public async Task SpeakAsync(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return;

            await TextToSpeech.Default.SpeakAsync(text);
        }
    }
}