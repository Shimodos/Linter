using System;
using Linter.Text;

namespace Linter.Keyboard;

internal sealed class KeyInterceptedEventArgs : EventArgs
{
    public KeyInterceptedEventArgs(KeyStroke stroke)
    {
        Stroke = stroke;
    }

    public KeyStroke Stroke { get; }

    /// <summary>
    /// When set to <c>true</c> the keystroke is swallowed and not delivered
    /// to the foreground application — used by hotkeys.
    /// </summary>
    public bool Handled { get; set; }
}
