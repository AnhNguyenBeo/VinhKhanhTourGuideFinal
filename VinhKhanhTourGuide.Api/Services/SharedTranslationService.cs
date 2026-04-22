using System.Collections.Concurrent;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using VinhKhanhTourGuide.Api.Data;
using VinhKhanhTourGuide.Api.Models;

namespace VinhKhanhTourGuide.Api.Services
{
    public class SharedTranslationService
    {
        private static readonly ConcurrentDictionary<string, SemaphoreSlim> TranslationLocks = new();

        private readonly TourDbContext _dbContext;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _configuration;
        private readonly ILogger<SharedTranslationService> _logger;

        public SharedTranslationService(
            TourDbContext dbContext,
            IHttpClientFactory httpClientFactory,
            IConfiguration configuration,
            ILogger<SharedTranslationService> logger)
        {
            _dbContext = dbContext;
            _httpClientFactory = httpClientFactory;
            _configuration = configuration;
            _logger = logger;
        }

        public async Task<TranslationResolveResponse> ResolveAsync(string poiId, string sourceText, string targetLanguageCode)
        {
            string normalizedLanguageCode = NormalizeLanguageCode(targetLanguageCode);
            string safeText = sourceText?.Trim() ?? string.Empty;

            if (string.IsNullOrWhiteSpace(poiId) || string.IsNullOrWhiteSpace(safeText))
            {
                return new TranslationResolveResponse
                {
                    Text = safeText,
                    LanguageCode = normalizedLanguageCode,
                    CacheHit = false,
                    Success = false
                };
            }

            if (normalizedLanguageCode == "vi")
            {
                return new TranslationResolveResponse
                {
                    Text = safeText,
                    LanguageCode = normalizedLanguageCode,
                    CacheHit = true,
                    Success = true
                };
            }

            TranslationCache? existingCache = await FindCacheAsync(poiId, normalizedLanguageCode);
            if (existingCache != null)
            {
                return BuildCacheHitResponse(existingCache);
            }

            string lockKey = $"{poiId}:{normalizedLanguageCode}";
            SemaphoreSlim translationLock = TranslationLocks.GetOrAdd(lockKey, _ => new SemaphoreSlim(1, 1));

            await translationLock.WaitAsync();
            try
            {
                existingCache = await FindCacheAsync(poiId, normalizedLanguageCode);
                if (existingCache != null)
                {
                    return BuildCacheHitResponse(existingCache);
                }

                string? translatedText = await TranslateWithGeminiAsync(safeText, normalizedLanguageCode);
                if (string.IsNullOrWhiteSpace(translatedText))
                {
                    return new TranslationResolveResponse
                    {
                        Text = safeText,
                        LanguageCode = normalizedLanguageCode,
                        CacheHit = false,
                        Success = false
                    };
                }

                var newCache = new TranslationCache
                {
                    PoiId = poiId,
                    LanguageCode = normalizedLanguageCode,
                    TranslatedText = translatedText.Trim(),
                    CreatedAt = DateTime.UtcNow
                };

                _dbContext.TranslationCaches.Add(newCache);

                try
                {
                    await _dbContext.SaveChangesAsync();
                }
                catch (DbUpdateException dbEx)
                {
                    _logger.LogWarning(dbEx, "Race condition khi luu cache dich cho {PoiId} - {LanguageCode}.", poiId, normalizedLanguageCode);
                    existingCache = await FindCacheAsync(poiId, normalizedLanguageCode);

                    if (existingCache != null)
                    {
                        return BuildCacheHitResponse(existingCache);
                    }

                    return new TranslationResolveResponse
                    {
                        Text = translatedText.Trim(),
                        LanguageCode = normalizedLanguageCode,
                        CacheHit = false,
                        Success = true
                    };
                }

                return new TranslationResolveResponse
                {
                    Text = newCache.TranslatedText,
                    LanguageCode = normalizedLanguageCode,
                    CacheHit = false,
                    Success = true
                };
            }
            finally
            {
                translationLock.Release();
            }
        }

        private async Task<TranslationCache?> FindCacheAsync(string poiId, string normalizedLanguageCode)
        {
            return await _dbContext.TranslationCaches
                .AsNoTracking()
                .FirstOrDefaultAsync(cache => cache.PoiId == poiId && cache.LanguageCode == normalizedLanguageCode);
        }

        private static TranslationResolveResponse BuildCacheHitResponse(TranslationCache cache)
        {
            return new TranslationResolveResponse
            {
                Text = cache.TranslatedText,
                LanguageCode = cache.LanguageCode,
                CacheHit = true,
                Success = true
            };
        }

        private async Task<string?> TranslateWithGeminiAsync(string sourceText, string normalizedLanguageCode)
        {
            string? apiKey = _configuration["Translation:GeminiApiKey"];
            if (string.IsNullOrWhiteSpace(apiKey))
            {
                _logger.LogWarning("GeminiApiKey chua duoc cau hinh. Bo qua shared translation cache.");
                return null;
            }

            string prompt =
                $"Translate the following Vietnamese culinary description to {normalizedLanguageCode}. " +
                "Preserve the cultural nuance and tone. " +
                "Only return the translated text without quotes or any extra explanation.\n\n" +
                sourceText;

            var requestBody = new
            {
                contents = new[]
                {
                    new
                    {
                        parts = new[]
                        {
                            new { text = prompt }
                        }
                    }
                }
            };

            string requestJson = JsonSerializer.Serialize(requestBody);
            using var client = _httpClientFactory.CreateClient();
            using var content = new StringContent(requestJson, Encoding.UTF8, "application/json");

            string url =
                $"https://generativelanguage.googleapis.com/v1beta/models/gemini-2.5-flash:generateContent?key={apiKey}";

            using HttpResponseMessage response = await client.PostAsync(url, content);

            if (!response.IsSuccessStatusCode)
            {
                string errorBody = await response.Content.ReadAsStringAsync();
                _logger.LogWarning("Gemini translation fail {StatusCode}: {Body}", (int)response.StatusCode, errorBody);
                return null;
            }

            string responseJson = await response.Content.ReadAsStringAsync();
            using JsonDocument document = JsonDocument.Parse(responseJson);

            return document.RootElement
                .GetProperty("candidates")[0]
                .GetProperty("content")
                .GetProperty("parts")[0]
                .GetProperty("text")
                .GetString();
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
