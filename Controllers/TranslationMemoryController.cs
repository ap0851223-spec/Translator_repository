using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Project_translator.Data;
using Project_translator.Models;

namespace Project_translator.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class TranslationMemoryController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<TranslationMemoryController> _logger;

        public TranslationMemoryController(
            ApplicationDbContext context,
            ILogger<TranslationMemoryController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // GET: api/TranslationMemory/history?source=en&target=tt&limit=10
        [HttpGet("history")]
        public async Task<ActionResult> GetHistory(
            [FromQuery] string? source = null,
            [FromQuery] string? target = null,
            [FromQuery] int limit = 10)
        {
            try
            {
                var query = _context.TranslationMemories.AsQueryable();

                if (!string.IsNullOrEmpty(source))
                {
                    query = query.Where(m => m.SourceLang == source.ToLower());
                }

                if (!string.IsNullOrEmpty(target))
                {
                    query = query.Where(m => m.TargetLang == target.ToLower());
                }

                var memories = await query
                    .OrderByDescending(m => m.UsageCount)
                    .ThenByDescending(m => m.CreatedAt)
                    .Take(limit)
                    .Select(m => new
                    {
                        m.Id,
                        m.SourceText,
                        m.TargetText,
                        m.SourceLang,
                        m.TargetLang,
                        m.UsageCount,
                        m.CreatedAt,
                        LastUsed = m.CreatedAt.ToString("yyyy-MM-dd HH:mm")
                    })
                    .ToListAsync();

                return Ok(new
                {
                    success = true,
                    count = memories.Count,
                    history = memories,
                    filters = new { source, target, limit }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting translation history");
                return StatusCode(500, new { success = false, error = ex.Message });
            }
        }

        // GET: api/TranslationMemory/stats
        [HttpGet("stats")]
        public async Task<ActionResult> GetStats()
        {
            try
            {
                var totalMemories = await _context.TranslationMemories.CountAsync();
                var totalUsage = await _context.TranslationMemories.SumAsync(m => m.UsageCount);
                var topLanguages = await _context.TranslationMemories
                    .GroupBy(m => new { m.SourceLang, m.TargetLang })
                    .Select(g => new
                    {
                        source = g.Key.SourceLang,
                        target = g.Key.TargetLang,
                        count = g.Count(),
                        totalUsage = g.Sum(m => m.UsageCount)
                    })
                    .OrderByDescending(x => x.totalUsage)
                    .Take(10)
                    .ToListAsync();

                return Ok(new
                {
                    success = true,
                    totalMemories,
                    totalUsage,
                    averageUsage = totalMemories > 0 ? totalUsage / totalMemories : 0,
                    topLanguagePairs = topLanguages
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting memory stats");
                return StatusCode(500, new { success = false, error = ex.Message });
            }
        }

        // DELETE: api/TranslationMemory/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteMemory(int id)
        {
            var memory = await _context.TranslationMemories.FindAsync(id);
            if (memory == null)
            {
                return NotFound();
            }

            _context.TranslationMemories.Remove(memory);
            await _context.SaveChangesAsync();

            return Ok(new { success = true, message = "Запись удалена из памяти переводов" });
        }

        // POST: api/TranslationMemory/clear
        [HttpPost("clear")]
        public async Task<IActionResult> ClearMemory([FromBody] ClearMemoryRequest? request = null)
        {
            try
            {
                var query = _context.TranslationMemories.AsQueryable();

                if (request != null)
                {
                    if (!string.IsNullOrEmpty(request.SourceLang))
                        query = query.Where(m => m.SourceLang == request.SourceLang);
                    if (!string.IsNullOrEmpty(request.TargetLang))
                        query = query.Where(m => m.TargetLang == request.TargetLang);
                }

                var count = await query.CountAsync();
                _context.TranslationMemories.RemoveRange(query);
                await _context.SaveChangesAsync();

                return Ok(new { success = true, deletedCount = count, message = $"Удалено {count} записей" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, error = ex.Message });
            }
        }
    }

    public class ClearMemoryRequest
    {
        public string? SourceLang { get; set; }
        public string? TargetLang { get; set; }
    }
}