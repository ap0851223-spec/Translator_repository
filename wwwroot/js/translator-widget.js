// =============================================
// ВИДЖЕТ ПЕРЕВОДЧИКА ДЛЯ ВСТРАИВАНИЯ НА САЙТЫ
// Версия 2.0 - Простая и надёжная
// =============================================
(function () {
    'use strict';

    // Не загружаем виджет повторно
    if (document.getElementById('tr-widget-btn')) return;

    // Языки
    var languages = [
        { code: 'ru', flag: '🇷🇺', name: 'Русский' },
        { code: 'en', flag: '🇬🇧', name: 'English' },
        { code: 'tt', flag: '🇹🇹', name: 'Татарский' },
        { code: 'es', flag: '🇪🇸', name: 'Español' },
        { code: 'fr', flag: '🇫🇷', name: 'Français' },
        { code: 'de', flag: '🇩🇪', name: 'Deutsch' },
        { code: 'tr', flag: '🇹🇷', name: 'Türkçe' },
        { code: 'zh', flag: '🇨🇳', name: '中文' }
    ];

    // Создаём кнопку
    var button = document.createElement('div');
    button.id = 'tr-widget-btn';
    button.innerHTML = '🌐';
    button.title = 'Перевести сайт';
    button.style.cssText = 'position:fixed;bottom:20px;right:20px;width:55px;height:55px;' +
        'border-radius:50%;background:#3498db;color:white;font-size:26px;' +
        'display:flex;align-items:center;justify-content:center;cursor:pointer;' +
        'box-shadow:0 4px 15px rgba(0,0,0,0.3);z-index:999999;' +
        'transition:transform 0.3s;user-select:none;';

    button.onmouseover = function () { this.style.transform = 'scale(1.1)'; };
    button.onmouseout = function () { this.style.transform = 'scale(1)'; };

    document.body.appendChild(button);

    // Создаём панель
    var panel = document.createElement('div');
    panel.id = 'tr-widget-panel';
    panel.style.cssText = 'position:fixed;bottom:85px;right:20px;background:white;' +
        'border-radius:12px;box-shadow:0 5px 30px rgba(0,0,0,0.3);' +
        'padding:12px;z-index:999998;display:none;min-width:180px;';

    // Заголовок
    var header = document.createElement('div');
    header.style.cssText = 'font-weight:bold;margin-bottom:8px;color:#2c3e50;font-size:13px;';
    header.textContent = 'Перевести на:';
    panel.appendChild(header);

    // Добавляем языки
    languages.forEach(function (lang) {
        var item = document.createElement('div');
        item.style.cssText = 'padding:7px 10px;cursor:pointer;border-radius:6px;' +
            'font-size:13px;color:#2c3e50;';
        item.innerHTML = lang.flag + ' ' + lang.name;

        item.onmouseover = function () { this.style.background = '#f0f0f0'; };
        item.onmouseout = function () { this.style.background = 'transparent'; };

        item.onclick = function () {
            var url = 'https://translate.google.com/translate?hl=' + lang.code +
                '&sl=auto&tl=' + lang.code +
                '&u=' + encodeURIComponent(window.location.href);
            window.open(url, '_blank');
            panel.style.display = 'none';
        };

        panel.appendChild(item);
    });

    // Кнопка закрытия
    var close = document.createElement('div');
    close.style.cssText = 'text-align:center;margin-top:6px;cursor:pointer;' +
        'color:#999;font-size:11px;';
    close.textContent = '✕ Закрыть';
    close.onclick = function () { panel.style.display = 'none'; };
    panel.appendChild(close);

    document.body.appendChild(panel);

    // Показ/скрытие панели
    button.onclick = function (e) {
        e.stopPropagation();
        panel.style.display = panel.style.display === 'block' ? 'none' : 'block';
    };

    // Закрытие при клике вне панели
    document.addEventListener('click', function (e) {
        if (e.target !== button && e.target !== panel && !panel.contains(e.target)) {
            panel.style.display = 'none';
        }
    });

    console.log('🌐 Виджет переводчика загружен!');
})();