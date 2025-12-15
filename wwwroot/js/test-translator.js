// test-translator.js - Тестовый скрипт для проверки работы переводчика
console.log('=== ТЕСТОВЫЙ СКРИПТ ЗАГРУЖЕН ===');

// Функция для проверки всех элементов на странице
function checkPageElements() {
    console.log('🧪 Проверка элементов страницы...');

    const requiredElements = [
        { id: 'translateBtn', name: 'Кнопка перевода' },
        { id: 'sourceText', name: 'Поле ввода текста' },
        { id: 'sourceLang', name: 'Выбор исходного языка' },
        { id: 'targetLang', name: 'Выбор целевого языка' },
        { id: 'result', name: 'Контейнер результата' },
        { id: 'swapBtn', name: 'Кнопка обмена языков' },
        { id: 'clearBtn', name: 'Кнопка очистки' }
    ];

    let allFound = true;
    requiredElements.forEach(element => {
        const el = document.getElementById(element.id);
        if (el) {
            console.log(`✅ ${element.name} (id="${element.id}") найден`);

            // Проверим некоторые свойства
            if (element.id === 'translateBtn') {
                console.log(`   Текст кнопки: "${el.textContent.trim()}"`);
                console.log(`   Кнопка активна: ${!el.disabled}`);
            }

        } else {
            console.error(`❌ ${element.name} (id="${element.id}") НЕ НАЙДЕН!`);
            allFound = false;
        }
    });

    return allFound;
}

// Проверка работы API
async function testAPI() {
    console.log('🌐 Проверка API...');

    const endpoints = [
        { url: '/api/Test/hello', method: 'GET', name: 'Базовый API' },
        { url: '/api/QuickTranslate/status', method: 'GET', name: 'Статус переводчика' }
    ];

    for (const endpoint of endpoints) {
        try {
            console.log(`   Тестирую ${endpoint.name} (${endpoint.url})...`);
            const response = await fetch(endpoint.url, { method: endpoint.method });

            if (response.ok) {
                const data = await response.json();
                console.log(`   ✅ ${endpoint.name}: Работает (${response.status})`);
                console.log(`      Ответ:`, data);
            } else {
                console.error(`   ❌ ${endpoint.name}: Ошибка ${response.status}`);
            }
        } catch (error) {
            console.error(`   ❌ ${endpoint.name}: ${error.message}`);
        }
    }
}

// Тест перевода
async function testTranslation() {
    console.log('🔤 Тест перевода...');

    const testCases = [
        { text: 'Hello', source: 'en', target: 'ru', expected: 'Привет' },
        { text: 'Привет', source: 'ru', target: 'tt', expected: 'Сәлам' }
    ];

    for (const testCase of testCases) {
        try {
            console.log(`   Перевод: "${testCase.text}" (${testCase.source} → ${testCase.target})`);

            const response = await fetch('/api/QuickTranslate/translate', {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify({
                    Text: testCase.text,
                    SourceLang: testCase.source,
                    TargetLang: testCase.target
                })
            });

            if (response.ok) {
                const data = await response.json();
                console.log(`   ✅ Успех: ${data.success}`);
                console.log(`      Оригинал: ${data.original}`);
                console.log(`      Перевод: ${data.translated}`);
                console.log(`      Языки: ${data.source} → ${data.target}`);

                if (data.success) {
                    console.log(`   🎉 Перевод получен успешно!`);
                }
            } else {
                console.error(`   ❌ HTTP ошибка: ${response.status}`);
            }
        } catch (error) {
            console.error(`   ❌ Ошибка: ${error.message}`);
        }
    }
}

