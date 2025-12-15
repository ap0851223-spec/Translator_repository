using Microsoft.EntityFrameworkCore;
using Project_translator.Models;
using System.Reflection.Emit;

namespace Project_translator.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

        public DbSet<Project> Projects { get; set; }
        public DbSet<Locale> Locales { get; set; }
        public DbSet<SourceString> SourceStrings { get; set; }
        public DbSet<Translation> Translations { get; set; }
        public DbSet<GlossaryTerm> GlossaryTerms { get; set; }
        public DbSet<TranslationMemory> TranslationMemories { get; set; }
        public DbSet<CulturalProfile> CulturalProfiles { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Указываем схему для всех сущностей
            modelBuilder.HasDefaultSchema("localization");

            // Все таблицы будут созданы с именами в нижнем регистре
            // благодаря атрибутам [Table] в моделях

            // Конфигурация связей
            modelBuilder.Entity<SourceString>()
                .HasOne(s => s.Project)
                .WithMany(p => p.SourceStrings)
                .HasForeignKey(s => s.ProjectId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Translation>()
                .HasOne(t => t.SourceString)
                .WithMany(s => s.Translations)
                .HasForeignKey(t => t.SourceStringId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Translation>()
                .HasOne(t => t.Locale)
                .WithMany(l => l.Translations)
                .HasForeignKey(t => t.LocaleId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<GlossaryTerm>()
                .HasOne(g => g.Project)
                .WithMany(p => p.GlossaryTerms)
                .HasForeignKey(g => g.ProjectId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<CulturalProfile>()
                .HasOne(c => c.Locale)
                .WithOne(l => l.CulturalProfile)
                .HasForeignKey<CulturalProfile>(c => c.LocaleId)
                .OnDelete(DeleteBehavior.Cascade);

            // Индексы для быстрого поиска
            modelBuilder.Entity<TranslationMemory>()
                .HasIndex(t => new { t.SourceText, t.SourceLang, t.TargetLang })
                .IsUnique();

            modelBuilder.Entity<GlossaryTerm>()
                .HasIndex(g => new { g.ProjectId, g.Term })
                .IsUnique();

            modelBuilder.Entity<SourceString>()
                .HasIndex(s => s.Key);

            modelBuilder.Entity<Translation>()
                .HasIndex(t => new { t.SourceStringId, t.LocaleId })
                .IsUnique();

            // Значения по умолчанию
            modelBuilder.Entity<Translation>()
                .Property(t => t.Status)
                .HasDefaultValue("draft");

            modelBuilder.Entity<TranslationMemory>()
                .Property(t => t.UsageCount)
                .HasDefaultValue(0);

            modelBuilder.Entity<CulturalProfile>()
                .Property(c => c.FirstDayOfWeek)
                .HasDefaultValue(1);

            // В методе OnModelCreating добавьте:
            modelBuilder.Entity<TranslationMemory>()
                .HasOne(tm => tm.Project)
                .WithMany()
                .HasForeignKey(tm => tm.ProjectId)
                .OnDelete(DeleteBehavior.SetNull); // При удалении проекта оставляем запись

            modelBuilder.Entity<TranslationMemory>()
                .HasOne(tm => tm.SourceString)
                .WithMany()
                .HasForeignKey(tm => tm.SourceStringId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<TranslationMemory>()
                .HasOne(tm => tm.Translation)
                .WithMany()
                .HasForeignKey(tm => tm.TranslationId)
                .OnDelete(DeleteBehavior.SetNull);

            // Индексы
            modelBuilder.Entity<TranslationMemory>()
                .HasIndex(tm => new { tm.SourceText, tm.SourceLang, tm.TargetLang, tm.ProjectId, tm.Context })
                .IsUnique();

            modelBuilder.Entity<TranslationMemory>()
                .HasIndex(tm => tm.ProjectId);

            modelBuilder.Entity<TranslationMemory>()
                .HasIndex(tm => tm.TranslationId);

            // Значения по умолчанию
            modelBuilder.Entity<TranslationMemory>()
                .Property(tm => tm.MatchScore)
                .HasDefaultValue(100);

            modelBuilder.Entity<TranslationMemory>()
                .Property(tm => tm.UsageCount)
                .HasDefaultValue(0);

            modelBuilder.Entity<TranslationMemory>()
                .Property(tm => tm.CreatedAt)
                .HasDefaultValueSql("NOW()");

            modelBuilder.Entity<TranslationMemory>()
                .Property(tm => tm.UpdatedAt)
                .HasDefaultValueSql("NOW()")
                .ValueGeneratedOnAddOrUpdate();
        }
    }
}