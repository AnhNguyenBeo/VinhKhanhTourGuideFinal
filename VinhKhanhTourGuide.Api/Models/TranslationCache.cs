using System.ComponentModel.DataAnnotations;

namespace VinhKhanhTourGuide.Api.Models
{
    public class TranslationCache
    {
        [Key]
        public int Id { get; set; }

        public string PoiId { get; set; }

        public string LanguageCode { get; set; }

        public string TranslatedText { get; set; }

        public DateTime CreatedAt { get; set; }
    }
}
