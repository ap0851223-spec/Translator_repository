using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using Project_translator.Data;
using Project_translator.Models;
using Project_translator.Services;

var builder = WebApplication.CreateBuilder(args);

// ========== ДОБАВЛЯЕМ СЕРВИСЫ ==========

// Добавляем HttpClient для API запросов
builder.Services.AddHttpClient();

// Регистрируем сервис памяти переводов (ТОЛЬКО ОДИН РАЗ!)
builder.Services.AddScoped<ITranslationMemoryService, TranslationMemoryService>();

// Регистрируем сервис перевода с зависимостью от памяти переводов
builder.Services.AddScoped<ITranslationService, MyMemoryTranslationService>();

// ИЛИ если нужна фабричная регистрация:

builder.Services.AddScoped<ITranslationService>(provider =>
{
    var httpClient = provider.GetRequiredService<HttpClient>();
    var logger = provider.GetRequiredService<ILogger<MyMemoryTranslationService>>();
    var memoryService = provider.GetRequiredService<ITranslationMemoryService>();
    
    return new MyMemoryTranslationService(httpClient, logger, memoryService);
});


// Регистрируем остальные сервисы
builder.Services.AddRazorPages();
builder.Services.AddControllers();

// Добавьте новые сервисы:
builder.Services.AddScoped<IVoiceService, VoiceService>();
builder.Services.AddScoped<ILanguageDetectionService, LanguageDetectionService>();

// Добавьте Memory Cache для кэширования голосовых профилей
builder.Services.AddMemoryCache();

// Настройка логгирования
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();

// Настройка Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Localization System API",
        Version = "v1",
        Description = "API для системы автоматического перевода и локализации с поддержкой памяти переводов и культурных профилей"
    });

    // Добавляем поддержку комментариев XML
    var xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (File.Exists(xmlPath))
    {
        c.IncludeXmlComments(xmlPath);
    }
});

// Настройка базы данных PostgreSQL
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// Добавляем CORS для API
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

var app = builder.Build();

// ========== НАСТРАИВАЕМ ПРИЛОЖЕНИЕ ==========

// Конвейер middleware
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();

    // Включаем Swagger только в разработке
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Localization System API v1");
        c.RoutePrefix = "swagger";
    });
}
else
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles(); // Для wwwroot файлов
app.UseRouting();
app.UseCors("AllowAll");
app.UseAuthorization();

app.MapRazorPages();
app.MapControllers();

