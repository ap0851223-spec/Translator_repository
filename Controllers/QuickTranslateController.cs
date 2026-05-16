// Controllers/QuickTranslateController.cs
using Microsoft.AspNetCore.Mvc;
using Project_translator.Services;

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
                _logger.LogInformation($"Запрос: {request.Text} ({request.SourceLang} -> {request.TargetLang})");

                var translated = await _translationService.TranslateAsync(
                    request.Text,
                    request.SourceLang,
                    request.TargetLang);

                return Ok(new
                {
                    success = true,
                    original = request.Text,
                    translated = translated,
                    source = request.SourceLang,
                    target = request.TargetLang
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка перевода");
                return Ok(new
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
                service = "Translation API",
                supportedLanguages = new[] { "en", "ru", "tt", "es", "fr", "de", "tr", "zh" },
                timestamp = DateTime.UtcNow
            });
        }

        public class QuickTranslateRequest
        {
            public string Text { get; set; } = "";
            public string SourceLang { get; set; } = "auto";
            public string TargetLang { get; set; } = "ru";
        }
    }
}