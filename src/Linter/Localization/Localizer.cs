using System;
using System.Collections.Generic;
using System.Globalization;

namespace Linter.Localization;

/// <summary>
/// Interface text in English, Russian and Ukrainian. Every string lives in
/// one table, one row per string, so a missing translation is visible at a
/// glance and a new language is one more column.
/// </summary>
internal static class Localizer
{
    /// <summary>Codes in column order; the first is the fallback.</summary>
    public static readonly string[] Languages = { "en", "ru", "uk" };

    /// <summary>Each language's own name, in column order — for the language picker.</summary>
    public static readonly string[] NativeNames = { "English", "Русский", "Українська" };

    private static int _column;

    public static string Current => Languages[_column];

    /// <summary>Selects a language: a code from <see cref="Languages"/>, or anything else to follow Windows.</summary>
    public static void Use(string? setting)
    {
        var index = Array.IndexOf(Languages, setting);
        if (index < 0) index = Array.IndexOf(Languages, CultureInfo.CurrentUICulture.TwoLetterISOLanguageName);
        _column = Math.Max(index, 0);
    }

    public static string T(string key)
        => Table.TryGetValue(key, out var row) ? row[_column] : key;

    public static string T(string key, params object[] args)
        => string.Format(CultureInfo.CurrentCulture, T(key), args);

