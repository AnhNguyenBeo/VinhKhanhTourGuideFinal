namespace VinhKhanhTourGuide.Api.Models
{
    public class TranslationResolveRequest
    {
        public string? PoiId { get; set; }

        public string? SourceText { get; set; }

        public string? TargetLanguageCode { get; set; }
    }
}
