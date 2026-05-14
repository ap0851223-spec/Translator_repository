// Services/LanguageDetectionService.cs
namespace Project_translator.Services
{
    public interface ILanguageDetectionService
    {
        string DetectLanguage(string text);
        double GetConfidence(string text, string language);
        List<(string language, double confidence)> DetectAllLanguages(string text);
    }

    public class LanguageDetectionService : ILanguageDetectionService
    {
        private readonly ILogger<LanguageDetectionService> _logger;

        // Характерные символы и паттерны для каждого языка
        private static readonly Dictionary<string, LanguagePattern> LanguagePatterns = new()
        {
            ["zh"] = new LanguagePattern
            {
                Name = "Chinese",
                CharacterRanges = new[] { (0x4E00, 0x9FFF), (0x3400, 0x4DBF) },
                CommonWords = new[] { "的", "是", "在", "我", "有", "和" },
                Weight = 10
            },
            ["ru"] = new LanguagePattern
            {
                Name = "Russian",
                CharacterRanges = new[] { (0x0400, 0x04FF), (0x0500, 0x052F) },
                CommonWords = new[] { "и", "в", "не", "на", "я", "быть", "он" },
                Weight = 5
            },
            ["tt"] = new LanguagePattern
            {
                Name = "Tatar",
                SpecialCharacters = new[] { "ә", "ө", "ү", "җ", "ң", "һ" },
                Weight = 8
            },
            ["tr"] = new LanguagePattern
            {
                Name = "Turkish",
                SpecialCharacters = new[] { "ğ", "ü", "ş", "ı", "ö", "ç", "Ğ", "Ü", "Ş", "İ", "Ö", "Ç" },
                CommonWords = new[] { "ve", "bu", "bir", "de", "da", "için" },
                Weight = 6
            },
            ["de"] = new LanguagePattern
            {
                Name = "German",
                SpecialCharacters = new[] { "ä", "ö", "ü", "ß", "Ä", "Ö", "Ü" },
                CommonWords = new[] { "der", "die", "das", "und", "ist", "ein", "zu", "von" },
                Weight = 4
            },
            ["fr"] = new LanguagePattern
            {
                Name = "French",
                SpecialCharacters = new[] { "à", "â", "æ", "ç", "é", "è", "ê", "ë", "î", "ï", "ô", "œ", "ù", "û", "ü", "ÿ" },
                CommonWords = new[] { "le", "la", "les", "de", "des", "et", "est", "une", "un", "du" },
                Weight = 4
            },
            ["es"] = new LanguagePattern
            {
                Name = "Spanish",
                SpecialCharacters = new[] { "á", "é", "í", "ó", "ú", "ñ", "ü", "¡", "¿" },
                CommonWords = new[] { "el", "la", "de", "que", "y", "en", "un", "ser", "se" },
                Weight = 4
            },
            ["en"] = new LanguagePattern
            {
                Name = "English",
                CommonWords = new[] { "the", "be", "to", "of", "and", "a", "in", "that", "have", "it" },
                Weight = 2
            }
        };

        public LanguageDetectionService(ILogger<LanguageDetectionService> logger)
        {
            _logger = logger;
        }

        public string DetectLanguage(string text)
        {
            var results = DetectAllLanguages(text);
            return results.FirstOrDefault().language ?? "en";
        }

        public double GetConfidence(string text, string language)
        {
            var results = DetectAllLanguages(text);
            var match = results.FirstOrDefault(r => r.language == language);
            return match.confidence;
        }

        public List<(string language, double confidence)> DetectAllLanguages(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return new List<(string, double)> { ("en", 0) };

            var scores = new Dictionary<string, double>();

            foreach (var (langCode, pattern) in LanguagePatterns)
            {
                double score = CalculateScore(text, pattern);
                scores[langCode] = score;
            }

            // Нормализация и сортировка
            double total = scores.Values.Sum();
            if (total == 0) total = 1;

            var results = scores
                .Select(s => (language: s.Key, confidence: Math.Round(s.Value / total, 3)))
                .OrderByDescending(r => r.confidence)
                .ToList();

            _logger.LogDebug($"Language detection for '{text}': {string.Join(", ", results.Select(r => $"{r.language}:{r.confidence}"))}");

            return results;
        }

        private double CalculateScore(string text, LanguagePattern pattern)
        {
            double score = 0;

            // Проверка диапазонов символов
            if (pattern.CharacterRanges != null)
            {
                foreach (var (start, end) in pattern.CharacterRanges)
                {
                    score += text.Count(c => c >= start && c <= end) * 2;
                }
            }

            // Проверка специальных символов
            if (pattern.SpecialCharacters != null)
            {
                score += text.Count(c => pattern.SpecialCharacters.Contains(c.ToString())) * 3;
            }

            // Проверка общих слов
            if (pattern.CommonWords != null)
            {
                var words = text.ToLower().Split(new[] { ' ', ',', '.', '!', '?', ';', ':' }, StringSplitOptions.RemoveEmptyEntries);
                score += words.Count(w => pattern.CommonWords.Contains(w)) * 4;
            }

            // Применяем вес языка
            score *= pattern.Weight;

            return score;
        }
    }

    public class LanguagePattern
    {
        public string Name { get; set; } = string.Empty;
        public (int start, int end)[]? CharacterRanges { get; set; }
        public string[]? SpecialCharacters { get; set; }
        public string[]? CommonWords { get; set; }
        public int Weight { get; set; } = 1;
    }
}