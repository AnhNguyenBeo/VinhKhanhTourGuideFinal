namespace VinhKhanhTourGuide.Api.Models
{
    public class TranslationResolveResponse
    {
        public string Text { get; set; }

        public string LanguageCode { get; set; }

        public bool CacheHit { get; set; }

        public bool Success { get; set; }
    }
}
