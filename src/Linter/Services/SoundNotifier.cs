using Linter.Interop;

namespace Linter.Services;

/// <summary>
/// Plays the audible signal that fires when a misspelling is detected.
/// Currently just a short beep — easy to swap for a custom .wav later.
/// </summary>
internal sealed class SoundNotifier
{
    public void PlayMisspelledSignal()
    {
        // Two short, low pips: distinctive but not jarring.
        NativeMethods.Beep(800, 60);
    }
}
