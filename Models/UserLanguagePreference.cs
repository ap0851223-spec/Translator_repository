// Models/UserLanguagePreference.cs
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Project_translator.Models
{
    [Table("user_language_preferences", Schema = "localization")]
    public class UserLanguagePreference
    {
        [Column("id")]
        public int Id { get; set; }

        [Column("session_id")]
        [MaxLength(100)]
        public string SessionId { get; set; } = string.Empty;

        [Column("last_source_locale_id")]
        public int? LastSourceLocaleId { get; set; }

        [Column("last_target_locale_id")]
        public int? LastTargetLocaleId { get; set; }

        [Column("voice_enabled")]
        public bool VoiceEnabled { get; set; } = true;

        [Column("auto_speak")]
        public bool AutoSpeak { get; set; } = true;

        [Column("speech_rate")]
        public double SpeechRate { get; set; } = 1.0;

        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [Column("updated_at")]
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // Навигационные свойства
        [ForeignKey("LastSourceLocaleId")]
        public Locale? SourceLocale { get; set; }

        [ForeignKey("LastTargetLocaleId")]
        public Locale? TargetLocale { get; set; }
    }
}