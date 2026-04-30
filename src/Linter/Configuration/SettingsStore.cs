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
            return JsonSerializer.Deserialize<AppSettings>(stream, JsonOptions) ?? new AppSettings();
        }
        catch
        {
            return new AppSettings();
        }
    }

    public void Save(AppSettings settings)
    {
        try
        {
            var dir = System.IO.Path.GetDirectoryName(Path);
            if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);
            using var stream = File.Create(Path);
            JsonSerializer.Serialize(stream, settings, JsonOptions);
        }
        catch
        {
            // Settings persistence is best-effort. Refusing to launch
            // because we couldn't write a JSON file would be far worse
            // than running with in-memory state for this session.
        }
    }
}
