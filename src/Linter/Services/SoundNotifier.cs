using Linter.Interop;

namespace Linter.Services;

/// <summary>
/// Plays the audible signal that fires when a misspelling is detected.
/// Uses <c>MessageBeep</c> rather than <c>Beep(freq, dur)</c> because the
/// latter routes through the legacy beep driver (often missing on modern
/// laptops) while the former plays the configured system sound.
/// </summary>
internal sealed class SoundNotifier
{
    public void PlayMisspelledSignal()
    {
        // MessageBeep is non-blocking — important: we are inside the
        // low-level keyboard hook and any blocking call here risks
        // Windows disabling the hook (LowLevelHooksTimeout).
        if (NativeMethods.MessageBeep(NativeConstants.MB_ICONEXCLAMATION)) return;
        // Fallback for the rare host where MessageBeep fails.
        NativeMethods.Beep(800, 60);
    }
}
