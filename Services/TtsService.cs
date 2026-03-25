using System.Threading;
using System.Threading.Tasks;
using Microsoft.Maui.Media;

namespace VinhKhanhTourGuide.Services
{
    public class TtsService
    {
        private CancellationTokenSource _cts;

        public async Task SpeakAsync(string text)
        {
            Stop(); // Chặn giọng đọc cũ nếu có
            _cts = new CancellationTokenSource();
            try
            {
                await TextToSpeech.Default.SpeakAsync(text, cancelToken: _cts.Token);
            }
            catch (TaskCanceledException)
            {
                // Bỏ qua lỗi khi người dùng chủ động bấm Stop
            }
        }

        public void Stop()
        {
            if (_cts?.IsCancellationRequested == false)
            {
                _cts.Cancel(); // Ra lệnh im lặng ngay lập tức
            }
        }
    }
}