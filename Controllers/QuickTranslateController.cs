using Microsoft.AspNetCore.Mvc;
using Project_translator.Services;
using System.Text.Json;

namespace Project_translator.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class QuickTranslateController : ControllerBase
    {
        private readonly ITranslationService _translationService;
        private readonly ILogger<QuickTranslateController> _logger;

        public QuickTranslateController(
            ITranslationService translationService,
            ILogger<QuickTranslateController> logger)
        {
            _translationService = translationService;
            _logger = logger;
        }

        [HttpPost("translate")]
        public async Task<IActionResult> Translate([FromBody] QuickTranslateRequest request)
        {
            try
            {
                _logger.LogInformation($"Quick translate: {request.Text} ({request.SourceLang} -> {request.TargetLang})");

                var translatedText = await _translationService.TranslateAsync(
                    request.Text,
                    request.SourceLang,
                    request.TargetLang);

                return Ok(new
                {
                    success = true,
                    original = request.Text,
                    translated = translatedText,
                    source = request.SourceLang,
                    target = request.TargetLang,
                    timestamp = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Quick translate error");
                return BadRequest(new
                {
                    success = false,
                    error = ex.Message
                });
            }
        }

        [HttpGet("status")]
        public IActionResult GetStatus()
        {
            return Ok(new
            {
                status = "online",
                service = "MyMemory Translation API",
                supportedLanguages = new[] { "en", "ru", "tt", "es", "fr", "de" },
                timestamp = DateTime.UtcNow
            });
        }

        [HttpGet("test")]
        public async Task<IActionResult> Test()
        {
            try
            {
                // Тестовый перевод
                var result = await _translationService.TranslateAsync("Hello", "en", "tt");

                return Ok(new
                {
                    success = true,
                    test = "Hello -> Tatar",
                    result = result,
                    apiWorking = !result.Contains("Hello") && result.Length > 0,
                    timestamp = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    success = false,
                    error = ex.Message,
                    suggestion = "Check internet connection and MyMemory API availability"
                });
            }
        }
    }

    public class QuickTranslateRequest
    {
        public string Text { get; set; } = string.Empty;
        public string SourceLang { get; set; } = "auto";
        public string TargetLang { get; set; } = "ru";
    }
}