    private static readonly Dictionary<string, string[]> Table = new()
    {
        // ---- tray ----
        ["tray.settings"]  = new[] { "Settings…", "Настройки…", "Налаштування…" },
        ["tray.enabled"]   = new[] { "Enabled", "Включён", "Увімкнено" },
        ["tray.autostart"] = new[] { "Start with Windows", "Запускать вместе с Windows", "Запускати разом із Windows" },
        ["tray.ignore"]    = new[] { "Ignore “{0}”", "Игнорировать «{0}»", "Ігнорувати «{0}»" },
        ["tray.openLog"]   = new[] { "Open log folder", "Открыть папку с логом", "Відкрити теку з логом" },
        ["tray.resetLog"]  = new[] { "Reset log", "Очистить лог", "Очистити лог" },
        ["tray.exit"]      = new[] { "Exit", "Выход", "Вихід" },
        ["tray.on"]        = new[] { "Linter — on", "Linter — включён", "Linter — увімкнено" },
        ["tray.off"]       = new[] { "Linter — off", "Linter — выключен", "Linter — вимкнено" },

        // ---- settings window ----
        ["settings.title"] = new[] { "Linter — Settings", "Linter — настройки", "Linter — налаштування" },
        ["button.ok"]      = new[] { "OK", "ОК", "ОК" },
        ["button.cancel"]  = new[] { "Cancel", "Отмена", "Скасувати" },

        ["tab.general"]    = new[] { "General", "Общие", "Загальні" },
        ["tab.hotkeys"]    = new[] { "Hotkeys", "Горячие клавиши", "Гарячі клавіші" },
        ["tab.spelling"]   = new[] { "Spelling", "Орфография", "Орфографія" },
        ["tab.exceptions"] = new[] { "Exceptions", "Исключения", "Винятки" },

        ["general.enable"]    = new[] { "Enable Linter", "Включить Linter", "Увімкнути Linter" },
        ["general.autostart"] = new[] { "Start with Windows", "Запускать вместе с Windows", "Запускати разом із Windows" },
        ["general.language"]  = new[] { "Interface language", "Язык интерфейса", "Мова інтерфейсу" },
        ["general.langAuto"]  = new[] { "Same as Windows", "Как в Windows", "Як у Windows" },
        ["general.diagnostics"] = new[] { "Diagnostics", "Диагностика", "Діагностика" },
        ["general.debugLog"]  = new[] { "Write a debug log", "Вести отладочный лог", "Вести налагоджувальний лог" },
        ["general.debugLogHint"] = new[]
        {
            "The log records the words you type, so keep it off unless you are tracking down a problem. It lives in %AppData%\\Linter.",
            "В лог попадают слова, которые вы набираете, поэтому включайте его только на время поиска проблемы. Файл лежит в %AppData%\\Linter.",
            "У лог потрапляють слова, які ви набираєте, тож вмикайте його лише на час пошуку проблеми. Файл лежить у %AppData%\\Linter.",
        },

        ["hotkey.convert"]     = new[] { "Convert layout", "Сменить раскладку", "Змінити розкладку" },
        ["hotkey.convertHint"] = new[]
        {
            "Re-types the phrase you just typed in the right layout. Press again to undo.",
            "Перепечатывает только что набранную фразу в правильной раскладке. Повторное нажатие — отмена.",
            "Передруковує щойно набрану фразу в правильній розкладці. Повторне натискання — скасування.",
        },
        ["hotkey.selection"]     = new[] { "Convert selection", "Сменить раскладку выделенного", "Змінити розкладку виділеного" },
        ["hotkey.selectionHint"] = new[]
        {
            "Does the same for the selected text.",
            "То же самое для выделенного текста.",
            "Те саме для виділеного тексту.",
        },
        ["hotkey.fix"]     = new[] { "Fix the word", "Исправить слово", "Виправити слово" },
        ["hotkey.fixHint"] = new[]
        {
            "Right after a signal: replaces the word with the dictionary's suggestion.",
            "Сразу после сигнала: заменяет слово вариантом из словаря.",
            "Одразу після сигналу: замінює слово варіантом зі словника.",
        },
        ["hotkey.ignore"]     = new[] { "Ignore the word", "Игнорировать слово", "Ігнорувати слово" },
        ["hotkey.ignoreHint"] = new[]
        {
            "Right after a signal: adds the word to the ignored list.",
            "Сразу после сигнала: добавляет слово в список игнорируемых.",
            "Одразу після сигналу: додає слово до списку ігнорованих.",
        },
        ["hotkey.change"]  = new[] { "Change…", "Изменить…", "Змінити…" },
        ["hotkey.clear"]   = new[] { "Clear", "Сбросить", "Скинути" },
        ["hotkey.none"]    = new[] { "Not set", "Не задана", "Не задано" },
        ["hotkey.press"]   = new[] { "Press a key… (Esc cancels)", "Нажмите клавишу… (Esc — отмена)", "Натисніть клавішу… (Esc — скасувати)" },
        ["hotkey.taken"]   = new[]
        {
            "{0} is already used for “{1}”.",
            "{0} уже назначена на «{1}».",
            "{0} вже призначено на «{1}».",
        },
        ["hotkey.passThrough"] = new[]
        {
            "“Fix” and “Ignore” are only intercepted right after a signal. The rest of the time those keys work as usual.",
            "«Исправить» и «Игнорировать» перехватываются только сразу после сигнала. В остальное время эти клавиши работают как обычно.",
            "«Виправити» та «Ігнорувати» перехоплюються лише одразу після сигналу. В інший час ці клавіші працюють як зазвичай.",
        },

        ["spelling.signal"]    = new[] { "Play a signal when a word is misspelled", "Подавать сигнал, когда слово написано с ошибкой", "Подавати сигнал, коли слово написано з помилкою" },
        ["spelling.languages"] = new[] { "Languages", "Языки", "Мови" },
        ["spelling.languagesHint"] = new[]
        {
            "Every language listed is used to pick the right layout when converting. Tick the ones that should also signal a misspelling.",
            "Все языки из списка участвуют в выборе раскладки при конвертации. Отметьте те, для которых нужен сигнал об ошибке.",
            "Усі мови зі списку беруть участь у виборі розкладки під час конвертації. Позначте ті, для яких потрібен сигнал про помилку.",
        },
        ["spelling.add"]       = new[] { "Add", "Добавить", "Додати" },
        ["spelling.remove"]    = new[] { "Remove selected", "Удалить выбранный", "Вилучити вибрану" },
        ["spelling.noDictionary"] = new[] { "dictionary not installed", "словарь не установлен", "словник не встановлено" },
        ["spelling.sound"]     = new[] { "Signal sound", "Звук сигнала", "Звук сигналу" },
        ["spelling.systemBeep"] = new[] { "System beep", "Системный сигнал", "Системний сигнал" },
        ["spelling.customFile"] = new[] { "Custom file…", "Свой файл…", "Власний файл…" },
        ["spelling.test"]      = new[] { "▶ Test", "▶ Проверить", "▶ Перевірити" },
        ["spelling.browse"]    = new[] { "Browse…", "Обзор…", "Огляд…" },
        ["spelling.chooseSound"] = new[] { "Choose a signal sound", "Выберите звук сигнала", "Виберіть звук сигналу" },
        ["spelling.waveFilter"] = new[] { "Wave audio (*.wav)|*.wav", "Аудио WAV (*.wav)|*.wav", "Аудіо WAV (*.wav)|*.wav" },

        ["exceptions.words"]     = new[] { "Ignored words", "Игнорируемые слова", "Ігноровані слова" },
        ["exceptions.wordsHint"] = new[]
        {
            "One per line. Never signalled and never held against a conversion — names, brands, slang.",
            "По одному на строку. Для них нет сигнала, и они не мешают конвертации — имена, бренды, сленг.",
            "По одному на рядок. Для них немає сигналу, і вони не заважають конвертації — імена, бренди, сленг.",
        },
        ["exceptions.apps"]     = new[] { "Excluded apps", "Исключённые приложения", "Виключені застосунки" },
        ["exceptions.appsHint"] = new[]
        {
            "One process name per line (e.g. mstsc). Linter stays completely passive while they are in the foreground.",
            "По одному имени процесса на строку (например, mstsc). Пока такое приложение активно, Linter ничего не делает.",
            "По одному імені процесу на рядок (наприклад, mstsc). Поки такий застосунок активний, Linter нічого не робить.",
        },
    };
}
