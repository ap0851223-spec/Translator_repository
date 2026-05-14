using System;
using System.Net.Http;
using System.Threading.Tasks;
using System.Text.Json;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
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

            // Автоопределение
            if (sourceLang == "auto")
            {
                sourceLang = DetectLanguage(text);
                _logger.LogInformation($"🔍 Определен язык: {sourceLang}");
            }

            if (!supportedLangs.Contains(sourceLang) || !supportedLangs.Contains(targetLang))
            {
                return text;
            }

            if (sourceLang == targetLang)
            {
                return text;
            }

            try
            {
                // 1. Память переводов
                var memoryResult = await _memoryService.FindInMemoryAsync(text, sourceLang, targetLang);
                if (memoryResult != null)
                {
                    _logger.LogInformation($"💾 Найдено в памяти: '{memoryResult}'");
                    return memoryResult;
                }

                // 2. Локальный словарь
                string dictKey = $"{sourceLang}-{targetLang}";
                if (_dictionaries.TryGetValue(dictKey, out var dict))
                {
                    if (dict.TryGetValue(text.Trim(), out var translation))
                    {
                        _logger.LogInformation($"📖 Найдено в словаре: '{translation}'");
                        await _memoryService.AddToMemoryAsync(text, translation, sourceLang, targetLang);
                        return translation;
                    }
                }

                // 3. Пробуем Google Translate (через неофициальное API)
                string result = await TryGoogleTranslate(text, sourceLang, targetLang);

                if (!string.IsNullOrEmpty(result) && result != text)
                {
                    _logger.LogInformation($"✅ Google Translate: '{result}'");
                    await _memoryService.AddToMemoryAsync(text, result, sourceLang, targetLang);
                    return result;
                }

                // 4. Пробуем MyMemory API
                result = await TryMyMemoryAPI(text, sourceLang, targetLang);

                if (!string.IsNullOrEmpty(result) && result != text)
                {
                    _logger.LogInformation($"✅ MyMemory: '{result}'");
                    await _memoryService.AddToMemoryAsync(text, result, sourceLang, targetLang);
                    return result;
                }

                // 5. Fallback: перевод через английский
                if (sourceLang != "en" && targetLang != "en")
                {
                    _logger.LogInformation("🔄 Пробуем через английский...");
                    var toEnglish = await TryGoogleTranslate(text, sourceLang, "en");
                    if (toEnglish != text)
                    {
                        var fromEnglish = await TryGoogleTranslate(toEnglish, "en", targetLang);
                        if (fromEnglish != toEnglish)
                        {
                            await _memoryService.AddToMemoryAsync(text, fromEnglish, sourceLang, targetLang);
                            return fromEnglish;
                        }
                    }
                }

                // Если ничего не сработало
                _logger.LogWarning($"⚠️ Не удалось перевести: '{text}'");
                return text;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"❌ Ошибка перевода: '{text}'");
                return text;
            }
        }

        // ===== GOOGLE TRANSLATE (неофициальное API) =====
        private async Task<string> TryGoogleTranslate(string text, string sourceLang, string targetLang)
        {
            try
            {
                // Нормализуем коды для Google
                string glSource = sourceLang switch
                {
                    "tt" => "ru", // Татарский через русский
                    "auto" => "auto",
                    _ => sourceLang
                };

                string glTarget = targetLang switch
                {
                    "tt" => "ru", // Татарский через русский
                    _ => targetLang
                };

                // Если после нормализации языки совпадают - пропускаем
                if (glSource == glTarget)
                {
                    return text;
                }

                string url = $"https://translate.googleapis.com/translate_a/single?client=gtx&sl={glSource}&tl={glTarget}&dt=t&q={Uri.EscapeDataString(text)}";

                _logger.LogInformation($"🌐 Google Translate: {url}");

                var response = await _httpClient.GetAsync(url);
                response.EnsureSuccessStatusCode();

                string json = await response.Content.ReadAsStringAsync();

                // Парсим ответ Google (это массив)
                using var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;

                if (root.GetArrayLength() > 0 && root[0].GetArrayLength() > 0)
                {
                    var firstSegment = root[0][0];
                    if (firstSegment.GetArrayLength() > 0)
                    {
                        string translatedText = firstSegment[0].GetString() ?? text;

                        if (!string.IsNullOrWhiteSpace(translatedText) && translatedText != text)
                        {
                            _logger.LogInformation($"✅ Google: '{translatedText}'");

                            // Для татарского - добавляем известные переводы
                            if (targetLang == "tt")
                            {
                                translatedText = ApplyTatarFixes(text, translatedText);
                            }

                            return translatedText;
                        }
                    }
                }

                return text;
            }
            catch (Exception ex)
            {
                _logger.LogWarning($"⚠️ Google Translate ошибка: {ex.Message}");
                return text;
            }
        }

        // ===== MYMEMORY API (резервный) =====
        private async Task<string> TryMyMemoryAPI(string text, string sourceLang, string targetLang)
        {
            try
            {
                string encodedText = Uri.EscapeDataString(text);
                string langPair = $"{sourceLang}|{targetLang}";
                string url = $"https://api.mymemory.translated.net/get?q={encodedText}&langpair={langPair}";

                _logger.LogInformation($"🌐 MyMemory: {url}");

                var response = await _httpClient.GetAsync(url);
                response.EnsureSuccessStatusCode();

                string json = await response.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;

                if (root.TryGetProperty("responseData", out var responseData))
                {
                    if (responseData.TryGetProperty("translatedText", out var translatedText))
                    {
                        string result = translatedText.GetString() ?? text;

                        // Очищаем от предупреждений
                        if (result.Contains("MYMEMORY WARNING:"))
                        {
                            result = result.Split("MYMEMORY WARNING:")[0].Trim();
                        }

                        result = result.Trim();

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
                _logger.LogWarning($"⚠️ MyMemory ошибка: {ex.Message}");
                return text;
            }
        }

        // ===== СПЕЦИАЛЬНЫЕ ИСПРАВЛЕНИЯ ДЛЯ ТАТАРСКОГО =====
        private string ApplyTatarFixes(string sourceText, string translatedText)
        {
            if (string.IsNullOrEmpty(translatedText))
                return sourceText;

            // Расширенный словарь татарских переводов
            var tatarDictionary = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                // Приветствия
                ["Здравствуйте"] = "Исәнмесез",
                ["Здравствуй"] = "Исәнме",
                ["Привет"] = "Сәлам",
                ["Доброе утро"] = "Хәерле иртә",
                ["Добрый день"] = "Хәерле көн",
                ["Добрый вечер"] = "Хәерле кич",
                ["До свидания"] = "Сау булыгыз",
                ["Пока"] = "Сау бул",

                // Благодарности
                ["Спасибо"] = "Рәхмәт",
                ["Большое спасибо"] = "Зур рәхмәт",
                ["Пожалуйста"] = "Зинһар",
                ["Извините"] = "Гафу итегез",
                ["Простите"] = "Гафу ит",

                // Базовые слова
                ["Да"] = "Әйе",
                ["Нет"] = "Юк",
                ["Хорошо"] = "Яхшы",
                ["Плохо"] = "Начар",
                ["Красивый"] = "Матур",
                ["Большой"] = "Зур",
                ["Маленький"] = "Кечкенә",

                // Люди
                ["Человек"] = "Кеше",
                ["Мужчина"] = "Ир",
                ["Женщина"] = "Хатын",
                ["Ребенок"] = "Бала",
                ["Друг"] = "Дус",
                ["Семья"] = "Гаилә",
                ["Мама"] = "Әни",
                ["Папа"] = "Әти",

                // Еда
                ["Хлеб"] = "Икмәк",
                ["Вода"] = "Су",
                ["Молоко"] = "Сөт",
                ["Мясо"] = "Ит",
                ["Чай"] = "Чәй",

                // Цвета
                ["Белый"] = "Ак",
                ["Черный"] = "Кара",
                ["Красный"] = "Кызыл",
                ["Синий"] = "Зәңгәр",
                ["Зеленый"] = "Яшел",
                ["Желтый"] = "Сары",

                // Числа
                ["Один"] = "Бер",
                ["Два"] = "Ике",
                ["Три"] = "Өч",
                ["Четыре"] = "Дүрт",
                ["Пять"] = "Биш",

                // Погода
                ["Солнце"] = "Кояш",
                ["Дождь"] = "Яңгыр",
                ["Снег"] = "Кар",
                ["Ветер"] = "Җил",

                // Действия
                ["Идти"] = "Барырга",
                ["Есть"] = "Ашарга",
                ["Пить"] = "Эчәргә",
                ["Спать"] = "Йокларга",
                ["Говорить"] = "Сөйләргә",
                ["Читать"] = "Укырга",
                ["Писать"] = "Язарга",
                ["Любить"] = "Яратырга",

                // Места
                ["Дом"] = "Өй",
                ["Школа"] = "Мәктәп",
                ["Больница"] = "Хастаханә",
                ["Магазин"] = "Кибет",
                ["Город"] = "Шәһәр",
                ["Деревня"] = "Авыл",

                // Время
                ["Сегодня"] = "Бүген",
                ["Завтра"] = "Иртәгә",
                ["Вчера"] = "Кичә",
                ["Утро"] = "Иртә",
                ["День"] = "Көн",
                ["Вечер"] = "Кич",
                ["Ночь"] = "Төн",

                // Английский → Татарский
                ["Hello"] = "Сәлам",
                ["Goodbye"] = "Сау булыгыз",
                ["Thank you"] = "Рәхмәт",
                ["Please"] = "Зинһар",
                ["Yes"] = "Әйе",
                ["No"] = "Юк",
                ["Good"] = "Яхшы",
                ["Bad"] = "Начар",
                ["Friend"] = "Дус",
                ["Family"] = "Гаилә",
                ["Love"] = "Мәхәббәт",
                ["Mother"] = "Әни",
                ["Father"] = "Әти",
                ["Water"] = "Су",
                ["Bread"] = "Икмәк",
                ["Home"] = "Өй",
                ["School"] = "Мәктәп"
            };

            // Сначала проверяем полное совпадение с исходным текстом
            if (tatarDictionary.TryGetValue(sourceText.Trim(), out var exactMatch))
            {
                return exactMatch;
            }

            // Если перевод пришел на русском (для татарского), 
            // пробуем заменить русские слова на татарские
            if (IsRussianText(translatedText))
            {
                var words = translatedText.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                var translatedWords = new List<string>();

                foreach (var word in words)
                {
                    var cleanWord = word.TrimEnd('.', ',', '!', '?', ':', ';');

                    if (tatarDictionary.TryGetValue(cleanWord, out var tatarWord))
                    {
                        translatedWords.Add(tatarWord);
                    }
                    else
                    {
                        translatedWords.Add(cleanWord);
                    }
                }

                string result = string.Join(" ", translatedWords);
                if (result != translatedText)
                {
                    _logger.LogInformation($"🔧 Татарский перевод исправлен: '{translatedText}' → '{result}'");
                }

                return result;
            }

            return translatedText;
        }

        private bool IsRussianText(string text)
        {
            return text.Any(c => c >= 'А' && c <= 'я');
        }

        // ===== ОПРЕДЕЛЕНИЕ ЯЗЫКА =====
        private string DetectLanguage(string text)
        {
            if (string.IsNullOrEmpty(text)) return "en";

            if (text.Any(c => c >= 0x4E00 && c <= 0x9FFF))
                return "zh";

            if (text.Any(c => "ğüşıöçĞÜŞİÖÇ".Contains(c)))
                return "tr";

            if (text.Any(c => "äöüßÄÖÜ".Contains(c)))
                return "de";

            if (text.Any(c => "àâæçéèêëîïôœùûüÿÀÂÆÇÉÈÊËÎÏÔŒÙÛÜŸ".Contains(c)))
                return "fr";

            if (text.Any(c => "áéíóúñü¡¿ÁÉÍÓÚÑÜ".Contains(c)))
                return "es";

            if (text.Contains("ә") || text.Contains("ө") || text.Contains("ү") ||
                text.Contains("җ") || text.Contains("ң") || text.Contains("һ"))
                return "tt";

            int cyrillicCount = text.Count(c => (c >= 'А' && c <= 'я') || c == 'ё' || c == 'Ё');
            int latinCount = text.Count(c => (c >= 'A' && c <= 'Z') || (c >= 'a' && c <= 'z'));

            return cyrillicCount > latinCount ? "ru" : "en";
        }

        // ===== НОРМАЛИЗАЦИЯ =====
        private string NormalizeLangCode(string langCode)
        {
            return langCode?.ToLowerInvariant().Trim() switch
            {
                "auto" => "auto",
                "eng" or "english" => "en",
                "rus" or "russian" => "ru",
                "tat" or "tatar" or "тат" or "татар" => "tt",
                "spa" or "spanish" or "español" => "es",
                "fra" or "french" or "français" => "fr",
                "deu" or "german" or "deutsch" => "de",
                "tur" or "turkish" or "türkçe" => "tr",
                "chi" or "zho" or "chinese" or "中文" => "zh",
                _ => langCode
            };
        }

        // ===== ЛОКАЛЬНЫЕ СЛОВАРИ (базовые) =====
        private static readonly Dictionary<string, Dictionary<string, string>> _dictionaries = new()
        {
            ["en-ru"] = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["Hello"] = "Привет",
                ["Goodbye"] = "До свидания",
                ["Thank you"] = "Спасибо",
                ["Please"] = "Пожалуйста"
            },
            ["de-ru"] = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["Hallo"] = "Привет",
                ["Danke"] = "Спасибо",
                ["Ja"] = "Да"
            },
            ["en-de"] = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["Hello"] = "Hallo",
                ["Thank you"] = "Danke",
                ["Yes"] = "Ja"
            },
            ["en-fr"] = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["Hello"] = "Bonjour",
                ["Thank you"] = "Merci",
                ["Yes"] = "Oui"
            },
            ["en-es"] = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["Hello"] = "Hola",
                ["Thank you"] = "Gracias",
                ["Yes"] = "Sí"
            },
            ["en-tr"] = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["Hello"] = "Merhaba",
                ["Thank you"] = "Teşekkürler",
                ["Yes"] = "Evet"
            },
            ["en-zh"] = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["Hello"] = "你好",
                ["Thank you"] = "谢谢",
                ["Yes"] = "是"
            },
            ["ru-en"] = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["Привет"] = "Hello",
                ["Спасибо"] = "Thank you",
                ["Да"] = "Yes"
            }
        };
    }
}