using Linter.Interop;
using Linter.Keyboard;
using Linter.Services;
using Linter.Text;
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

    /// <summary>The same, for a test that needs its own set of languages.</summary>
    private readonly struct SpellCheckerOrNull
    {
        public SpellCheckerOrNull(IEnumerable<string> languages)
        {
            try { Value = new SpellChecker(languages); }
            catch (Exception) { Value = null; }
        }

        public SpellChecker? Value { get; }
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

    private static IntPtr LayoutFor(int languageId)
    {
        foreach (var layout in InstalledLayouts.Enumerate())
        {
            if ((layout.ToInt64() & 0xFFFF) == languageId) return layout;
        }
        return IntPtr.Zero;
    }

    /// <summary>The key strokes that type <paramref name="text"/> under a layout, as the hook would record them.</summary>
    private static WordSnapshot Typed(string text, IntPtr layout)
    {
        var strokes = new List<KeyStroke>();
        foreach (var ch in text)
        {
            var key = NativeMethods.VkKeyScanEx(ch, layout);
            var vk = (uint)(key & 0xFF);
            var modifiers = ((key >> 8) & 1) != 0 ? KeyModifiers.Shift : KeyModifiers.None;
            var scan = NativeMethods.MapVirtualKeyEx(vk, NativeConstants.MAPVK_VK_TO_VSC, layout);
            strokes.Add(new KeyStroke(vk, scan, modifiers, ch.ToString(), layout));
        }
        return new WordSnapshot(strokes, text, string.Empty);
    }

    [Fact]
    public void A_phrase_typed_in_the_wrong_layout_is_converted()
    {
        using var spell = TryCreate();
        var english = LayoutFor(0x0409);
        if (spell is null || !spell.IsLanguageSupported("ru-RU") || !spell.IsLanguageSupported("en-US")
            || english == IntPtr.Zero || !HasRussianLayout()) return;
        var converter = new LayoutConverter(new InputSimulator(), new LayoutSwitcher(), spell);

        Assert.Equal("привет", converter.ChooseWinner(Typed("ghbdtn", english), english)?.Text);
        Assert.Equal("hello", converter.ChooseWinner(Typed("руддщ", LayoutFor(0x0419)), LayoutFor(0x0419))?.Text);
    }

    [Fact]
    public void A_wrong_layout_phrase_with_a_typo_is_still_converted()
    {
        using var spell = TryCreate();
        var english = LayoutFor(0x0409);
        if (spell is null || !spell.IsLanguageSupported("ru-RU") || !spell.IsLanguageSupported("en-US")
            || english == IntPtr.Zero || !HasRussianLayout()) return;
        var converter = new LayoutConverter(new InputSimulator(), new LayoutSwitcher(), spell);

        // "ghdbtn" is "првиет": no real word in either alphabet, but clearly not meant as Latin.
        Assert.Equal("првиет", converter.ChooseWinner(Typed("ghdbtn", english), english)?.Text);
    }

    [Fact]
    public void A_phrase_of_real_words_is_never_converted_into_nonsense()
    {
        using var spell = TryCreate();
        var english = LayoutFor(0x0409);
        var russian = LayoutFor(0x0419);
        if (spell is null || !spell.IsLanguageSupported("ru-RU") || !spell.IsLanguageSupported("en-US")
            || english == IntPtr.Zero || russian == IntPtr.Zero) return;
        var converter = new LayoutConverter(new InputSimulator(), new LayoutSwitcher(), spell);

        Assert.Null(converter.ChooseWinner(Typed("hello", english), english));
        Assert.Null(converter.ChooseWinner(Typed("привет", russian), russian));
    }

    [Fact]
    public void A_russian_typo_that_is_a_ukrainian_word_is_caught_on_the_russian_layout()
    {
        using var spell = new SpellCheckerOrNull(new[] { "ru-RU", "uk-UA" }).Value;
        if (spell is null || !spell.IsLanguageSupported("ru-RU") || !spell.IsLanguageSupported("uk-UA")) return;
        var signalling = new[] { "ru-RU", "uk-UA" };

        // Either dictionary: Ukrainian accepts it, so there was no signal.
        Assert.True(spell.IsCorrect("ва", signalling));
        // Typed on the Russian layout, only Russian judges it.
        Assert.False(spell.IsCorrect("ва", SignalLanguages.For(signalling, "ru-RU")));
        Assert.True(spell.IsCorrect("ва", SignalLanguages.For(signalling, "uk-UA")));
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
