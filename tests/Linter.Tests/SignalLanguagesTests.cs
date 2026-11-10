using Linter.Services;
using Linter.Text;
using Xunit;

namespace Linter.Tests;

public class SignalLanguagesTests
{
    private static readonly string[] Signalling = { "ru-RU", "uk-UA" };

    [Fact]
    public void Only_the_language_of_the_layout_is_consulted()
    {
        Assert.Equal(new[] { "ru-RU" }, SignalLanguages.For(Signalling, "ru-RU"));
        Assert.Equal(new[] { "uk-UA" }, SignalLanguages.For(Signalling, "uk-UA"));
    }

    [Fact]
    public void A_regional_variant_of_the_layout_still_matches()
        => Assert.Equal(new[] { "en-US" }, SignalLanguages.For(new[] { "en-US", "ru-RU" }, "en-GB"));

    [Fact]
    public void A_layout_with_no_signalling_language_is_not_checked()
        => Assert.Empty(SignalLanguages.For(Signalling, "en-US"));

    [Fact]
    public void An_unknown_layout_does_not_narrow_the_choice()
    {
        Assert.Equal(Signalling, SignalLanguages.For(Signalling, null));
        Assert.Null(SignalLanguages.OfLayout(IntPtr.Zero));
    }

    [Theory]
    [InlineData(0x04190419, "ru-RU")]
    [InlineData(0x04090409, "en-US")]
    // Ukrainian (Enhanced): the high word is a device id, only the low word is the language.
    [InlineData(unchecked((long)0xFFFFFFFFF0A80422), "uk-UA")]
    public void The_language_comes_from_the_low_word_of_the_layout(long layout, string expected)
        => Assert.Equal(expected, SignalLanguages.OfLayout(new IntPtr(layout)));
}

public class WordLayoutTests
{
    [Fact]
    public void A_finished_word_reports_the_layout_it_was_typed_under()
    {
        var russian = new IntPtr(0x04190419);
        var buffer = new WordBuffer();
        foreach (var ch in "ва") buffer.Apply(new KeyStroke('A', 0, KeyModifiers.None, ch.ToString(), russian));

        var transition = buffer.Apply(Keys.Space);

        Assert.Equal("ва", transition.CompletedWord);
        // The space itself may come from anywhere; the word's own letters decide.
        Assert.Equal(russian, transition.Layout);
    }
}
