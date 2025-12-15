using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Project_translator.Models
{
    [Table("translation_memory", Schema = "localization")]
    public class TranslationMemory
    {
        [Column("id")]
        public int Id { get; set; }

        // Связь с проектом (необязательная)
        [Column("project_id")]
        public int? ProjectId { get; set; }

        // Связь с исходной строкой (необязательная)
        [Column("source_string_id")]
        public int? SourceStringId { get; set; }

        // Языковые коды
        [Column("source_lang")]
        [Required]
        [MaxLength(10)]
        public string SourceLang { get; set; } = "en";

        [Column("target_lang")]
        [Required]
        [MaxLength(10)]
        public string TargetLang { get; set; } = "ru";

        // Тексты
        [Column("source_text")]
        [Required]
        public string SourceText { get; set; } = string.Empty;

        [Column("target_text")]
        [Required]
        public string TargetText { get; set; } = string.Empty;

        // Контекст для лучшего соответствия
        [Column("context")]
        public string? Context { get; set; }

        // Метрики качества
        [Column("match_score")]
        public int MatchScore { get; set; } = 100; // 0-100%

        [Column("usage_count")]
        public int UsageCount { get; set; } = 0;

        [Column("last_used")]
        public DateTime? LastUsed { get; set; }

        // Связь с существующим переводом
        [Column("translation_id")]
        public int? TranslationId { get; set; }

        // Метаданные
        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [Column("updated_at")]
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // Навигационные свойства
        [ForeignKey("ProjectId")]
        public Project? Project { get; set; }

        [ForeignKey("SourceStringId")]
        public SourceString? SourceString { get; set; }

        [ForeignKey("TranslationId")]
        public Translation? Translation { get; set; }

        // Языковые связи через коды (не через ID)
        // Эти свойства не маппятся на БД, но полезны в коде
        [NotMapped]
        public Locale? SourceLocale { get; set; }

        [NotMapped]
        public Locale? TargetLocale { get; set; }
    }
}