using System.IO;
using Linter.Interop;

namespace Linter.Services;

/// <summary>
/// Plays the audible signal that fires when a misspelling is detected.
/// If a custom <c>.wav</c> is configured it is played; otherwise falls back
/// to <c>MessageBeep</c> (the configured system sound) rather than
/// <c>Beep(freq, dur)</c>, which routes through the legacy beep driver that
/// is often missing on modern laptops.
/// </summary>
internal sealed class SoundNotifier
{
    private readonly string? _soundFile;

    public SoundNotifier(string? soundFile = null)
    {
        _soundFile = string.IsNullOrWhiteSpace(soundFile) ? null : soundFile;
    }

    public void PlayMisspelledSignal()
    {
        // Every path here is non-blocking (SND_ASYNC / MessageBeep) — the
        // signal may fire from work deferred off the LL keyboard hook, and
        // a blocking call risks Windows disabling the hook (LowLevelHooksTimeout).
        if (_soundFile is not null && File.Exists(_soundFile))
        {
            var flags = NativeConstants.SND_FILENAME | NativeConstants.SND_ASYNC;
            if (NativeMethods.PlaySound(_soundFile, System.IntPtr.Zero, flags)) return;
        }

        if (NativeMethods.MessageBeep(NativeConstants.MB_ICONEXCLAMATION)) return;
        // Fallback for the rare host where MessageBeep fails.
        NativeMethods.Beep(800, 60);
    }
}
