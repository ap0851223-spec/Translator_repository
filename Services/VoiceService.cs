// Services/VoiceService.cs
namespace Project_translator.Services
{
    public interface IVoiceService
    {
        VoiceConfig GetVoiceConfig(string languageCode);
        Dictionary<string, VoiceConfig> GetAllVoiceConfigs();
    }

    public class VoiceService : IVoiceService
    {
        private readonly Dictionary<string, VoiceConfig> _voiceConfigs = new()
        {
            ["ru"] = new VoiceConfig
            {
                LanguageCode = "ru",
                VoiceName = "Russian Female",
                LangTag = "ru-RU",
                Rate = 0.9,
                Pitch = 1.0,
                Volume = 1.0,
                Gender = "female"
            },
            ["en"] = new VoiceConfig
            {
                LanguageCode = "en",
                VoiceName = "English US Female",
                LangTag = "en-US",
                Rate = 1.0,
                Pitch = 1.0,
                Volume = 1.0,
                Gender = "female"
            },
            ["tt"] = new VoiceConfig
            {
                LanguageCode = "tt",
                VoiceName = "Russian Female (Tatar accent)",
                LangTag = "ru-RU",
                Rate = 0.8,
                Pitch = 1.0,
                Volume = 1.0,
                Gender = "female"
            },
            ["es"] = new VoiceConfig
            {
                LanguageCode = "es",
                VoiceName = "Spanish Female",
                LangTag = "es-ES",
                Rate = 1.1,
                Pitch = 1.0,
                Volume = 1.0,
                Gender = "female"
            },
            ["fr"] = new VoiceConfig
            {
                LanguageCode = "fr",
                VoiceName = "French Female",
                LangTag = "fr-FR",
                Rate = 1.0,
                Pitch = 0.9,
                Volume = 1.0,
                Gender = "female"
            },
            ["de"] = new VoiceConfig
            {
                LanguageCode = "de",
                VoiceName = "German Female",
                LangTag = "de-DE",
                Rate = 1.0,
                Pitch = 1.0,
                Volume = 1.0,
                Gender = "female"
            },
            ["tr"] = new VoiceConfig
            {
                LanguageCode = "tr",
                VoiceName = "Turkish Female",
                LangTag = "tr-TR",
                Rate = 1.0,
                Pitch = 1.0,
                Volume = 1.0,
                Gender = "female"
            },
            ["zh"] = new VoiceConfig
            {
                LanguageCode = "zh",
                VoiceName = "Chinese Female",
                LangTag = "zh-CN",
                Rate = 0.9,
                Pitch = 1.1,
                Volume = 1.0,
                Gender = "female"
            }
        };

        public VoiceConfig GetVoiceConfig(string languageCode)
        {
            return _voiceConfigs.GetValueOrDefault(languageCode.ToLower(),
                new VoiceConfig
                {
                    LanguageCode = "en",
                    VoiceName = "Default",
                    LangTag = "en-US",
                    Rate = 1.0,
                    Pitch = 1.0,
                    Volume = 1.0
                });
        }

        public Dictionary<string, VoiceConfig> GetAllVoiceConfigs()
        {
            return _voiceConfigs;
        }
    }

    public class VoiceConfig
    {
        public string LanguageCode { get; set; } = "en";
        public string VoiceName { get; set; } = "Default";
        public string LangTag { get; set; } = "en-US";
        public double Rate { get; set; } = 1.0;
        public double Pitch { get; set; } = 1.0;
        public double Volume { get; set; } = 1.0;
        public string Gender { get; set; } = "female";
    }
}