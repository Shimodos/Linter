using Linter.Text;

namespace Linter.Tests;

/// <summary>Builds the keystrokes a test needs without touching Win32.</summary>
internal static class Keys
{
    public static KeyStroke Letter(char ch, KeyModifiers mods = KeyModifiers.None)
        => new(char.ToUpperInvariant(ch), 0, mods, ch.ToString(), IntPtr.Zero);

    /// <summary>A key that types <paramref name="output"/> — punctuation, or a control character under Ctrl.</summary>
    public static KeyStroke Typing(uint vk, string output, KeyModifiers mods = KeyModifiers.None)
        => new(vk, 0, mods, output, IntPtr.Zero);

    /// <summary>A key that types nothing: arrows, F-keys, bare modifiers.</summary>
    public static KeyStroke Silent(uint vk, KeyModifiers mods = KeyModifiers.None)
        => new(vk, 0, mods, string.Empty, IntPtr.Zero);

    public static KeyStroke Space => Typing(VirtualKeyCodes.Space, " ");
    public static KeyStroke Enter => Typing(VirtualKeyCodes.Return, "\r");
    public static KeyStroke Backspace => Typing(VirtualKeyCodes.Back, "\b");
    public static KeyStroke Comma => Typing(0xBC, ",");

    public static void Type(this WordBuffer buffer, string text)
    {
        foreach (var ch in text)
        {
            buffer.Apply(ch == ' ' ? Space : Letter(ch));
        }
    }
}
