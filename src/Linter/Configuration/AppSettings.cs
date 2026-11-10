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

    /// <summary>
    /// Words the tool must leave alone: never beep on them, and never let
    /// them drag a conversion candidate's score. For nicknames, brands,
    /// slang and abbreviations no dictionary knows (e.g. "GitHub", "xD").
    /// Matched case-insensitively.
    /// </summary>
    public List<string> IgnoredWords { get; set; } = new();

    /// <summary>
    /// Foreground process names (without <c>.exe</c>) where the hook should
    /// stay completely passive — no conversion, no spell signal. Useful for
    /// remote-desktop clients, games, or terminals that resent injected
    /// input. Matched case-insensitively.
    /// </summary>
    public List<string> ExcludedProcesses { get; set; } = new();

    /// <summary>
    /// The <c>.wav</c> played on a detected misspelling. A bare file name
    /// ("tick.wav") is one of the built-in sounds shipped in the
    /// <c>Sounds</c> folder next to the exe; a rooted path is the user's own
    /// file. Null means the system beep, which is also the fallback if the
    /// file is missing.
    /// </summary>
    public string? SoundFile { get; set; }

    /// <summary>
    /// Writes a diagnostic trace to <c>%AppData%\Linter\debug.log</c>. Off by
    /// default: the trace contains the words being typed, so it should only
    /// exist while someone is actively debugging.
    /// </summary>
    public bool DebugLog { get; set; }

    /// <summary>
    /// Interface language: "en", "ru", "uk", or "auto" to follow Windows.
    /// </summary>
    public string Language { get; set; } = "auto";

    public HotkeyConfig Hotkeys { get; set; } = new();
}
