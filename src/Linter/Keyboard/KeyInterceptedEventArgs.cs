using System;
using Linter.Text;

namespace Linter.Keyboard;

internal sealed class KeyInterceptedEventArgs : EventArgs
{
    public KeyInterceptedEventArgs(KeyStroke stroke, bool isRepeat)
    {
        Stroke = stroke;
        IsRepeat = isRepeat;
    }

    public KeyStroke Stroke { get; }

    /// <summary>
    /// <c>true</c> when this is keyboard auto-repeat — the key was already
    /// down. Typing repeats are real input; a repeated hotkey is not.
    /// </summary>
    public bool IsRepeat { get; }

    /// <summary>
    /// When set to <c>true</c> the keystroke is swallowed and not delivered
    /// to the foreground application — used by hotkeys.
    /// </summary>
    public bool Handled { get; set; }
}
