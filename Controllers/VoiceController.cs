using Microsoft.AspNetCore.Mvc;
using Project_translator.Services;
using System.Text.Json;

namespace Project_translator.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class VoiceController : ControllerBase
    {
        private readonly ITranslationService _translationService;
        private readonly ILogger<VoiceController> _logger;

        // Голосовые профили для разных языков
        private static readonly Dictionary<string, VoiceProfile> VoiceProfiles = new()
        {
            ["ru"] = new VoiceProfile { Lang = "ru-RU", Name = "Google русский", Gender = "female", Rate = 0.9 },
            ["en"] = new VoiceProfile { Lang = "en-US", Name = "Google US English", Gender = "female", Rate = 1.0 },
            ["tt"] = new VoiceProfile { Lang = "ru-RU", Name = "Google русский", Gender = "female", Rate = 0.8 },
            ["es"] = new VoiceProfile { Lang = "es-ES", Name = "Google español", Gender = "female", Rate = 1.1 },
            ["fr"] = new VoiceProfile { Lang = "fr-FR", Name = "Google français", Gender = "female", Rate = 1.0 },
            ["de"] = new VoiceProfile { Lang = "de-DE", Name = "Google Deutsch", Gender = "female", Rate = 1.0 },
            ["tr"] = new VoiceProfile { Lang = "tr-TR", Name = "Google Türkçe", Gender = "female", Rate = 1.0 },
            ["zh"] = new VoiceProfile { Lang = "zh-CN", Name = "Google 普通话", Gender = "female", Rate = 0.9 }
        };

        public VoiceController(
            ITranslationService translationService,
            ILogger<VoiceController> logger)
        {
            _translationService = translationService;
            _logger = logger;
        }

        [HttpGet("profile/{langCode}")]
        public IActionResult GetVoiceProfile(string langCode)
        {
            if (VoiceProfiles.TryGetValue(langCode.ToLower(), out var profile))
            {
                return Ok(new
                {
                    success = true,
                    profile = new
                    {
                        profile.Lang,
                        profile.Name,
                        profile.Gender,
                        profile.Rate,
                        // Рекомендации для SpeechSynthesis
                        recommendations = new
                        {
                            pitch = langCode switch
                            {
                                "zh" => 1.1,
                                "fr" => 0.9,
                                _ => 1.0
                            },
                            volume = 1.0
                        }
                    }
                });
            }

            return Ok(new
            {
                success = true,
                profile = new VoiceProfile { Lang = "en-US", Name = "Default", Gender = "female", Rate = 1.0 }
            });
        }

        [HttpGet("profiles")]
        public IActionResult GetAllVoiceProfiles()
        {
            return Ok(new
            {
                success = true,
                profiles = VoiceProfiles.Select(kvp => new
                {
                    code = kvp.Key,
                    kvp.Value.Lang,
                    kvp.Value.Name,
                    kvp.Value.Gender,
                    kvp.Value.Rate
                })
            });
        }

        [HttpPost("speak")]
        public IActionResult GetSpeechData([FromBody] SpeechRequest request)
        {
            var profile = VoiceProfiles.GetValueOrDefault(request.Lang.ToLower(),
                new VoiceProfile { Lang = "en-US", Name = "Default", Gender = "female", Rate = 1.0 });

            return Ok(new
            {
                success = true,
                text = request.Text,
                lang = request.Lang,
                voiceProfile = profile,
                settings = new
                {
                    rate = request.Speed > 0 ? request.Speed : profile.Rate,
                    pitch = request.Pitch > 0 ? request.Pitch : 1.0,
                    volume = request.Volume > 0 ? request.Volume : 1.0
                }
            });
        }
    }

    public class VoiceProfile
    {
        public string Lang { get; set; } = "en-US";
        public string Name { get; set; } = "Default";
        public string Gender { get; set; } = "female";
        public double Rate { get; set; } = 1.0;
    }

    public class SpeechRequest
    {
        public string Text { get; set; } = string.Empty;
        public string Lang { get; set; } = "en";
        public double Speed { get; set; } = 1.0;
        public double Pitch { get; set; } = 1.0;
        public double Volume { get; set; } = 1.0;
    }
}