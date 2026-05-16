using System;
using System.Net.Http;
using System.Threading.Tasks;
using System.Text.Json;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Project_translator.Services
{
    public class MyMemoryTranslationService : ITranslationService
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<MyMemoryTranslationService> _logger;
        private readonly ITranslationMemoryService _memoryService;

        public MyMemoryTranslationService(
            HttpClient httpClient,
            ILogger<MyMemoryTranslationService> logger,
            ITranslationMemoryService memoryService)
        {
            _httpClient = httpClient;
            _logger = logger;
            _memoryService = memoryService;
        }

        public async Task<string> TranslateAsync(string text, string sourceLang, string targetLang)
        {
            if (string.IsNullOrWhiteSpace(text))
                return text;

            sourceLang = NormalizeLangCode(sourceLang);
            targetLang = NormalizeLangCode(targetLang);

            _logger.LogInformation($"🔄 Перевод: '{text}' ({sourceLang} → {targetLang})");

            var supportedLangs = new[] { "en", "ru", "tt", "es", "fr", "de", "tr", "zh" };

            if (sourceLang == "auto")
            {
                sourceLang = DetectLanguage(text);
                _logger.LogInformation($"🔍 Определен: {sourceLang}");
            }

            if (!supportedLangs.Contains(sourceLang) || !supportedLangs.Contains(targetLang))
                return text;

            if (sourceLang == targetLang)
                return text;

            try
            {
                // 1. Память переводов (БД)
                var memoryResult = await _memoryService.FindInMemoryAsync(text, sourceLang, targetLang);
                if (memoryResult != null && memoryResult != text)
                {
                    _logger.LogInformation($"💾 Из памяти: {memoryResult}");
                    return memoryResult;
                }

                // 2. Пробуем ВСЕ доступные API по очереди
                string result = await TryAllTranslationAPIs(text, sourceLang, targetLang);

                if (result != text && !string.IsNullOrWhiteSpace(result))
                {
                    await _memoryService.AddToMemoryAsync(text, result, sourceLang, targetLang);
                    return result;
                }

                // 3. Последняя попытка - через промежуточный язык
                if (sourceLang != "en" && targetLang != "en")
                {
                    _logger.LogInformation("🔄 Пробуем через английский...");
                    var toEnglish = await TranslateViaGoogle(text, sourceLang, "en");
                    if (toEnglish != text)
                    {
                        var fromEnglish = await TranslateViaGoogle(toEnglish, "en", targetLang);
                        if (fromEnglish != toEnglish && fromEnglish != text)
                        {
                            await _memoryService.AddToMemoryAsync(text, fromEnglish, sourceLang, targetLang);
                            return fromEnglish;
                        }
                    }
                }

                _logger.LogWarning($"⚠️ Не удалось перевести: '{text}'");
                return text;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"❌ Ошибка: {text}");
                return text;
            }
        }

        // ===== ПРОБУЕМ ВСЕ API =====
        private async Task<string> TryAllTranslationAPIs(string text, string sourceLang, string targetLang)
        {
            // 1. Google Translate (основной)
            var result = await TranslateViaGoogle(text, sourceLang, targetLang);
            if (result != text && !string.IsNullOrWhiteSpace(result))
                return result;

            // 2. Microsoft Translator (резервный)
            result = await TranslateViaMicrosoft(text, sourceLang, targetLang);
            if (result != text && !string.IsNullOrWhiteSpace(result))
                return result;

            // 3. MyMemory (ещё один резервный)
            result = await TranslateViaMyMemory(text, sourceLang, targetLang);
            if (result != text && !string.IsNullOrWhiteSpace(result))
                return result;

            // 4. LibreTranslate (последний шанс)
            result = await TranslateViaLibre(text, sourceLang, targetLang);
            if (result != text && !string.IsNullOrWhiteSpace(result))
                return result;

            return text;
        }

        // ===== GOOGLE TRANSLATE =====
        private async Task<string> TranslateViaGoogle(string text, string sourceLang, string targetLang)
        {
            try
            {
                // Для татарского Google использует "tt"
                string glSource = sourceLang == "tt" ? "tt" : sourceLang;
                string glTarget = targetLang == "tt" ? "tt" : targetLang;

                string url = $"https://translate.googleapis.com/translate_a/single?client=gtx&sl={glSource}&tl={glTarget}&dt=t&dt=bd&dj=1&q={Uri.EscapeDataString(text)}";

                var response = await _httpClient.GetStringAsync(url);
                using var doc = JsonDocument.Parse(response);

                // Новый формат ответа Google (с dj=1)
                if (doc.RootElement.TryGetProperty("sentences", out var sentences))
                {
                    var result = new System.Text.StringBuilder();
                    foreach (var sentence in sentences.EnumerateArray())
                    {
                        if (sentence.TryGetProperty("trans", out var trans))
                        {
                            result.Append(trans.GetString());
                        }
                    }

                    string translated = result.ToString().Trim();
                    if (!string.IsNullOrWhiteSpace(translated) &&
                        !translated.Equals(text, StringComparison.OrdinalIgnoreCase))
                    {
                        _logger.LogInformation($"✅ Google: '{translated}'");
                        return translated;
                    }
                }

                // Старый формат (fallback)
                if (doc.RootElement.ValueKind == JsonValueKind.Array &&
                    doc.RootElement.GetArrayLength() > 0)
                {
                    var firstArray = doc.RootElement[0];
                    if (firstArray.GetArrayLength() > 0)
                    {
                        var result = new System.Text.StringBuilder();
                        foreach (var item in firstArray.EnumerateArray())
                        {
                            if (item.GetArrayLength() > 0 && item[0].ValueKind == JsonValueKind.String)
                            {
                                result.Append(item[0].GetString());
                            }
                        }

                        string translated = result.ToString().Trim();
                        if (!string.IsNullOrWhiteSpace(translated) &&
                            !translated.Equals(text, StringComparison.OrdinalIgnoreCase))
                        {
                            _logger.LogInformation($"✅ Google (old): '{translated}'");
                            return translated;
                        }
                    }
                }

                return text;
            }
            catch (Exception ex)
            {
                _logger.LogWarning($"⚠️ Google error: {ex.Message}");
                return text;
            }
        }

        // ===== MICROSOFT TRANSLATOR (БЕСПЛАТНЫЙ) =====
        private async Task<string> TranslateViaMicrosoft(string text, string sourceLang, string targetLang)
        {
            try
            {
                // Маппинг кодов для Microsoft
                var msMap = new Dictionary<string, string>
                {
                    ["en"] = "en",
                    ["ru"] = "ru",
                    ["tt"] = "tt",
                    ["es"] = "es",
                    ["fr"] = "fr",
                    ["de"] = "de",
                    ["tr"] = "tr",
                    ["zh"] = "zh-Hans"
                };

                string msSource = msMap.GetValueOrDefault(sourceLang, sourceLang);
                string msTarget = msMap.GetValueOrDefault(targetLang, targetLang);

                // Microsoft Translator бесплатный endpoint
                string url = $"https://api.microsofttranslator.com/V2/Http.svc/Translate?text={Uri.EscapeDataString(text)}&from={msSource}&to={msTarget}";

                var response = await _httpClient.GetStringAsync(url);

                // Извлекаем перевод из XML-ответа
                var startTag = "<string xmlns=\"http://schemas.microsoft.com/2003/10/Serialization/\">";
                var endTag = "</string>";

                if (response.Contains(startTag))
                {
                    int startIndex = response.IndexOf(startTag) + startTag.Length;
                    int endIndex = response.IndexOf(endTag);

                    string translated = response.Substring(startIndex, endIndex - startIndex);

                    if (!string.IsNullOrWhiteSpace(translated) &&
                        !translated.Equals(text, StringComparison.OrdinalIgnoreCase))
                    {
                        _logger.LogInformation($"✅ Microsoft: '{translated}'");
                        return translated;
                    }
                }

                return text;
            }
            catch (Exception ex)
            {
                _logger.LogWarning($"⚠️ Microsoft error: {ex.Message}");
                return text;
            }
        }

        // ===== MYMEMORY (существующий) =====
        private async Task<string> TranslateViaMyMemory(string text, string sourceLang, string targetLang)
        {
            try
            {
                string url = $"https://api.mymemory.translated.net/get?q={Uri.EscapeDataString(text)}&langpair={sourceLang}|{targetLang}&de=demo@example.com";

                var response = await _httpClient.GetStringAsync(url);
                using var doc = JsonDocument.Parse(response);

                if (doc.RootElement.TryGetProperty("responseData", out var data))
                {
                    if (data.TryGetProperty("translatedText", out var translated))
                    {
                        string result = translated.GetString()?.Trim() ?? text;

                        if (result.Contains("MYMEMORY WARNING:"))
                            result = result.Split("MYMEMORY WARNING:")[0].Trim();

                        if (!string.IsNullOrWhiteSpace(result) &&
                            !result.Equals(text, StringComparison.OrdinalIgnoreCase))
                        {
                            _logger.LogInformation($"✅ MyMemory: '{result}'");
                            return result;
                        }
                    }
                }

                return text;
            }
            catch (Exception ex)
            {
                _logger.LogWarning($"⚠️ MyMemory error: {ex.Message}");
                return text;
            }
        }

        // ===== LIBRETRANSLATE (бесплатный, без API ключа) =====
        private async Task<string> TranslateViaLibre(string text, string sourceLang, string targetLang)
        {
            try
            {
                // Маппинг для LibreTranslate
                var libreMap = new Dictionary<string, string>
                {
                    ["en"] = "en",
                    ["ru"] = "ru",
                    ["tt"] = "tt",
                    ["es"] = "es",
                    ["fr"] = "fr",
                    ["de"] = "de",
                    ["tr"] = "tr",
                    ["zh"] = "zh"
                };

                string libreSource = libreMap.GetValueOrDefault(sourceLang, sourceLang);
                string libreTarget = libreMap.GetValueOrDefault(targetLang, targetLang);

                var content = new FormUrlEncodedContent(new[]
                {
                    new KeyValuePair<string, string>("q", text),
                    new KeyValuePair<string, string>("source", libreSource),
                    new KeyValuePair<string, string>("target", libreTarget),
                    new KeyValuePair<string, string>("format", "text")
                });

                var response = await _httpClient.PostAsync("https://libretranslate.de/translate", content);
                var json = await response.Content.ReadAsStringAsync();

                using var doc = JsonDocument.Parse(json);

                if (doc.RootElement.TryGetProperty("translatedText", out var translated))
                {
                    string result = translated.GetString()?.Trim() ?? text;

                    if (!string.IsNullOrWhiteSpace(result) &&
                        !result.Equals(text, StringComparison.OrdinalIgnoreCase))
                    {
                        _logger.LogInformation($"✅ LibreTranslate: '{result}'");
                        return result;
                    }
                }

                return text;
            }
            catch (Exception ex)
            {
                _logger.LogWarning($"⚠️ LibreTranslate error: {ex.Message}");
                return text;
            }
        }

        // ===== ОПРЕДЕЛЕНИЕ ЯЗЫКА =====
        private string DetectLanguage(string text)
        {
            if (string.IsNullOrEmpty(text)) return "en";

            if (text.Any(c => c >= 0x4E00 && c <= 0x9FFF)) return "zh";
            if (text.Any(c => "ğüşıöçĞÜŞİÖÇ".Contains(c))) return "tr";
            if (text.Any(c => "äöüßÄÖÜ".Contains(c))) return "de";
            if (text.Any(c => "àâæçéèêëîïôœùûüÿ".Contains(c))) return "fr";
            if (text.Any(c => "áéíóúñü¡¿".Contains(c))) return "es";
            if (text.Contains("ә") || text.Contains("ө") || text.Contains("ү") ||
                text.Contains("җ") || text.Contains("ң") || text.Contains("һ")) return "tt";

            int cyrillic = text.Count(c => (c >= 'А' && c <= 'я') || c == 'ё' || c == 'Ё');
            int latin = text.Count(c => (c >= 'A' && c <= 'Z') || (c >= 'a' && c <= 'z'));

            return cyrillic > latin ? "ru" : "en";
        }

        // ===== НОРМАЛИЗАЦИЯ =====
        private string NormalizeLangCode(string code)
        {
            return code?.ToLowerInvariant() switch
            {
                "auto" => "auto",
                "eng" or "english" => "en",
                "rus" or "russian" => "ru",
                "tat" or "tatar" => "tt",
                "spa" or "spanish" => "es",
                "fra" or "french" => "fr",
                "deu" or "german" => "de",
                "tur" or "turkish" => "tr",
                "chi" or "zho" or "chinese" => "zh",
                _ => code?.ToLower() ?? "en"
            };
        }
    }
}