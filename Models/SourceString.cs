using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Project_translator.Models
{
    [Table("source_strings", Schema = "localization")]
    public class SourceString
    {
        [Column("id")]
        public int Id { get; set; }

        [Column("project_id")]
        public int ProjectId { get; set; }

        [Column("key")]
        [Required]
        [MaxLength(500)]
        public string Key { get; set; } = string.Empty;

        [Column("text")]
        [Required]
        public string Text { get; set; } = string.Empty;

        [Column("context")]
        public string? Context { get; set; }

        [Column("max_length")]
        public int? MaxLength { get; set; }

        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Навигационные свойства
        [ForeignKey("ProjectId")]
        public Project? Project { get; set; }

        public ICollection<Translation>? Translations { get; set; }
    }
}