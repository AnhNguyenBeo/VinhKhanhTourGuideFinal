using VinhKhanhTourGuide.Data;
using VinhKhanhTourGuide.Models;

namespace VinhKhanhTourGuide.Services
{
    public class TranslationService
    {
        private readonly AppDbContext _dbContext;

        public TranslationService(AppDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<(string text, bool success)> ResolvePoiNarrationAsync(Poi poi, string targetLanguageCode)
        {
            string normalizedLanguageCode = NormalizeLanguageCode(targetLanguageCode);

            if (normalizedLanguageCode == "vi")
            {
                return (poi.Description_VN, true);
            }

            TranslationCache? localCache = await _dbContext.GetCacheAsync(poi.Id, normalizedLanguageCode);
            if (localCache != null && !string.IsNullOrWhiteSpace(localCache.TranslatedText))
            {
                return (localCache.TranslatedText, true);
            }

            var (translatedText, success, _) = await _dbContext.ResolveSharedTranslationAsync(
                poi.Id,
                poi.Description_VN,
                normalizedLanguageCode);

            if (!success || string.IsNullOrWhiteSpace(translatedText))
            {
                return (poi.Description_VN, false);
            }

            await _dbContext.SaveCacheAsync(new TranslationCache
            {
                PoiId = poi.Id,
                LanguageCode = normalizedLanguageCode,
                TranslatedText = translatedText,
                CreatedAt = DateTime.Now
            });

            return (translatedText, true);
        }

        private static string NormalizeLanguageCode(string? languageCode)
        {
            if (string.IsNullOrWhiteSpace(languageCode))
            {
                return "vi";
            }

            string normalized = languageCode.Trim().ToLowerInvariant();
            int separatorIndex = normalized.IndexOfAny(['-', '_']);

            return separatorIndex > 0
                ? normalized[..separatorIndex]
                : normalized;
        }
    }
}
