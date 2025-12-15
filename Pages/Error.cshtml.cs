using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Diagnostics;

namespace Project_translator.Pages
{
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    [IgnoreAntiforgeryToken]
    public class ErrorModel : PageModel
    {
        public string? RequestId { get; set; }
        public int ErrorCode { get; set; }
        public string? ErrorMessage { get; set; }

        public bool ShowRequestId => !string.IsNullOrEmpty(RequestId);

        public void OnGet(int? code = null)
        {
            RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier;

            if (code.HasValue)
            {
                ErrorCode = code.Value;
                ErrorMessage = GetErrorMessage(code.Value);
            }
            else
            {
                var statusCode = HttpContext.Response.StatusCode;
                ErrorCode = statusCode;
                ErrorMessage = GetErrorMessage(statusCode);
            }
        }

        private string GetErrorMessage(int statusCode)
        {
            return statusCode switch
            {
                400 => "Некорректный запрос",
                401 => "Требуется авторизация",
                403 => "Доступ запрещен",
                404 => "Страница не найдена",
                500 => "Внутренняя ошибка сервера",
                _ => "Произошла ошибка"
            };
        }
    }
}