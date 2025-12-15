// test-api.js - для отладки API
async function testQuickTranslateAPI() {
    console.log('Тестирование QuickTranslate API...');

    try {
        const response = await fetch('/api/QuickTranslate/translate', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
            },
            body: JSON.stringify({
                Text: "Hello",
                SourceLang: "en",
                TargetLang: "ru"
            })
        });

        console.log('Статус:', response.status);
        console.log('Заголовки:', response.headers);

        const text = await response.text();
        console.log('Ответ текст:', text);

        try {
            const data = JSON.parse(text);
            console.log('Ответ JSON:', data);
        } catch (e) {
            console.log('Не удалось распарсить JSON');
        }

    } catch (error) {
        console.error('Ошибка теста API:', error);
    }
}

// Тест других API endpoints
async function testAllEndpoints() {
    const endpoints = [
        '/api/Test/hello',
        '/api/QuickTranslate/status',
        '/api/Translations/test-mymemory?text=Hello&source=en&target=ru'
    ];

    for (const endpoint of endpoints) {
        try {
            console.log(`Тестируем ${endpoint}...`);
            const response = await fetch(endpoint);
            console.log(`${endpoint}: ${response.status}`);
            const data = await response.json();
            console.log(`${endpoint} данные:`, data);
        } catch (error) {
            console.error(`${endpoint} ошибка:`, error.message);
        }
    }
}

// Автоматически запустить тесты при загрузке страницы
if (window.location.pathname === '/') {
    setTimeout(() => {
        console.log('=== Начинаю тестирование API ===');
        testAllEndpoints();
        testQuickTranslateAPI();
    }, 2000);
}