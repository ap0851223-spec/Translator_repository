using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Project_translator.Models
{
    [Table("cultural_profiles", Schema = "localization")]
    public class CulturalProfile
    {
        [Column("id")]
        public int Id { get; set; }

        [Column("locale_id")]
        public int LocaleId { get; set; }

        [Column("currency")]
        [MaxLength(10)]
        public string? Currency { get; set; }

        [Column("date_format")]
        [MaxLength(50)]
        public string? DateFormat { get; set; } = "dd.MM.yyyy";

        [Column("time_format")]
        [MaxLength(50)]
        public string? TimeFormat { get; set; } = "HH:mm";

        [Column("first_day_of_week")]
        public int FirstDayOfWeek { get; set; } = 1; // 1 = Monday

        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Навигационные свойства
        [ForeignKey("LocaleId")]
        public Locale? Locale { get; set; }
    }
}