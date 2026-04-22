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
                return (string.Empty, false);
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

        public async Task PrefetchNarrationsAsync(IEnumerable<Poi> pois, string targetLanguageCode, int maxCount = 6)
        {
            string normalizedLanguageCode = NormalizeLanguageCode(targetLanguageCode);
            if (normalizedLanguageCode == "vi")
            {
                return;
            }

            int warmed = 0;
            foreach (Poi poi in pois)
            {
                if (warmed >= maxCount)
                {
                    break;
                }

                if (poi == null ||
                    string.IsNullOrWhiteSpace(poi.Id) ||
                    string.IsNullOrWhiteSpace(poi.Description_VN))
                {
                    continue;
                }

                try
                {
                    TranslationCache? localCache = await _dbContext.GetCacheAsync(poi.Id, normalizedLanguageCode);
                    if (localCache != null && !string.IsNullOrWhiteSpace(localCache.TranslatedText))
                    {
                        warmed++;
                        continue;
                    }

                    await ResolvePoiNarrationAsync(poi, normalizedLanguageCode);
                    warmed++;
                }
                catch
                {
                    // Prefetch khong duoc phep lam anh huong luong chinh.
                }
            }
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
