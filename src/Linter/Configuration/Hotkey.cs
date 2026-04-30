using Linter.Text;

namespace Linter.Configuration;

/// <summary>
/// A single hotkey: virtual key + required modifier mask. CapsLock is
/// excluded from the comparison because it is a state, not a chord member.
/// </summary>
internal sealed class Hotkey
{
    public uint VirtualKey { get; set; }
    public KeyModifiers Modifiers { get; set; }

    public Hotkey() { }

    public Hotkey(uint virtualKey, KeyModifiers modifiers)
    {
        VirtualKey = virtualKey;
        Modifiers = modifiers;
    }

    public bool Matches(KeyStroke stroke)
        => stroke.VirtualKey == VirtualKey
        && Normalize(stroke.Modifiers) == Normalize(Modifiers);

    private static KeyModifiers Normalize(KeyModifiers mods) => mods & ~KeyModifiers.CapsLock;
}