// ========== ИНИЦИАЛИЗАЦИЯ БАЗЫ ДАННЫХ ==========
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

    try
    {
        // Создаем базу данных и таблицы (если их нет)
        db.Database.EnsureCreated();
        Console.WriteLine("✅ База данных проверена/создана");

        // Проверяем соединение с БД
        var localesCount = await db.Locales.CountAsync();
        Console.WriteLine($"✅ Подключение к БД успешно. Найдено локалей: {localesCount}");

        // 1. Добавляем начальные культурные профили если их нет
        if (!db.CulturalProfiles.Any())
        {
            var locales = await db.Locales.ToListAsync();
            int addedProfiles = 0;

            foreach (var locale in locales)
            {
                // Проверяем, нет ли уже профиля для этой локали
                var existingProfile = await db.CulturalProfiles
                    .FirstOrDefaultAsync(c => c.LocaleId == locale.Id);

                if (existingProfile == null)
                {
                    var profile = new CulturalProfile
                    {
                        LocaleId = locale.Id,
                        Currency = locale.Code switch
                        {
                            "ru" => "RUB",
                            "en" => "USD",
                            "tt" => "RUB",
                            _ => "USD"
                        },
                        DateFormat = locale.Code switch
                        {
                            "ru" => "dd.MM.yyyy",
                            "en" => "MM/dd/yyyy",
                            "tt" => "dd.MM.yyyy",
                            _ => "yyyy-MM-dd"
                        },
                        TimeFormat = "HH:mm",
                        FirstDayOfWeek = locale.Code == "en" ? 0 : 1 // 0 = Sunday для EN
                    };

                    db.CulturalProfiles.Add(profile);
                    addedProfiles++;
                }
            }

            if (addedProfiles > 0)
            {
                await db.SaveChangesAsync();
                Console.WriteLine($"✅ Добавлено культурных профилей: {addedProfiles}");
            }
        }
        else
        {
            var profilesCount = await db.CulturalProfiles.CountAsync();
            Console.WriteLine($"✅ Культурные профили уже существуют: {profilesCount}");
        }

        // 2. Добавляем тестовые термины в глоссарий если пусто
        if (!db.GlossaryTerms.Any() && db.Projects.Any())
        {
            var firstProject = await db.Projects.FirstOrDefaultAsync();
            if (firstProject != null)
            {
                var glossaryTerms = new List<GlossaryTerm>
                {
                    new GlossaryTerm
                    {
                        ProjectId = firstProject.Id,
                        Term = "Submit",
                        Description = "Кнопка отправки формы"
                    },
                    new GlossaryTerm
                    {
                        ProjectId = firstProject.Id,
                        Term = "Cancel",
                        Description = "Кнопка отмены действия"
                    },
                    new GlossaryTerm
                    {
                        ProjectId = firstProject.Id,
                        Term = "Login",
                        Description = "Вход в систему"
                    },
                    new GlossaryTerm
                    {
                        ProjectId = firstProject.Id,
                        Term = "Logout",
                        Description = "Выход из системы"
                    }
                };

                foreach (var term in glossaryTerms)
                {
                    db.GlossaryTerms.Add(term);
                }

                await db.SaveChangesAsync();
                Console.WriteLine($"✅ Добавлено терминов в глоссарий: {glossaryTerms.Count}");
            }
        }

        // 3. Проверяем все таблицы
        var stats = new
        {
            Projects = await db.Projects.CountAsync(),
            Locales = await db.Locales.CountAsync(),
            SourceStrings = await db.SourceStrings.CountAsync(),
            Translations = await db.Translations.CountAsync(),
            GlossaryTerms = await db.GlossaryTerms.CountAsync(),
            TranslationMemories = await db.TranslationMemories.CountAsync(),
            CulturalProfiles = await db.CulturalProfiles.CountAsync()
        };

        Console.WriteLine("📊 Статистика базы данных:");
        Console.WriteLine($"   • Проекты: {stats.Projects}");
        Console.WriteLine($"   • Локали: {stats.Locales}");
        Console.WriteLine($"   • Исходные строки: {stats.SourceStrings}");
        Console.WriteLine($"   • Переводы: {stats.Translations}");
        Console.WriteLine($"   • Термины глоссария: {stats.GlossaryTerms}");
        Console.WriteLine($"   • Записи памяти переводов: {stats.TranslationMemories}");
        Console.WriteLine($"   • Культурные профили: {stats.CulturalProfiles}");

    }
    catch (Exception ex)
    {
        Console.WriteLine($"❌ Ошибка при инициализации БД: {ex.Message}");
        if (ex.InnerException != null)
        {
            Console.WriteLine($"   Внутренняя ошибка: {ex.InnerException.Message}");
        }
    }
}

// ========== НАСТРОЙКА МАРШРУТОВ ==========

// Правильная настройка маршрутов
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

// Перенаправление с корня на Index
app.MapGet("/", () => Results.Redirect("/Index"));

// Тестовый endpoint для проверки API
app.MapGet("/api/health", () =>
    Results.Ok(new
    {
        status = "healthy",
        timestamp = DateTime.UtcNow,
        service = "Localization System API"
    }));

// Запускаем приложение
Console.WriteLine("🚀 Приложение запущено!");
Console.WriteLine($"   • Swagger UI: https://localhost:5001/swagger");
Console.WriteLine($"   • Главная страница: https://localhost:5001/");
Console.WriteLine($"   • API Health Check: https://localhost:5001/api/health");

app.Run();