using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Project_translator.Models
{
    [Table("translations", Schema = "localization")]
    public class Translation
    {
        [Column("id")]
        public int Id { get; set; }

        [Column("source_string_id")]
        public int SourceStringId { get; set; }

        [Column("locale_id")]
        public int LocaleId { get; set; }

        [Column("translated_text")]
        [Required]
        public string TranslatedText { get; set; } = string.Empty;

        [Column("status")]
        [MaxLength(20)]
        public string Status { get; set; } = "draft";

        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Навигационные свойства
        [ForeignKey("SourceStringId")]
        public SourceString? SourceString { get; set; }

        [ForeignKey("LocaleId")]
        public Locale? Locale { get; set; }
    }
}