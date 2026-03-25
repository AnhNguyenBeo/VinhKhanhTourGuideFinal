using SQLite;
using System;

namespace VinhKhanhTourGuide.Models
{
    public class TranslationCache
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }

        [Indexed] // Đánh index để truy vấn cực nhanh khi khách đi vào vùng Geofence
        public string PoiId { get; set; }
        public string LanguageCode { get; set; }
        public string TranslatedText { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}