using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Project_translator.Data;
using Project_translator.Models;
using Project_translator.Services;

namespace Project_translator.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class TranslationsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly ITranslationService _translationService;

        // Добавляем сервис перевода в конструктор
        public TranslationsController(ApplicationDbContext context, ITranslationService translationService)
        {
            _context = context;
            _translationService = translationService;
        }

        // GET: api/Translations
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Translation>>> GetTranslations()
        {
            return await _context.Translations
                .Include(t => t.SourceString)
                .Include(t => t.Locale)
                .ToListAsync();
        }

        // GET: api/Translations/5
        [HttpGet("{id}")]
        public async Task<ActionResult<Translation>> GetTranslation(int id)
        {
            var translation = await _context.Translations
                .Include(t => t.SourceString)
                .Include(t => t.Locale)
                .FirstOrDefaultAsync(t => t.Id == id);

            if (translation == null)
            {
                return NotFound();
            }

            return translation;
        }

        // GET: api/Translations/string/5/locale/2
        [HttpGet("string/{sourceStringId}/locale/{localeId}")]
        public async Task<ActionResult<Translation>> GetTranslationByStringAndLocale(int sourceStringId, int localeId)
        {
            var translation = await _context.Translations
                .Include(t => t.SourceString)
                .Include(t => t.Locale)
                .FirstOrDefaultAsync(t => t.SourceStringId == sourceStringId && t.LocaleId == localeId);

            if (translation == null)
            {
                return NotFound();
            }

            return translation;
        }

        // GET: api/Translations/locale/2
        [HttpGet("locale/{localeId}")]
        public async Task<ActionResult<IEnumerable<Translation>>> GetTranslationsByLocale(int localeId)
        {
            return await _context.Translations
                .Where(t => t.LocaleId == localeId)
                .Include(t => t.SourceString)
                .Include(t => t.Locale)
                .ToListAsync();
        }

        // GET: api/Translations/project/1/locale/2
        [HttpGet("project/{projectId}/locale/{localeId}")]
        public async Task<ActionResult<IEnumerable<Translation>>> GetTranslationsByProjectAndLocale(int projectId, int localeId)
        {
            return await _context.Translations
                .Include(t => t.SourceString)
                .Include(t => t.Locale)
                .Where(t => t.LocaleId == localeId && t.SourceString.ProjectId == projectId)
                .ToListAsync();
        }

        // POST: api/Translations
        [HttpPost]
        public async Task<ActionResult<Translation>> PostTranslation(Translation translation)
        {
            _context.Translations.Add(translation);
            await _context.SaveChangesAsync();

            return CreatedAtAction("GetTranslation", new { id = translation.Id }, translation);
        }

        // PUT: api/Translations/5
        [HttpPut("{id}")]
        public async Task<IActionResult> PutTranslation(int id, Translation translation)
        {
            if (id != translation.Id)
            {
                return BadRequest();
            }

            _context.Entry(translation).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!TranslationExists(id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }

            return NoContent();
        }

        // PATCH: api/Translations/5/status
        [HttpPatch("{id}/status")]
        public async Task<IActionResult> UpdateTranslationStatus(int id, [FromBody] UpdateStatusRequest request)
        {
            var translation = await _context.Translations.FindAsync(id);
            if (translation == null)
            {
                return NotFound();
            }

            translation.Status = request.Status;
            await _context.SaveChangesAsync();

            return NoContent();
        }

        // DELETE: api/Translations/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteTranslation(int id)
        {
            var translation = await _context.Translations.FindAsync(id);
            if (translation == null)
            {
                return NotFound();
            }

            _context.Translations.Remove(translation);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        // ★★★★★ НОВЫЙ ENDPOINT: Перевод текста ★★★★★
        [HttpPost("translate-text")]
        public async Task<ActionResult<TranslationResponse>> TranslateText([FromBody] TranslateTextRequest request)
        {
            try
            {
                // Выполняем перевод через сервис
                var translatedText = await _translationService.TranslateAsync(
                    request.Text,
                    request.SourceLang,
                    request.TargetLang);

                // Если нужно сохранить в БД
                if (request.SaveToDatabase)
                {
                    var sourceString = await _context.SourceStrings.FindAsync(request.SourceStringId);
                    var locale = await _context.Locales.FirstOrDefaultAsync(l => l.Code == request.TargetLang);

                    if (sourceString != null && locale != null)
                    {
                        var translation = new Translation
                        {
                            SourceStringId = sourceString.Id,
                            LocaleId = locale.Id,
                            TranslatedText = translatedText,
                            Status = "auto_generated"
                        };

                        _context.Translations.Add(translation);
                        await _context.SaveChangesAsync();

                        return Ok(new TranslationResponse
                        {
                            Success = true,
                            TranslationId = translation.Id,
                            OriginalText = request.Text,
                            TranslatedText = translatedText,
                            SourceLang = request.SourceLang,
                            TargetLang = request.TargetLang
                        });
                    }
                }

                // Возвращаем только перевод без сохранения в БД
                return Ok(new TranslationResponse
                {
                    Success = true,
                    OriginalText = request.Text,
                    TranslatedText = translatedText,
                    SourceLang = request.SourceLang,
                    TargetLang = request.TargetLang
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new TranslationResponse
                {
                    Success = false,
                    Error = ex.Message
                });
            }
        }

        // ★★★★★ Существующий endpoint для авто-перевода строк ★★★★★
        [HttpPost("auto-translate")]
        public async Task<ActionResult<Translation>> AutoTranslate([FromBody] AutoTranslateRequest request)
        {
            var sourceString = await _context.SourceStrings
                .FirstOrDefaultAsync(s => s.Id == request.SourceStringId);

            if (sourceString == null)
            {
                return NotFound("Source string not found");
            }

            var locale = await _context.Locales
                .FirstOrDefaultAsync(l => l.Id == request.LocaleId);

            if (locale == null)
            {
                return NotFound("Locale not found");
            }

            var translatedText = await _translationService.TranslateAsync(
                sourceString.Text,
                "en",
                locale.Code);

            var existingTranslation = await _context.Translations
                .FirstOrDefaultAsync(t => t.SourceStringId == request.SourceStringId && t.LocaleId == request.LocaleId);

            if (existingTranslation == null)
            {
                var translation = new Translation
                {
                    SourceStringId = request.SourceStringId,
                    LocaleId = request.LocaleId,
                    TranslatedText = translatedText,
                    Status = "auto_translated"
                };

                _context.Translations.Add(translation);
                await _context.SaveChangesAsync();

                return CreatedAtAction("GetTranslation", new { id = translation.Id }, translation);
            }
            else
            {
                existingTranslation.TranslatedText = translatedText;
                existingTranslation.Status = "auto_updated";
                await _context.SaveChangesAsync();

                return Ok(existingTranslation);
            }
        }

        // ★★★★★ НОВЫЙ ENDPOINT: Тест MyMemory API ★★★★★
        [HttpGet("test-mymemory")]
        public async Task<ActionResult> TestMyMemoryAPI([FromQuery] string text = "Hello",
                                                        [FromQuery] string source = "en",
                                                        [FromQuery] string target = "tt")
        {
            try
            {
                var translatedText = await _translationService.TranslateAsync(text, source, target);

                return Ok(new
                {
                    success = true,
                    originalText = text,
                    translatedText = translatedText,
                    sourceLang = source,
                    targetLang = target,
                    timestamp = DateTime.UtcNow,
                    api = "MyMemory"
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new
                {
                    success = false,
                    error = ex.Message,
                    stackTrace = ex.StackTrace
                });
            }
        }

        // ★★★★★ Массовый перевод ★★★★★
        [HttpPost("batch-translate")]
        public async Task<ActionResult<BatchTranslationResponse>> BatchTranslate([FromBody] BatchTranslateRequest request)
        {
            var results = new List<TranslationResponse>();
            var errors = new List<string>();

            foreach (var sourceStringId in request.SourceStringIds)
            {
                try
                {
                    var sourceString = await _context.SourceStrings.FindAsync(sourceStringId);
                    var locale = await _context.Locales.FindAsync(request.LocaleId);

                    if (sourceString == null || locale == null)
                    {
                        errors.Add($"Source string {sourceStringId} or locale {request.LocaleId} not found");
                        continue;
                    }

                    var translatedText = await _translationService.TranslateAsync(
                        sourceString.Text,
                        "en",
                        locale.Code);

                    var translation = new Translation
                    {
                        SourceStringId = sourceStringId,
                        LocaleId = request.LocaleId,
                        TranslatedText = translatedText,
                        Status = "batch_generated"
                    };

                    _context.Translations.Add(translation);
                    results.Add(new TranslationResponse
                    {
                        Success = true,
                        TranslationId = translation.Id,
                        OriginalText = sourceString.Text,
                        TranslatedText = translatedText,
                        SourceLang = "en",
                        TargetLang = locale.Code
                    });
                }
                catch (Exception ex)
                {
                    errors.Add($"Error translating string {sourceStringId}: {ex.Message}");
                }
            }

            await _context.SaveChangesAsync();

            return Ok(new BatchTranslationResponse
            {
                Success = errors.Count == 0,
                TranslatedCount = results.Count,
                ErrorCount = errors.Count,
                Results = results,
                Errors = errors
            });
        }

        private bool TranslationExists(int id)
        {
            return _context.Translations.Any(e => e.Id == id);
        }
    }

    // ★★★★★ Классы для запросов ★★★★★

    public class AutoTranslateRequest
    {
        public int SourceStringId { get; set; }
        public int LocaleId { get; set; }
    }

    public class TranslateTextRequest
    {
        public string Text { get; set; } = string.Empty;
        public string SourceLang { get; set; } = "en";
        public string TargetLang { get; set; } = "ru";
        public bool SaveToDatabase { get; set; } = false;
        public int? SourceStringId { get; set; }
    }

    public class BatchTranslateRequest
    {
        public List<int> SourceStringIds { get; set; } = new();
        public int LocaleId { get; set; }
    }

    public class UpdateStatusRequest
    {
        public string Status { get; set; } = string.Empty;
    }

    public class TranslationResponse
    {
        public bool Success { get; set; }
        public int? TranslationId { get; set; }
        public string OriginalText { get; set; } = string.Empty;
        public string TranslatedText { get; set; } = string.Empty;
        public string SourceLang { get; set; } = string.Empty;
        public string TargetLang { get; set; } = string.Empty;
        public string? Error { get; set; }
    }

    public class BatchTranslationResponse
    {
        public bool Success { get; set; }
        public int TranslatedCount { get; set; }
        public int ErrorCount { get; set; }
        public List<TranslationResponse> Results { get; set; } = new();
        public List<string> Errors { get; set; } = new();
    }

}