// api-tester.js - Утилита для тестирования переводческих API
class ApiTester {
    constructor() {
        this.apis = {
            mymemory: 'https://api.mymemory.translated.net/get',
            google: 'https://translate.googleapis.com/translate_a/single',
            yandex: 'https://translate.yandex.net/api/v1.5/tr.json/translate'
        };
    }

    async testMyMemory(text, sourceLang, targetLang) {
        try {
            const encodedText = encodeURIComponent(text);
            const url = `${this.apis.mymemory}?q=${encodedText}&langpair=${sourceLang}|${targetLang}`;

            const response = await fetch(url);
            if (!response.ok) throw new Error(`HTTP ${response.status}`);

            const data = await response.json();
            return {
                success: true,
                provider: 'MyMemory',
                translatedText: data.responseData?.translatedText || text,
                match: data.responseData?.match || 0,
                raw: data
            };
        } catch (error) {
            return {
                success: false,
                provider: 'MyMemory',
                error: error.message
            };
        }
    }

    async testAllProviders(text, sourceLang, targetLang) {
        const results = [];

        // Тестируем MyMemory
        results.push(await this.testMyMemory(text, sourceLang, targetLang));

        // Можно добавить другие провайдеры здесь

        return results;
    }

    async benchmark(text, sourceLang, targetLang, iterations = 3) {
        const times = [];

        for (let i = 0; i < iterations; i++) {
            const start = performance.now();
            await this.testMyMemory(text, sourceLang, targetLang);
            const end = performance.now();
            times.push(end - start);

            // Пауза между запросами
            await new Promise(resolve => setTimeout(resolve, 100));
        }

        return {
            average: times.reduce((a, b) => a + b, 0) / times.length,
            min: Math.min(...times),
            max: Math.max(...times),
            times
        };
    }
}

// Экспортируем для использования в других файлах
window.ApiTester = ApiTester;