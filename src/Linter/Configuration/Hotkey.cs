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

    /// <summary>A hotkey with no key is switched off and never matches.</summary>
    [System.Text.Json.Serialization.JsonIgnore]
    public bool IsSet => VirtualKey != 0;

    public static Hotkey None => new(0, KeyModifiers.None);

    public bool Matches(KeyStroke stroke)
    {
        if (!IsSet || stroke.VirtualKey != VirtualKey) return false;
        // When the hotkey itself is a modifier (Alt, Ctrl, Shift, Win), its
        // own bit will be set in stroke.Modifiers — the press of Alt is
        // both "Alt held" and "Alt pressed". Mask it out before comparing.
        var captured = Normalize(stroke.Modifiers) & ~SelfModifier(VirtualKey);
        return captured == Normalize(Modifiers);
    }

    public bool SameAs(Hotkey other) => VirtualKey == other.VirtualKey && Normalize(Modifiers) == Normalize(other.Modifiers);

    private static KeyModifiers Normalize(KeyModifiers mods) => mods & ~KeyModifiers.CapsLock;

    private static KeyModifiers SelfModifier(uint vk) => vk switch
    {
        VirtualKeyCodes.LeftShift  or VirtualKeyCodes.RightShift => KeyModifiers.Shift,
        VirtualKeyCodes.LeftCtrl   or VirtualKeyCodes.RightCtrl  => KeyModifiers.Control,
        VirtualKeyCodes.LeftAlt    or VirtualKeyCodes.RightAlt   => KeyModifiers.Alt,
        VirtualKeyCodes.LeftWin    or VirtualKeyCodes.RightWin   => KeyModifiers.Win,
        _ => KeyModifiers.None,
    };
}
