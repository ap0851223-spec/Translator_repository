using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Project_translator.Data;

namespace Project_translator.Pages
{
    public class IndexModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public IndexModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public int TotalProjects { get; set; }
        public int TotalTranslations { get; set; }
        public int TotalLocales { get; set; }
        public int TotalSourceStrings { get; set; }

        public async Task OnGetAsync()
        {
            try
            {
                TotalProjects = await _context.Projects.CountAsync();
                TotalTranslations = await _context.Translations.CountAsync();
                TotalLocales = await _context.Locales.CountAsync();
                TotalSourceStrings = await _context.SourceStrings.CountAsync();
            }
            catch
            {
                // Если БД недоступна, устанавливаем нули
                TotalProjects = 0;
                TotalTranslations = 0;
                TotalLocales = 0;
                TotalSourceStrings = 0;
            }
        }
    }
}