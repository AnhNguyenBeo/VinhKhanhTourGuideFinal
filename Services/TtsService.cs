using System.Threading;
using System.Threading.Tasks;
using Microsoft.Maui.Media;
using System;

namespace VinhKhanhTourGuide.Services
{
    public class TtsService
    {
        private CancellationTokenSource _cts;

        public async Task SpeakAsync(string text)
        {
            Stop(); // Chặn giọng đọc cũ nếu đang phát
            _cts = new CancellationTokenSource();

            try
            {
                // Gọi thẳng engine mặc định của hệ điều hành
                await TextToSpeech.Default.SpeakAsync(text, cancelToken: _cts.Token);
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
                _cts.Cancel(); // Ra lệnh im lặng ngay lập tức
            }
        }
    }
}