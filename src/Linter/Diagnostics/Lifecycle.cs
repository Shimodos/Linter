using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Text;
using Microsoft.Win32;

namespace Linter.Diagnostics;

/// <summary>
/// A small always-on record of when the app started and how it stopped —
/// never of anything typed. It exists to answer one question after the fact:
/// "Linter is gone; did it exit, crash, or was it killed, and when?"
/// <list type="bullet">
///   <item><c>lifecycle.log</c> — one line per start, stop, sleep, resume and session end.</item>
///   <item><c>alive.txt</c> — rewritten once a minute while running. If the log has no
///   stop line after the last start, the process was terminated from outside, and
///   this file says when it was last seen alive.</item>
/// </list>
/// </summary>
internal static class Lifecycle
{
    private const long MaxLogBytes = 256 * 1024;
    private static readonly object Sync = new();
    private static readonly Encoding Utf8Bom = new UTF8Encoding(encoderShouldEmitUTF8Identifier: true);

    private static string Folder => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Linter");

    public static string LogPath => Path.Combine(Folder, "lifecycle.log");
    public static string AlivePath => Path.Combine(Folder, "alive.txt");

    /// <summary>Writes the start line and subscribes to everything that can end or pause the process.</summary>
    public static void Begin()
    {
        var version = Assembly.GetExecutingAssembly().GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion ?? "?";
        // What was known about the previous run, before this one overwrites it.
        var previous = ReadPreviousHeartbeat();
        if (previous is not null) Record($"previous run was last seen alive at {previous}");

        Record($"START pid={Environment.ProcessId} version={version} runtime={Environment.Version} exe={Environment.ProcessPath}");

        AppDomain.CurrentDomain.ProcessExit += (_, _) => Record("STOP process is exiting normally");
        AppDomain.CurrentDomain.UnhandledException += (_, e) =>
            Record($"STOP unhandled exception: {(e.ExceptionObject as Exception)?.GetType().FullName}: {(e.ExceptionObject as Exception)?.Message}");

        try
        {
            SystemEvents.SessionEnding += (_, e) => Record($"session ending: {e.Reason}");
            SystemEvents.SessionEnded += (_, e) => Record($"session ended: {e.Reason}");
            SystemEvents.PowerModeChanged += (_, e) =>
            {
                if (e.Mode != PowerModes.StatusChange) Record($"power: {e.Mode}");
            };
        }
        catch (Exception ex)
        {
            Record($"system events unavailable: {ex.Message}");
        }
    }

    public static void Record(string message)
    {
        try
        {
            lock (Sync)
            {
                Directory.CreateDirectory(Folder);
                var file = new FileInfo(LogPath);
                if (file.Exists && file.Length > MaxLogBytes) file.Delete();
                File.AppendAllText(LogPath, $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {message}{Environment.NewLine}", Utf8Bom);
            }
        }
        catch
        {
            // Diagnostics must never break the host.
        }
    }

    /// <summary>Called once a minute: "still running at this time".</summary>
    public static void Heartbeat()
    {
        try
        {
            Directory.CreateDirectory(Folder);
            File.WriteAllText(AlivePath, $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} pid={Environment.ProcessId}");
        }
        catch
        {
            // Same: best-effort.
        }
    }

    private static string? ReadPreviousHeartbeat()
    {
        try { return File.Exists(AlivePath) ? File.ReadAllText(AlivePath).Trim() : null; }
        catch { return null; }
    }
}
