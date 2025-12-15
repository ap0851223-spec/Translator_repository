// Project_translator/Services/ITranslationService.cs
namespace Project_translator.Services
{
    public interface ITranslationService
    {
        Task<string> TranslateAsync(string text, string sourceLang, string targetLang);
    }
}