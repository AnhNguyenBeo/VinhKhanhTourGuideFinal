using Microsoft.AspNetCore.Mvc;
using VinhKhanhTourGuide.Api.Models;
using VinhKhanhTourGuide.Api.Services;

namespace VinhKhanhTourGuide.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class TranslationsController : ControllerBase
    {
        private readonly SharedTranslationService _sharedTranslationService;

        public TranslationsController(SharedTranslationService sharedTranslationService)
        {
            _sharedTranslationService = sharedTranslationService;
        }

        [HttpPost("resolve")]
        public async Task<IActionResult> Resolve([FromBody] TranslationResolveRequest request)
        {
            if (request == null ||
                string.IsNullOrWhiteSpace(request.PoiId) ||
                string.IsNullOrWhiteSpace(request.SourceText))
            {
                return BadRequest(new TranslationResolveResponse
                {
                    Text = request?.SourceText ?? string.Empty,
                    LanguageCode = request?.TargetLanguageCode ?? "vi",
                    CacheHit = false,
                    Success = false
                });
            }

            try
            {
                TranslationResolveResponse response = await _sharedTranslationService.ResolveAsync(
                    request.PoiId!,
                    request.SourceText!,
                    request.TargetLanguageCode ?? "vi");

                return Ok(response);
            }
            catch
            {
                return Ok(new TranslationResolveResponse
                {
                    Text = request.SourceText ?? string.Empty,
                    LanguageCode = request.TargetLanguageCode ?? "vi",
                    CacheHit = false,
                    Success = false
                });
            }
        }
    }
}
