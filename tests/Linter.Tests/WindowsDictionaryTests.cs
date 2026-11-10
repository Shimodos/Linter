using Linter.Keyboard;
using Linter.Services;
using Xunit;

namespace Linter.Tests;

/// <summary>
/// These talk to the real Windows spell checker and the installed keyboard
/// layouts. A machine without the spell-checking service, the Russian
/// dictionary or the Russian layout (a CI runner) cannot run them, so each
/// one returns early instead of failing there.
/// </summary>
public class WindowsDictionaryTests
{
    private static readonly string[] Languages = { "en-US", "ru-RU" };

    /// <summary>Null where Windows has no spell-checking service at all.</summary>
    private static SpellChecker? TryCreate(IEnumerable<string>? ignored = null)
    {
        try { return new SpellChecker(Languages, ignored); }
        catch (Exception) { return null; }
    }

    private static bool HasRussianLayout()
    {
        foreach (var layout in InstalledLayouts.Enumerate())
        {
            // The low word of a layout handle is its language id; 0x0419 is Russian.
            if ((layout.ToInt64() & 0xFFFF) == 0x0419) return true;
        }
        return false;
    }

    [Fact]
    public void Every_listed_language_has_a_dictionary()
    {
        using var spell = TryCreate();
        if (spell is null) return;

        Assert.All(spell.InstalledLanguages(), tag => Assert.True(spell.IsLanguageSupported(tag), tag));
    }

    [Fact]
    public void A_misspelled_word_gets_a_suggestion()
    {
        using var spell = TryCreate();
        if (spell is null || !spell.IsLanguageSupported("en-US")) return;

        Assert.False(spell.IsCorrect("recieve", Languages));
        Assert.Equal("receive", spell.Suggest("recieve", Languages));
    }

    [Fact]
    public void A_correct_word_gets_no_suggestion()
    {
        using var spell = TryCreate();
        if (spell is null || !spell.IsLanguageSupported("en-US")) return;

        Assert.Null(spell.Suggest("receive", Languages));
    }

    [Fact]
    public void A_russian_typo_is_suggested_from_the_russian_dictionary()
    {
        using var spell = TryCreate();
        if (spell is null || !spell.IsLanguageSupported("ru-RU")) return;

        Assert.Equal("привет", spell.Suggest("превет", Languages));
    }

    [Fact]
    public void An_ignored_word_is_correct()
    {
        using var spell = TryCreate();
        if (spell is null || !spell.IsLanguageSupported("en-US")) return;

        Assert.False(spell.IsCorrect("qwfpgj", Languages));
        spell.Ignore("qwfpgj");
        Assert.True(spell.IsCorrect("QWFPGJ", Languages));
    }

    [Fact]
    public void Reconfigure_swaps_languages_and_ignore_list()
    {
        using var spell = TryCreate(new[] { "qwfpgj" });
        if (spell is null || !spell.IsLanguageSupported("en-US")) return;

        spell.Reconfigure(new[] { "en-US" }, Array.Empty<string>());

        Assert.Equal(new[] { "en-US" }, spell.SupportedLanguages);
        Assert.False(spell.IsCorrect("qwfpgj"));
    }

    [Fact]
    public void Selected_text_typed_in_the_wrong_layout_is_converted()
    {
        using var spell = TryCreate();
        if (spell is null || !spell.IsLanguageSupported("ru-RU") || !spell.IsLanguageSupported("en-US") || !HasRussianLayout()) return;
        var converter = new SelectionConverter(new InputSimulator(), new LayoutSwitcher(), spell);

        Assert.Equal("привет, мир", converter.Convert("ghbdtn? vbh")?.Text);
        Assert.Equal("hello world", converter.Convert("руддщ цщкдв")?.Text);
    }

    [Fact]
    public void Selected_text_that_already_reads_well_is_left_alone()
    {
        using var spell = TryCreate();
        if (spell is null || !spell.IsLanguageSupported("ru-RU") || !spell.IsLanguageSupported("en-US")) return;
        var converter = new SelectionConverter(new InputSimulator(), new LayoutSwitcher(), spell);

        Assert.Null(converter.Convert("hello world"));
        Assert.Null(converter.Convert("привет, мир"));
    }
}
