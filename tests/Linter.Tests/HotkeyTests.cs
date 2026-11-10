using Linter.Configuration;
using Linter.Text;
using Xunit;

namespace Linter.Tests;

public class HotkeyTests
{
    [Fact]
    public void A_modifier_hotkey_matches_its_own_press()
    {
        var hotkey = new Hotkey(VirtualKeyCodes.RightAlt, KeyModifiers.None);

        // Pressing Alt is both "Alt pressed" and "Alt held".
        Assert.True(hotkey.Matches(Keys.Silent(VirtualKeyCodes.RightAlt, KeyModifiers.Alt)));
        Assert.True(hotkey.Matches(Keys.Silent(VirtualKeyCodes.RightAlt)));
        Assert.False(hotkey.Matches(Keys.Silent(VirtualKeyCodes.LeftAlt, KeyModifiers.Alt)));
    }

    [Fact]
    public void Extra_modifiers_do_not_match()
    {
        var hotkey = new Hotkey(VirtualKeyCodes.RightAlt, KeyModifiers.None);

        Assert.False(hotkey.Matches(Keys.Silent(VirtualKeyCodes.RightAlt, KeyModifiers.Alt | KeyModifiers.Shift)));
    }

    [Fact]
    public void A_chord_needs_exactly_its_modifiers()
    {
        var hotkey = new Hotkey(VirtualKeyCodes.Pause, KeyModifiers.Shift);

        Assert.True(hotkey.Matches(Keys.Silent(VirtualKeyCodes.Pause, KeyModifiers.Shift)));
        Assert.False(hotkey.Matches(Keys.Silent(VirtualKeyCodes.Pause)));
        Assert.False(hotkey.Matches(Keys.Silent(VirtualKeyCodes.Pause, KeyModifiers.Shift | KeyModifiers.Control)));
    }

    [Fact]
    public void Caps_lock_is_a_state_not_part_of_the_chord()
    {
        var hotkey = new Hotkey(VirtualKeyCodes.Pause, KeyModifiers.Shift);

        Assert.True(hotkey.Matches(Keys.Silent(VirtualKeyCodes.Pause, KeyModifiers.Shift | KeyModifiers.CapsLock)));
    }

    [Fact]
    public void An_unset_hotkey_never_matches()
    {
        Assert.False(Hotkey.None.IsSet);
        Assert.False(Hotkey.None.Matches(Keys.Silent(0)));
    }

    [Fact]
    public void SameAs_compares_key_and_modifiers()
    {
        var a = new Hotkey(VirtualKeyCodes.Pause, KeyModifiers.Shift);

        Assert.True(a.SameAs(new Hotkey(VirtualKeyCodes.Pause, KeyModifiers.Shift)));
        Assert.False(a.SameAs(new Hotkey(VirtualKeyCodes.Pause, KeyModifiers.None)));
        Assert.False(a.SameAs(new Hotkey(VirtualKeyCodes.Insert, KeyModifiers.Shift)));
    }

    [Theory]
    [InlineData(VirtualKeyCodes.RightAlt, (int)KeyModifiers.None, "Right Alt")]
    [InlineData(VirtualKeyCodes.Pause, (int)KeyModifiers.Shift, "Shift + Pause")]
    [InlineData(VirtualKeyCodes.F1 + 8, (int)KeyModifiers.None, "F9")]
    [InlineData((uint)'K', (int)(KeyModifiers.Control | KeyModifiers.Alt), "Ctrl + Alt + K")]
    [InlineData(0xE2u, (int)KeyModifiers.None, "Key 0xE2")]
    // The enum is internal, so it crosses the public test signature as an int.
    public void Hotkeys_are_described_as_printed_on_the_keyboard(uint vk, int mods, string expected)
        => Assert.Equal(expected, HotkeyNames.Describe(new Hotkey(vk, (KeyModifiers)mods)));
}
