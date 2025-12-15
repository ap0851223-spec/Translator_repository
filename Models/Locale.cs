using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Project_translator.Models
{
    [Table("locales", Schema = "localization")]
    public class Locale
    {
        [Column("id")]
        public int Id { get; set; }

        [Column("code")]
        [Required]
        [MaxLength(10)]
        public string Code { get; set; } = string.Empty;

        [Column("name")]
        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        [Column("is_default")]
        public bool IsDefault { get; set; }

        // Навигационные свойства
        public ICollection<Translation>? Translations { get; set; }
        public CulturalProfile? CulturalProfile { get; set; }
    }
}