using Microsoft.EntityFrameworkCore;
using Project_translator.Data;
using Project_translator.Models;

namespace Project_translator.Services
{
    public interface ITranslationMemoryService
    {
        // Оригинальный метод (для обратной совместимости)
        Task<string?> FindInMemoryAsync(string text, string sourceLang, string targetLang);

        // Новый метод с дополнительными параметрами (опционально)
        Task<string?> FindInMemoryAsync(string text, string sourceLang, string targetLang, int? projectId = null, string? context = null);

        Task AddToMemoryAsync(string sourceText, string targetText, string sourceLang, string targetLang);
        Task IncrementUsageAsync(int memoryId);
    }

    public class TranslationMemoryService : ITranslationMemoryService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<TranslationMemoryService> _logger;

        public TranslationMemoryService(
            ApplicationDbContext context,
            ILogger<TranslationMemoryService> logger)
        {
            _context = context;
            _logger = logger;
        }

        // Реализация метода с дополнительными параметрами
        public async Task<string?> FindInMemoryAsync(string text, string sourceLang, string targetLang, int? projectId = null, string? context = null)
        {
            try
            {
                _logger.LogInformation($"Поиск в памяти переводов: '{text}' ({sourceLang}→{targetLang}) Project: {projectId}");

                // Базовый запрос
                var query = _context.TranslationMemories
                    .Where(tm =>
                        tm.SourceLang == sourceLang &&
                        tm.TargetLang == targetLang);

                // Поиск по точному совпадению текста
                var exactMatch = await query
                    .Where(tm => tm.SourceText.ToLower() == text.ToLower())
                    .FirstOrDefaultAsync();

                if (exactMatch != null)
                {
                    _logger.LogInformation($"Найдено точное совпадение: '{text}' → '{exactMatch.TargetText}'");
                    await UpdateUsageAsync(exactMatch);
                    return exactMatch.TargetText;
                }

                // Поиск по частичному совпадению (если текст содержит сохраненный фрагмент)
                var partialMatches = await query
                    .Where(tm => text.ToLower().Contains(tm.SourceText.ToLower()))
                    .OrderByDescending(tm => tm.SourceText.Length) // Более длинные совпадения приоритетнее
                    .ThenByDescending(tm => tm.UsageCount)
                    .ToListAsync();

                if (partialMatches.Any())
                {
                    var bestMatch = partialMatches.First();
                    _logger.LogInformation($"Найдено частичное совпадение: '{bestMatch.SourceText}' → '{bestMatch.TargetText}'");
                    await UpdateUsageAsync(bestMatch);

                    // Заменяем найденный фрагмент в тексте
                    var result = text.Replace(bestMatch.SourceText, bestMatch.TargetText, StringComparison.OrdinalIgnoreCase);
                    return result;
                }

                _logger.LogInformation($"Не найдено совпадений для: '{text}'");
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при поиске в памяти переводов");
                return null;
            }
        }

        // Реализация оригинального метода (для обратной совместимости)
        public async Task<string?> FindInMemoryAsync(string text, string sourceLang, string targetLang)
        {
            return await FindInMemoryAsync(text, sourceLang, targetLang, null, null);
        }

        // Обновление счетчика использования
        private async Task UpdateUsageAsync(TranslationMemory memory)
        {
            try
            {
                memory.UsageCount++;
                memory.LastUsed = DateTime.UtcNow;
                await _context.SaveChangesAsync();
                _logger.LogDebug($"Обновлен счетчик использования для записи {memory.Id}: {memory.UsageCount}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при обновлении счетчика использования");
            }
        }

        public async Task AddToMemoryAsync(string sourceText, string targetText, string sourceLang, string targetLang)
        {
            try
            {
                _logger.LogInformation($"Добавление в память переводов: '{sourceText}' → '{targetText}' ({sourceLang}→{targetLang})");

                // Проверяем, нет ли уже такой записи
                var existing = await _context.TranslationMemories
                    .FirstOrDefaultAsync(tm =>
                        tm.SourceText.ToLower() == sourceText.ToLower() &&
                        tm.SourceLang == sourceLang &&
                        tm.TargetLang == targetLang);

                if (existing == null)
                {
                    var memory = new TranslationMemory
                    {
                        SourceText = sourceText,
                        TargetText = targetText,
                        SourceLang = sourceLang,
                        TargetLang = targetLang,
                        UsageCount = 1,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    };

                    _context.TranslationMemories.Add(memory);
                    await _context.SaveChangesAsync();

                    _logger.LogInformation($"✅ Добавлена новая запись в память переводов (ID: {memory.Id})");
                }
                else
                {
                    // Если запись уже есть, обновляем счетчик
                    existing.UsageCount++;
                    existing.UpdatedAt = DateTime.UtcNow;
                    await _context.SaveChangesAsync();

                    _logger.LogInformation($"↻ Обновлена существующая запись в памяти переводов (ID: {existing.Id})");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Ошибка при добавлении в память переводов");
            }
        }

        public async Task IncrementUsageAsync(int memoryId)
        {
            try
            {
                var memory = await _context.TranslationMemories.FindAsync(memoryId);
                if (memory != null)
                {
                    memory.UsageCount++;
                    memory.LastUsed = DateTime.UtcNow;
                    await _context.SaveChangesAsync();
                    _logger.LogDebug($"Счетчик использования увеличен для записи {memoryId}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при увеличении счетчика использования");
            }
        }
    }
}