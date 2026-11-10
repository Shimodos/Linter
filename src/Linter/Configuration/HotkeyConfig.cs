using Linter.Text;

namespace Linter.Configuration;

internal sealed class HotkeyConfig
{
    /// <summary>Re-types the phrase just typed under the right keyboard layout; again to undo.</summary>
    public Hotkey Convert { get; set; } = new(VirtualKeyCodes.RightAlt, KeyModifiers.None);

    /// <summary>Does the same for the text currently selected.</summary>
    public Hotkey ConvertSelection { get; set; } = new(VirtualKeyCodes.Pause, KeyModifiers.Shift);

    /// <summary>
    /// Replaces the word that was just signalled with the dictionary's first
    /// suggestion. Only intercepted right after a signal — the rest of the
    /// time the key reaches the application untouched.
    /// </summary>
    public Hotkey Fix { get; set; } = new(VirtualKeyCodes.RightCtrl, KeyModifiers.None);

    /// <summary>
    /// Adds the word that was just signalled to the ignore list. Like
    /// <see cref="Fix"/>, only intercepted right after a signal.
    /// </summary>
    public Hotkey Ignore { get; set; } = new(VirtualKeyCodes.F1 + 8, KeyModifiers.None); // F9
}
