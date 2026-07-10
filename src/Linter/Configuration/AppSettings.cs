using System.Collections.Generic;

namespace Linter.Configuration;

internal sealed class AppSettings
{
    public bool Enabled { get; set; } = true;
    public bool SpellCheckEnabled { get; set; } = true;

    /// <summary>
    /// BCP-47 language tags whose dictionaries are loaded. Used by the
    /// layout converter to score conversion candidates. Words are still
    /// only checked against dictionaries of a matching script — a Cyrillic
    /// dictionary has no opinion on Latin words.
    /// </summary>
    public List<string> Languages { get; set; } = new()
    {
        "en-US",
        "ru-RU",
        "uk-UA",
        "es-ES",
    };

    /// <summary>
    /// Subset of <see cref="Languages"/> that triggers the audible
    /// "misspelled" signal on a word boundary. Defaults to Cyrillic only —
    /// most users do not want a beep on every English typo while their
    /// Cyrillic dictionaries are the primary thing they want monitored.
    /// </summary>
    public List<string> SpellCheckLanguages { get; set; } = new()
    {
        "ru-RU",
        "uk-UA",
    };

    public HotkeyConfig Hotkeys { get; set; } = new();
}
