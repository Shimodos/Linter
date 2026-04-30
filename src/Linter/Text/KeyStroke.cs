using System;

namespace Linter.Text;

/// <summary>
/// Immutable record of a single physical keypress: which key, with which
/// modifiers, under which keyboard layout, and what character(s) the OS
/// produced as a result. Storing the raw key data is what lets us re-render
/// the same keystroke under a *different* layout when converting.
/// </summary>
internal readonly record struct KeyStroke(
    uint VirtualKey,
    uint ScanCode,
    KeyModifiers Modifiers,
    string Output,
    IntPtr Layout)
{
    public bool IsBackspace => VirtualKey == VirtualKeyCodes.Back;
    public bool IsEnter     => VirtualKey == VirtualKeyCodes.Return;
    public bool IsSpace     => VirtualKey == VirtualKeyCodes.Space;
    public bool IsEscape    => VirtualKey == VirtualKeyCodes.Escape;

    public bool ProducesText => !string.IsNullOrEmpty(Output);
}
