using System;
using System.Collections.Generic;
using System.IO;
using Linter.Configuration;
using Linter.Interop;

namespace Linter.Services;

/// <summary>
/// Plays the audible signal that fires when a misspelling is detected.
/// If a <c>.wav</c> is configured it is played; otherwise falls back
/// to <c>MessageBeep</c> (the configured system sound) rather than
/// <c>Beep(freq, dur)</c>, which routes through the legacy beep driver that
/// is often missing on modern laptops.
/// </summary>
internal sealed class SoundNotifier
{
    private readonly AppSettings _settings;

    public SoundNotifier(AppSettings settings)
    {
        _settings = settings;
    }

    /// <summary>Folder with the built-in sounds, next to the exe.</summary>
    public static string SoundsDir => Path.Combine(AppContext.BaseDirectory, "Sounds");

    /// <summary>File names (not paths) of the built-in sounds, sorted.</summary>
    public static IReadOnlyList<string> BuiltInSounds()
    {
        try
        {
            if (!Directory.Exists(SoundsDir)) return Array.Empty<string>();
            var names = new List<string>();
            foreach (var file in Directory.GetFiles(SoundsDir, "*.wav"))
            {
                names.Add(Path.GetFileName(file));
            }
            names.Sort(StringComparer.OrdinalIgnoreCase);
            return names;
        }
        catch
        {
            return Array.Empty<string>();
        }
    }

    /// <summary>A bare file name is a built-in sound; a rooted path is used as is.</summary>
    public static string ResolvePath(string soundFile)
        => Path.IsPathRooted(soundFile) ? soundFile : Path.Combine(SoundsDir, soundFile);

    // Reads the setting on every call, so a change made in the settings
    // window takes effect immediately.
    public void PlayMisspelledSignal() => Play(_settings.SoundFile);

    /// <summary>Plays <paramref name="soundFile"/>, or the system beep if it is null or missing.</summary>
    public static void Play(string? soundFile)
    {
        // Every path here is non-blocking (SND_ASYNC / MessageBeep) — the
        // signal may fire from work deferred off the LL keyboard hook, and
        // a blocking call risks Windows disabling the hook (LowLevelHooksTimeout).
        if (!string.IsNullOrWhiteSpace(soundFile))
        {
            var path = ResolvePath(soundFile);
            if (File.Exists(path))
            {
                var flags = NativeConstants.SND_FILENAME | NativeConstants.SND_ASYNC;
                if (NativeMethods.PlaySound(path, IntPtr.Zero, flags)) return;
            }
        }

        if (NativeMethods.MessageBeep(NativeConstants.MB_ICONEXCLAMATION)) return;
        // Fallback for the rare host where MessageBeep fails.
        NativeMethods.Beep(800, 60);
    }
}
