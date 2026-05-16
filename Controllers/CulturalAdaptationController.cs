using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Project_translator.Data;

namespace Project_translator.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CulturalAdaptationController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public CulturalAdaptationController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet("profile/{langCode}")]
        public async Task<IActionResult> GetProfile(string langCode)
        {
            var locale = await _context.Locales
                .Include(l => l.CulturalProfile)
                .FirstOrDefaultAsync(l => l.Code == langCode);

            if (locale?.CulturalProfile == null)
                return NotFound(new { error = "Profile not found" });

            var profile = locale.CulturalProfile;
            var info = GetDetailedLanguageInfo(locale.Code);

            return Ok(new
            {
                locale = locale.Code,
                localeName = locale.Name,
                currency = profile.Currency,
                dateFormat = profile.DateFormat,
                timeFormat = profile.TimeFormat,
                firstDayOfWeek = profile.FirstDayOfWeek,
                examples = new
                {
                    date = DateTime.Now.ToString(profile.DateFormat),
                    time = DateTime.Now.ToString(profile.TimeFormat)
                },
                languageInfo = info
            });
        }

        private LanguageInfo GetDetailedLanguageInfo(string langCode)
        {
            return langCode switch
            {
                "ru" => new LanguageInfo
                {
                    LanguageName = "Русский",
                    NativeName = "Русский язык",
                    Countries = new[] { "🇷🇺 Россия", "🇧🇾 Беларусь", "🇰🇿 Казахстан", "🇰🇬 Кыргызстан" },
                    Regions = new[] { "Восточная Европа", "Северная Азия", "Центральная Азия" },
                    TotalSpeakers = "≈ 258 млн человек",
                    NativeSpeakers = "≈ 154 млн человек",
                    LanguageFamily = "Индоевропейская → Славянская → Восточнославянская",
                    WritingSystem = "Кириллица",
                    IsOfficialIn = new[] { "Россия", "Беларусь", "Казахстан", "Кыргызстан", "ООН" },
                    InterestingFacts = new[]
                    {
                        "Самый распространённый славянский язык в мире",
                        "Один из 6 официальных языков ООН",
                        "В русском языке 6 падежей и 3 склонения",
                        "Кириллица состоит из 33 букв"
                    },
                    CurrencySymbol = "₽",
                    CurrencyName = "Российский рубль",
                    DateFormatDescription = "ДД.ММ.ГГГГ (день.месяц.год)",
                    TimeFormatDescription = "24-часовой формат (ЧЧ:ММ)"
                },

                "en" => new LanguageInfo
                {
                    LanguageName = "Английский",
                    NativeName = "English",
                    Countries = new[] { "🇬🇧 Великобритания", "🇺🇸 США", "🇨🇦 Канада", "🇦🇺 Австралия", "🇳🇿 Новая Зеландия" },
                    Regions = new[] { "Северная Америка", "Европа", "Океания", "Южная Азия" },
                    TotalSpeakers = "≈ 1,5 млрд человек",
                    NativeSpeakers = "≈ 380 млн человек",
                    LanguageFamily = "Индоевропейская → Германская → Западногерманская",
                    WritingSystem = "Латиница",
                    IsOfficialIn = new[] { "67 стран", "ООН", "ЕС", "НАТО" },
                    InterestingFacts = new[]
                    {
                        "Самый распространённый язык международного общения",
                        "Официальный язык воздушного и морского сообщения",
                        "В английском больше всего слов среди всех языков (≈ 1 млн)",
                        "Shakespeare добавил около 1700 новых слов в английский язык"
                    },
                    CurrencySymbol = "$",
                    CurrencyName = "Доллар США / Фунт стерлингов",
                    DateFormatDescription = "MM/DD/YYYY (месяц/день/год) — в США",
                    TimeFormatDescription = "12-часовой формат (AM/PM)"
                },

                "tt" => new LanguageInfo
                {
                    LanguageName = "Татарский",
                    NativeName = "Татар теле",
                    Countries = new[] { "🇷🇺 Россия (Татарстан, Башкортостан)" },
                    Regions = new[] { "Республика Татарстан", "Республика Башкортостан", "Чувашская Республика", "Удмуртская Республика" },
                    TotalSpeakers = "≈ 5,5 млн человек",
                    NativeSpeakers = "≈ 5,2 млн человек",
                    LanguageFamily = "Тюркская → Кыпчакская → Поволжско-кыпчакская",
                    WritingSystem = "Кириллица (с 1939 года). Ранее: арабская графика, латиница",
                    IsOfficialIn = new[] { "Республика Татарстан (государственный язык)" },
                    InterestingFacts = new[]
                    {
                        "Второй по распространённости язык в России после русского",
                        "Татарский язык использует все 33 буквы русского алфавита + 6 дополнительных (ә, ө, ү, җ, ң, һ)",
                        "Казанский Кремль — объект Всемирного наследия ЮНЕСКО в Татарстане",
                        "Татарская литература имеет более чем 1000-летнюю историю",
                        "На татарском издаётся более 30 газет и 10 журналов"
                    },
                    CurrencySymbol = "₽",
                    CurrencyName = "Российский рубль",
                    DateFormatDescription = "ДД.ММ.ГГГГ (день.месяц.год)",
                    TimeFormatDescription = "24-часовой формат (ЧЧ:ММ)"
                },

                "de" => new LanguageInfo
                {
                    LanguageName = "Немецкий",
                    NativeName = "Deutsch",
                    Countries = new[] { "🇩🇪 Германия", "🇦🇹 Австрия", "🇨🇭 Швейцария", "🇱🇮 Лихтенштейн", "🇱🇺 Люксембург" },
                    Regions = new[] { "Центральная Европа" },
                    TotalSpeakers = "≈ 130 млн человек",
                    NativeSpeakers = "≈ 95 млн человек",
                    LanguageFamily = "Индоевропейская → Германская → Западногерманская",
                    WritingSystem = "Латиница + умлауты (ä, ö, ü) и ß",
                    IsOfficialIn = new[] { "Германия", "Австрия", "Швейцария", "Лихтенштейн", "Люксембург", "ЕС" },
                    InterestingFacts = new[]
                    {
                        "Самый распространённый родной язык в Европейском Союзе",
                        "Немецкий известен своими длинными составными словами",
                        "Умлауты (ä, ö, ü) меняют произношение и значение слов",
                        "В немецком все существительные пишутся с большой буквы"
                    },
                    CurrencySymbol = "€",
                    CurrencyName = "Евро",
                    DateFormatDescription = "DD.MM.YYYY (день.месяц.год)",
                    TimeFormatDescription = "24-часовой формат (ЧЧ:ММ)"
                },

                "fr" => new LanguageInfo
                {
                    LanguageName = "Французский",
                    NativeName = "Français",
                    Countries = new[] { "🇫🇷 Франция", "🇧🇪 Бельгия", "🇨🇭 Швейцария", "🇨🇦 Канада (Квебек)", "🇸🇳 Сенегал" },
                    Regions = new[] { "Западная Европа", "Северная Америка", "Западная Африка", "Карибский бассейн" },
                    TotalSpeakers = "≈ 320 млн человек",
                    NativeSpeakers = "≈ 80 млн человек",
                    LanguageFamily = "Индоевропейская → Романская → Галло-романская",
                    WritingSystem = "Латиница с диакритическими знаками",
                    IsOfficialIn = new[] { "29 стран", "ООН", "ЕС", "НАТО", "Олимпийский комитет" },
                    InterestingFacts = new[]
                    {
                        "Официальный язык дипломатии и международных отношений",
                        "Язык любви и искусства (langue de l'amour)",
                        "Французская академия регулирует язык с 1635 года",
                        "Около 45% английских слов имеют французское происхождение"
                    },
                    CurrencySymbol = "€",
                    CurrencyName = "Евро",
                    DateFormatDescription = "DD/MM/YYYY (день/месяц/год)",
                    TimeFormatDescription = "24-часовой формат (ЧЧ:ММ)"
                },

                "es" => new LanguageInfo
                {
                    LanguageName = "Испанский",
                    NativeName = "Español / Castellano",
                    Countries = new[] { "🇪🇸 Испания", "🇲🇽 Мексика", "🇦🇷 Аргентина", "🇨🇴 Колумбия", "🇵🇪 Перу" },
                    Regions = new[] { "Южная Европа", "Латинская Америка", "Центральная Америка", "Карибский бассейн" },
                    TotalSpeakers = "≈ 600 млн человек",
                    NativeSpeakers = "≈ 500 млн человек",
                    LanguageFamily = "Индоевропейская → Романская → Иберо-романская",
                    WritingSystem = "Латиница с дополнительными символами (ñ, á, é, í, ó, ú, ü)",
                    IsOfficialIn = new[] { "21 страна", "ООН", "ЕС" },
                    InterestingFacts = new[]
                    {
                        "Второй по распространённости родной язык в мире после китайского",
                        "Самый быстрый язык по скорости произношения",
                        "Перевёрнутые вопросительный (¿) и восклицательный (¡) знаки уникальны",
                        "В разных испаноязычных странах сильно отличается произношение и сленг"
                    },
                    CurrencySymbol = "€",
                    CurrencyName = "Евро (в Испании)",
                    DateFormatDescription = "DD/MM/YYYY (день/месяц/год)",
                    TimeFormatDescription = "24-часовой формат (ЧЧ:ММ)"
                },

                "tr" => new LanguageInfo
                {
                    LanguageName = "Турецкий",
                    NativeName = "Türkçe",
                    Countries = new[] { "🇹🇷 Турция", "🇨🇾 Северный Кипр" },
                    Regions = new[] { "Малая Азия", "Юго-Восточная Европа", "Ближний Восток" },
                    TotalSpeakers = "≈ 88 млн человек",
                    NativeSpeakers = "≈ 75 млн человек",
                    LanguageFamily = "Тюркская → Огузская → Западно-огузская",
                    WritingSystem = "Латиница (с 1928 года)",
                    IsOfficialIn = new[] { "Турция", "Северный Кипр" },
                    InterestingFacts = new[]
                    {
                        "Реформа алфавита 1928 года: переход с арабской графики на латиницу",
                        "Агглютинативный язык: слова строятся добавлением суффиксов",
                        "Гармония гласных — важнейшее правило турецкой фонетики",
                        "В турецком нет грамматического рода (he/she/it = o)"
                    },
                    CurrencySymbol = "₺",
                    CurrencyName = "Турецкая лира",
                    DateFormatDescription = "DD.MM.YYYY (день.месяц.год)",
                    TimeFormatDescription = "24-часовой формат (ЧЧ:ММ)"
                },

                "zh" => new LanguageInfo
                {
                    LanguageName = "Китайский (Мандарин)",
                    NativeName = "中文 / 普通话",
                    Countries = new[] { "🇨🇳 Китай", "🇹🇼 Тайвань", "🇸🇬 Сингапур" },
                    Regions = new[] { "Восточная Азия", "Юго-Восточная Азия" },
                    TotalSpeakers = "≈ 1,3 млрд человек",
                    NativeSpeakers = "≈ 940 млн человек",
                    LanguageFamily = "Сино-тибетская → Китайская → Мандарин",
                    WritingSystem = "Китайские иероглифы (汉字). Пиньинь для транслитерации",
                    IsOfficialIn = new[] { "Китай", "Тайвань", "Сингапур", "ООН" },
                    InterestingFacts = new[]
                    {
                        "Самый распространённый родной язык в мире",
                        "Более 50 000 иероглифов, но для чтения газет достаточно 2 000-3 000",
                        "Тональный язык: 4 тона + нейтральный тон",
                        "Одна из древнейших непрерывно используемых письменных систем (более 3000 лет)",
                        "Красный цвет символизирует удачу и процветание"
                    },
                    CurrencySymbol = "¥",
                    CurrencyName = "Китайский юань (жэньминьби)",
                    DateFormatDescription = "YYYY-MM-DD (год-месяц-день)",
                    TimeFormatDescription = "24-часовой формат (ЧЧ:ММ)"
                },

                _ => new LanguageInfo
                {
                    LanguageName = langCode,
                    NativeName = "",
                    Countries = new[] { "Нет данных" },
                    Regions = new[] { "Нет данных" },
                    TotalSpeakers = "Нет данных",
                    NativeSpeakers = "Нет данных",
                    LanguageFamily = "Нет данных",
                    WritingSystem = "Нет данных",
                    IsOfficialIn = new[] { "Нет данных" },
                    InterestingFacts = new[] { "Информация отсутствует" },
                    CurrencySymbol = "",
                    CurrencyName = "",
                    DateFormatDescription = "",
                    TimeFormatDescription = ""
                }
            };
        }
    }

    public class LanguageInfo
    {
        public string LanguageName { get; set; } = "";
        public string NativeName { get; set; } = "";
        public string[] Countries { get; set; } = Array.Empty<string>();
        public string[] Regions { get; set; } = Array.Empty<string>();
        public string TotalSpeakers { get; set; } = "";
        public string NativeSpeakers { get; set; } = "";
        public string LanguageFamily { get; set; } = "";
        public string WritingSystem { get; set; } = "";
        public string[] IsOfficialIn { get; set; } = Array.Empty<string>();
        public string[] InterestingFacts { get; set; } = Array.Empty<string>();
        public string CurrencySymbol { get; set; } = "";
        public string CurrencyName { get; set; } = "";
        public string DateFormatDescription { get; set; } = "";
        public string TimeFormatDescription { get; set; } = "";
    }
}