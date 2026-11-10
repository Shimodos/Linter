using System.Collections.Generic;
using Linter.Text;

namespace Linter.Configuration;

/// <summary>
/// Turns a <see cref="Hotkey"/> into what is printed on the keyboard:
/// "Right Alt", "Shift + Pause". Key names stay in English in every
/// interface language, because that is what the keycaps say.
/// </summary>
internal static class HotkeyNames
{
    public static string Describe(Hotkey hotkey)
    {
        var parts = new List<string>(4);
        if (hotkey.Modifiers.HasFlag(KeyModifiers.Control)) parts.Add("Ctrl");
        if (hotkey.Modifiers.HasFlag(KeyModifiers.Alt)) parts.Add("Alt");
        if (hotkey.Modifiers.HasFlag(KeyModifiers.Shift)) parts.Add("Shift");
        if (hotkey.Modifiers.HasFlag(KeyModifiers.Win)) parts.Add("Win");
        parts.Add(KeyName(hotkey.VirtualKey));
        return string.Join(" + ", parts);
    }

    public static string KeyName(uint vk) => vk switch
    {
        VirtualKeyCodes.RightAlt   => "Right Alt",
        VirtualKeyCodes.LeftAlt    => "Left Alt",
        VirtualKeyCodes.RightCtrl  => "Right Ctrl",
        VirtualKeyCodes.LeftCtrl   => "Left Ctrl",
        VirtualKeyCodes.RightShift => "Right Shift",
        VirtualKeyCodes.LeftShift  => "Left Shift",
        VirtualKeyCodes.RightWin   => "Right Win",
        VirtualKeyCodes.LeftWin    => "Left Win",
        VirtualKeyCodes.CapsLock   => "Caps Lock",
        VirtualKeyCodes.Pause      => "Pause",
        VirtualKeyCodes.ScrollLock => "Scroll Lock",
        VirtualKeyCodes.Insert     => "Insert",
        VirtualKeyCodes.Delete     => "Delete",
        VirtualKeyCodes.Home       => "Home",
        VirtualKeyCodes.End        => "End",
        VirtualKeyCodes.PageUp     => "Page Up",
        VirtualKeyCodes.PageDown   => "Page Down",
        VirtualKeyCodes.Space      => "Space",
        VirtualKeyCodes.Tab        => "Tab",
        VirtualKeyCodes.Return     => "Enter",
        VirtualKeyCodes.Back       => "Backspace",
        >= VirtualKeyCodes.F1 and <= VirtualKeyCodes.F1 + 23 => $"F{vk - VirtualKeyCodes.F1 + 1}",
        >= '0' and <= '9' or >= 'A' and <= 'Z' => ((char)vk).ToString(),
        _ => $"Key 0x{vk:X2}",
    };
}