// Проверка работы кнопок
function testButtons() {
    console.log('🔄 Проверка работы кнопок...');

    // Тест кнопки перевода
    const translateBtn = document.getElementById('translateBtn');
    if (translateBtn) {
        console.log('   Тестирую кнопку перевода...');

        // Создаем тестовое событие
        const clickEvent = new MouseEvent('click', {
            view: window,
            bubbles: true,
            cancelable: true
        });

        // Проверяем, есть ли обработчик
        const hasClickHandler = translateBtn.onclick ||
            translateBtn.getAttribute('onclick') ||
            translateBtn.hasAttribute('listener');

        console.log(`   Кнопка имеет обработчик: ${!!hasClickHandler}`);

        if (hasClickHandler) {
            console.log('   ✅ Кнопка перевода настроена');
        } else {
            console.warn('   ⚠️ Кнопка перевода не имеет обработчика клика!');
        }
    }
}

// Основная функция тестирования
async function runAllTests() {
    console.log('🚀 ЗАПУСК ПОЛНОГО ТЕСТИРОВАНИЯ');
    console.log('================================');

    // 1. Проверка элементов
    console.log('\n1. ПРОВЕРКА ЭЛЕМЕНТОВ СТРАНИЦЫ:');
    const elementsOk = checkPageElements();

    if (!elementsOk) {
        console.error('❌ Некоторые элементы не найдены!');
        return;
    }

    // 2. Проверка API
    console.log('\n2. ПРОВЕРКА API:');
    await testAPI();

    // 3. Тест перевода
    console.log('\n3. ТЕСТ ПЕРЕВОДА:');
    await testTranslation();

    // 4. Проверка кнопок
    console.log('\n4. ПРОВЕРКА КНОПОК:');
    testButtons();

    console.log('\n================================');
    console.log('✅ ТЕСТИРОВАНИЕ ЗАВЕРШЕНО');

    // Показать результат в UI
    showTestResults();
}

// Показать результаты теста на странице
function showTestResults() {
    const testResults = document.createElement('div');
    testResults.id = 'testResults';
    testResults.innerHTML = `
        <div class="alert alert-info mt-3">
            <h5><i class="bi bi-check-circle me-2"></i>Тестирование завершено</h5>
            <p>Проверьте консоль браузера (F12 → Console) для деталей</p>
            <button onclick="runAllTests()" class="btn btn-sm btn-primary">
                <i class="bi bi-arrow-repeat me-1"></i>Запустить тест снова
            </button>
        </div>
    `;

    // Вставляем перед переводчиком
    const translatorCard = document.querySelector('.translator-card, .card, .translator');
    if (translatorCard) {
        translatorCard.parentNode.insertBefore(testResults, translatorCard);
    } else {
        document.body.insertBefore(testResults, document.body.firstChild);
    }
}

// Добавить кнопку тестирования на страницу
function addTestButton() {
    // Удаляем старую кнопку если есть
    const oldButton = document.getElementById('devTestButton');
    if (oldButton) oldButton.remove();

    // Создаем новую кнопку
    const testButton = document.createElement('button');
    testButton.id = 'devTestButton';
    testButton.className = 'btn btn-warning position-fixed';
    testButton.style.cssText = 'bottom: 20px; right: 20px; z-index: 1000;';
    testButton.innerHTML = '<i class="bi bi-bug me-1"></i>Тест системы';
    testButton.onclick = runAllTests;

    document.body.appendChild(testButton);
    console.log('🛠️ Кнопка тестирования добавлена');
}

// Автоматический запуск тестов при загрузке
document.addEventListener('DOMContentLoaded', function () {
    console.log('🧪 Тестовый скрипт инициализирован');

    // Добавляем кнопку тестирования
    addTestButton();

    // Запускаем автоматическое тестирование через 3 секунды
    setTimeout(() => {
        console.log('🔄 Автоматический запуск тестов через 3 секунды...');
    }, 3000);
});

// Делаем функции доступными глобально
window.runAllTests = runAllTests;
window.checkPageElements = checkPageElements;
window.testAPI = testAPI;
window.testTranslation = testTranslation;

console.log('🧪 Тестовый скрипт готов. Используйте runAllTests() для запуска тестов.');