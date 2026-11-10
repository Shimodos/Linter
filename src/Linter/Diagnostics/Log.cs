using System;
using System.IO;
using System.Text;

namespace Linter.Diagnostics;

/// <summary>
/// Opt-in, append-only diagnostic log. Lives at <c>%AppData%\Linter\debug.log</c>.
/// Single static surface so any layer can call it without dependency
/// injection — there is exactly one log file per running instance, and
/// no log can ever block the keyboard hook on a missing-file error.
/// </summary>
internal static class Log
{
    private static readonly object Sync = new();

    // UTF-8 with BOM so Windows editors that auto-detect by BOM open the
    // file as UTF-8 instead of Windows-1251 (which mangled "→" into "â").
    private static readonly Encoding LogEncoding = new UTF8Encoding(encoderShouldEmitUTF8Identifier: true);

    /// <summary>Off until the app turns it on from the <c>debugLog</c> setting.</summary>
    public static bool Enabled { get; set; }

    public static string Path { get; } = System.IO.Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "Linter",
        "debug.log");

    public static void Trace(string message)
    {
        if (!Enabled) return;
        try
        {
            var dir = System.IO.Path.GetDirectoryName(Path);
            if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);
            lock (Sync)
            {
                File.AppendAllText(Path, $"[{DateTime.Now:HH:mm:ss.fff}] {message}{Environment.NewLine}", LogEncoding);
            }
        }
        catch
        {
            // Diagnostics must never break the host.
        }
    }

    public static void Reset()
    {
        try
        {
            lock (Sync)
            {
                if (File.Exists(Path)) File.Delete(Path);
            }
        }
        catch
        {
            // Best-effort.
        }
    }
}
