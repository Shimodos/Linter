using System;
using System.IO;

namespace Linter.Shell;

internal static class CrashReporter
{
    private static readonly object Sync = new();

    public static void Report(Exception? ex)
    {
        if (ex is null) return;
        try
        {
            var dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Linter");
            Directory.CreateDirectory(dir);
            var path = Path.Combine(dir, "crash.log");
            lock (Sync)
            {
                File.AppendAllText(path, $"[{DateTime.Now:O}] {ex}{Environment.NewLine}{Environment.NewLine}");
            }
        }
        catch
        {
            // Crash reporter must never throw.
        }
    }
}
