using System.Collections.Generic;

namespace Linter.Configuration;

internal sealed class AppSettings
{
    public bool Enabled { get; set; } = true;
    public bool SpellCheckEnabled { get; set; } = true;

    /// <summary>
    /// BCP-47 language tags to spell-check against. The first language a
    /// word is valid in counts as "correct". Defaults match what the user
    /// most commonly has installed on Windows.
    /// </summary>
    public List<string> Languages { get; set; } = new()
    {
        "en-US",
        "ru-RU",
        "uk-UA",
        "es-ES",
    };

    public HotkeyConfig Hotkeys { get; set; } = new();
}
