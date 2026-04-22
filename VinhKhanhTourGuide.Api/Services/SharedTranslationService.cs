using System.Collections.Concurrent;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Caching.Memory;
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
        private readonly IMemoryCache _memoryCache;

        public SharedTranslationService(
            TourDbContext dbContext,
            IHttpClientFactory httpClientFactory,
            IConfiguration configuration,
            ILogger<SharedTranslationService> logger,
            IMemoryCache memoryCache)
        {
            _dbContext = dbContext;
            _httpClientFactory = httpClientFactory;
            _configuration = configuration;
            _logger = logger;
            _memoryCache = memoryCache;
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

            string memoryCacheKey = BuildMemoryCacheKey(poiId, normalizedLanguageCode);
            if (_memoryCache.TryGetValue(memoryCacheKey, out string? memoryHitText) &&
                !string.IsNullOrWhiteSpace(memoryHitText))
            {
                return new TranslationResolveResponse
                {
                    Text = memoryHitText,
                    LanguageCode = normalizedLanguageCode,
                    CacheHit = true,
                    Success = true
                };
            }

            TranslationCache? existingCache = await TryFindCacheAsync(poiId, normalizedLanguageCode);
            if (existingCache != null)
            {
                SetMemoryCache(memoryCacheKey, existingCache.TranslatedText);
                return BuildCacheHitResponse(existingCache);
            }

            string lockKey = $"{poiId}:{normalizedLanguageCode}";
            SemaphoreSlim translationLock = TranslationLocks.GetOrAdd(lockKey, _ => new SemaphoreSlim(1, 1));

            await translationLock.WaitAsync();
            try
            {
                existingCache = await TryFindCacheAsync(poiId, normalizedLanguageCode);
                if (existingCache != null)
                {
                    SetMemoryCache(memoryCacheKey, existingCache.TranslatedText);
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

                try
                {
                    _dbContext.TranslationCaches.Add(newCache);
                    await _dbContext.SaveChangesAsync();
                }
                catch (DbUpdateException dbEx)
                {
                    _logger.LogWarning(dbEx, "Race condition khi luu cache dich cho {PoiId} - {LanguageCode}.", poiId, normalizedLanguageCode);
                    existingCache = await TryFindCacheAsync(poiId, normalizedLanguageCode);

                    if (existingCache != null)
                    {
                        SetMemoryCache(memoryCacheKey, existingCache.TranslatedText);
                        return BuildCacheHitResponse(existingCache);
                    }

                    SetMemoryCache(memoryCacheKey, translatedText.Trim());
                    return new TranslationResolveResponse
                    {
                        Text = translatedText.Trim(),
                        LanguageCode = normalizedLanguageCode,
                        CacheHit = false,
                        Success = true
                    };
                }
                catch (Exception ex)
                {
                    // Cache la toi uu, khong duoc phep lam hong luong dich chinh.
                    _logger.LogWarning(ex, "Khong the luu translation cache cho {PoiId} - {LanguageCode}.", poiId, normalizedLanguageCode);
                    SetMemoryCache(memoryCacheKey, translatedText.Trim());
                    return new TranslationResolveResponse
                    {
                        Text = translatedText.Trim(),
                        LanguageCode = normalizedLanguageCode,
                        CacheHit = false,
                        Success = true
                    };
                }

                SetMemoryCache(memoryCacheKey, newCache.TranslatedText);
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

        private void SetMemoryCache(string key, string translatedText)
        {
            _memoryCache.Set(
                key,
                translatedText,
                new MemoryCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(12),
                    SlidingExpiration = TimeSpan.FromHours(2)
                });
        }

        private static string BuildMemoryCacheKey(string poiId, string languageCode)
        {
            return $"translation:{poiId}:{languageCode}";
        }

        private async Task<TranslationCache?> TryFindCacheAsync(string poiId, string normalizedLanguageCode)
        {
            try
            {
                return await _dbContext.TranslationCaches
                    .AsNoTracking()
                    .FirstOrDefaultAsync(cache => cache.PoiId == poiId && cache.LanguageCode == normalizedLanguageCode);
            }
            catch (Exception ex)
            {
                // Loi DB/cache khong duoc phep danh sap endpoint dich.
                _logger.LogWarning(ex, "Khong the doc TranslationCache cho {PoiId} - {LanguageCode}.", poiId, normalizedLanguageCode);
                return null;
            }
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

            try
            {
                using HttpResponseMessage response = await client.PostAsync(url, content);

                if (!response.IsSuccessStatusCode)
                {
                    string errorBody = await response.Content.ReadAsStringAsync();
                    _logger.LogWarning("Gemini translation fail {StatusCode}: {Body}", (int)response.StatusCode, errorBody);
                    return null;
                }

                string responseJson = await response.Content.ReadAsStringAsync();
                using JsonDocument document = JsonDocument.Parse(responseJson);

                if (!document.RootElement.TryGetProperty("candidates", out JsonElement candidates) ||
                    candidates.ValueKind != JsonValueKind.Array ||
                    candidates.GetArrayLength() == 0)
                {
                    _logger.LogWarning("Gemini response khong co candidates hop le: {Body}", responseJson);
                    return null;
                }

                JsonElement firstCandidate = candidates[0];
                if (!firstCandidate.TryGetProperty("content", out JsonElement contentElement) ||
                    !contentElement.TryGetProperty("parts", out JsonElement partsElement) ||
                    partsElement.ValueKind != JsonValueKind.Array ||
                    partsElement.GetArrayLength() == 0)
                {
                    _logger.LogWarning("Gemini response khong co content/parts hop le: {Body}", responseJson);
                    return null;
                }

                JsonElement firstPart = partsElement[0];
                if (!firstPart.TryGetProperty("text", out JsonElement textElement))
                {
                    _logger.LogWarning("Gemini response khong co truong text: {Body}", responseJson);
                    return null;
                }

                return textElement.GetString();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Gemini translation exception.");
                return null;
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
