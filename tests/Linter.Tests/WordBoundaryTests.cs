using Linter.Text;
using Xunit;

namespace Linter.Tests;

public class WordBoundaryTests
{
    [Fact]
    public void Letters_extend_the_word()
    {
        Assert.Equal(StrokeKind.Letter, WordBoundary.Classify(Keys.Letter('a')));
        Assert.Equal(StrokeKind.Letter, WordBoundary.Classify(Keys.Letter('ж')));
        Assert.Equal(StrokeKind.Letter, WordBoundary.Classify(Keys.Letter('A', KeyModifiers.Shift)));
    }

    [Fact]
    public void Apostrophe_and_hyphen_stay_inside_a_word()
    {
        Assert.Equal(StrokeKind.Letter, WordBoundary.Classify(Keys.Typing(0xDE, "'")));
        Assert.Equal(StrokeKind.Letter, WordBoundary.Classify(Keys.Typing(0xBD, "-")));
    }

    [Fact]
    public void Space_ends_the_word_but_not_the_phrase()
        => Assert.Equal(StrokeKind.SoftBoundary, WordBoundary.Classify(Keys.Space));

    [Fact]
    public void Punctuation_and_enter_end_the_phrase()
    {
        Assert.Equal(StrokeKind.HardBoundary, WordBoundary.Classify(Keys.Comma));
        Assert.Equal(StrokeKind.HardBoundary, WordBoundary.Classify(Keys.Enter));
    }

    [Fact]
    public void Backspace_is_tracked()
        => Assert.Equal(StrokeKind.Backspace, WordBoundary.Classify(Keys.Backspace));

    [Theory]
    [InlineData(VirtualKeyCodes.LeftArrow)]
    [InlineData(VirtualKeyCodes.UpArrow)]
    [InlineData(VirtualKeyCodes.Home)]
    [InlineData(VirtualKeyCodes.End)]
    [InlineData(VirtualKeyCodes.PageUp)]
    [InlineData(VirtualKeyCodes.PageDown)]
    [InlineData(VirtualKeyCodes.Delete)]
    [InlineData(VirtualKeyCodes.Escape)]
    public void Moving_the_caret_drops_the_buffer(uint vk)
        => Assert.Equal(StrokeKind.Invalidates, WordBoundary.Classify(Keys.Silent(vk)));

    [Fact]
    public void Ctrl_shortcuts_drop_the_buffer()
    {
        // Under Ctrl, ToUnicodeEx reports control characters: Ctrl+V is "\x16".
        Assert.Equal(StrokeKind.Invalidates, WordBoundary.Classify(Keys.Typing('V', "\x16", KeyModifiers.Control)));
        Assert.Equal(StrokeKind.Invalidates, WordBoundary.Classify(Keys.Typing('Z', "\x1A", KeyModifiers.Control)));
        Assert.Equal(StrokeKind.Invalidates, WordBoundary.Classify(Keys.Silent('A', KeyModifiers.Control)));
    }

    [Fact]
    public void Ctrl_backspace_is_a_shortcut_not_a_single_backspace()
        => Assert.Equal(StrokeKind.Invalidates,
            WordBoundary.Classify(Keys.Typing(VirtualKeyCodes.Back, "\x7F", KeyModifiers.Control)));

    [Fact]
    public void Alt_and_win_shortcuts_drop_the_buffer()
    {
        Assert.Equal(StrokeKind.Invalidates, WordBoundary.Classify(Keys.Letter('f', KeyModifiers.Alt)));
        // Win is not part of the keyboard state ToUnicodeEx sees, so Win+D still "types" a d.
        Assert.Equal(StrokeKind.Invalidates, WordBoundary.Classify(Keys.Letter('d', KeyModifiers.Win)));
    }

    [Fact]
    public void AltGr_characters_are_ordinary_typing()
    {
        var altGr = KeyModifiers.Control | KeyModifiers.Alt;
        Assert.Equal(StrokeKind.Letter, WordBoundary.Classify(Keys.Typing('A', "ą", altGr)));
        Assert.Equal(StrokeKind.HardBoundary, WordBoundary.Classify(Keys.Typing('2', "@", altGr)));
        // Ctrl+Alt+key that types nothing is a shortcut like any other.
        Assert.Equal(StrokeKind.Invalidates, WordBoundary.Classify(Keys.Silent(VirtualKeyCodes.Delete, altGr)));
    }

    [Theory]
    [InlineData(VirtualKeyCodes.LeftShift, (int)KeyModifiers.Shift)]
    [InlineData(VirtualKeyCodes.RightCtrl, (int)KeyModifiers.Control)]
    [InlineData(VirtualKeyCodes.RightAlt, (int)KeyModifiers.Alt)]
    [InlineData(VirtualKeyCodes.LeftWin, (int)KeyModifiers.Win)]
    [InlineData(VirtualKeyCodes.CapsLock, (int)KeyModifiers.None)]
    // The enum is internal, so it crosses the public test signature as an int.
    public void A_bare_modifier_changes_nothing(uint vk, int ownBit)
        => Assert.Equal(StrokeKind.Ignored, WordBoundary.Classify(Keys.Silent(vk, (KeyModifiers)ownBit)));

    [Fact]
    public void Function_keys_are_ignored()
        => Assert.Equal(StrokeKind.Ignored, WordBoundary.Classify(Keys.Silent(VirtualKeyCodes.F1 + 4)));
}
