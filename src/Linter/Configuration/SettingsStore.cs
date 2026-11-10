using System;
using System.IO;
using System.Text.Json;

namespace Linter.Configuration;

/// <summary>
/// Reads and writes <see cref="AppSettings"/> as JSON in
/// <c>%AppData%\Linter\settings.json</c>. Failures fall back to defaults
/// rather than blocking startup.
/// </summary>
internal sealed class SettingsStore
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    public string Path => System.IO.Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "Linter",
        "settings.json");

    public AppSettings Load()
    {
        try
        {
            if (!File.Exists(Path)) return new AppSettings();
            using var stream = File.OpenRead(Path);
            return Normalize(JsonSerializer.Deserialize<AppSettings>(stream, JsonOptions) ?? new AppSettings());
        }
        catch
        {
            return new AppSettings();
        }
    }

    /// <summary>
    /// A hand-edited file may contain explicit <c>null</c>s ("languages": null),
    /// which the deserializer happily assigns over the defaults. The rest of
    /// the app assumes the collections exist.
    /// </summary>
    private static AppSettings Normalize(AppSettings settings)
    {
        var defaults = new AppSettings();
        settings.Languages ??= defaults.Languages;
        settings.SpellCheckLanguages ??= defaults.SpellCheckLanguages;
        settings.IgnoredWords ??= new();
        settings.ExcludedProcesses ??= new();
        settings.Hotkeys ??= new();
        settings.Hotkeys.Convert ??= defaults.Hotkeys.Convert;
        settings.Hotkeys.ConvertSelection ??= Hotkey.None;
        settings.Hotkeys.Fix ??= Hotkey.None;
        settings.Hotkeys.Ignore ??= Hotkey.None;
        if (string.IsNullOrWhiteSpace(settings.Language)) settings.Language = defaults.Language;
        return settings;
    }

    public void Save(AppSettings settings)
    {
        try
        {
            var dir = System.IO.Path.GetDirectoryName(Path);
            if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);
            // Write to a sibling file and swap it in, so a crash or power
            // loss mid-write can't leave a truncated settings.json behind.
            var temp = Path + ".tmp";
            using (var stream = File.Create(temp))
            {
                JsonSerializer.Serialize(stream, settings, JsonOptions);
            }
            File.Move(temp, Path, overwrite: true);
        }
        catch
        {
            // Settings persistence is best-effort. Refusing to launch
            // because we couldn't write a JSON file would be far worse
            // than running with in-memory state for this session.
        }
    }
}
