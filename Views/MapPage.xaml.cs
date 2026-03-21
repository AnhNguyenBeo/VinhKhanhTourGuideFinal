using VinhKhanhTourGuide.Models;
using VinhKhanhTourGuide.Services;

namespace VinhKhanhTourGuide.Views
{
    public partial class MapPage : ContentPage
    {
        private readonly TtsService _ttsService;
        private readonly TranslationService _translationService;

        // Tiêm (Inject) các Services vào View
        public MapPage(TtsService ttsService, TranslationService translationService)
        {
            InitializeComponent();
            _ttsService = ttsService;
            _translationService = translationService;
        }

        private async void OnTriggerOcOanhClicked(object sender, EventArgs e)
        {
            // 1. Kích hoạt giả lập Geofence
            StatusLabel.Text = "Trạng thái: Đã lọt vào vùng Geofence của Ốc Oanh...";
            ((Button)sender).IsEnabled = false; // Khóa nút để tránh bấm 2 lần chồng âm thanh

            var ocOanh = new Poi
            {
                Id = "OANH-001",
                Name = "Ốc Oanh",
                Description_VN = "Ốc Oanh là quán hải sản nổi tiếng nhất phố Vĩnh Khánh. Đặc sản ở đây là ốc hương nướng muối ớt."
            };

            // 2. Gọi AI dịch (Mock)
            StatusLabel.Text = "Trạng thái: Đang gọi AI dịch sang tiếng Anh...";
            string englishText = await _translationService.TranslateAsync(ocOanh.Description_VN, "en-US");

            // 3. Đọc Text-to-Speech
            StatusLabel.Text = "Trạng thái: Đang phát âm thanh TTS...";
            await _ttsService.SpeakAsync(englishText);

            StatusLabel.Text = "Trạng thái: Hoàn tất thuyết minh.";
            ((Button)sender).IsEnabled = true;
        }
    }
}