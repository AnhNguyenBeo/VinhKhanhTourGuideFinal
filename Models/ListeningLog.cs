namespace VinhKhanhTourGuide.Models
{
    public class ListeningLog
    {
        public string PoiId { get; set; }
        // Mã ẩn danh sinh ra 1 lần duy nhất cho mỗi điện thoại
        public string AnonymousSessionId { get; set; }
        public double DurationSeconds { get; set; }
        public double Latitude { get; set; }
        public double Longitude { get; set; }
    }
}