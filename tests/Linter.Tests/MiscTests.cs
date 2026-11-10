using System.Reflection;
using Linter.Localization;
using Linter.Services;
using Linter.Shell;
using Linter.Text;
using Xunit;

namespace Linter.Tests;

public class SelectionWordsTests
{
    [Fact]
    public void Punctuation_is_stripped_from_words()
        => Assert.Equal(new[] { "Hello", "world" }, SelectionConverter.SplitWords("Hello, world!"));

    [Fact]
    public void Inner_apostrophes_and_hyphens_are_kept()
        => Assert.Equal(new[] { "don't", "кто-то" }, SelectionConverter.SplitWords("don't кто-то"));

    [Fact]
    public void Leading_and_trailing_marks_are_not_words()
        => Assert.Equal(new[] { "word" }, SelectionConverter.SplitWords("- 'word' -"));

    [Fact]
    public void Line_breaks_separate_words()
        => Assert.Equal(new[] { "one", "two" }, SelectionConverter.SplitWords("one\r\ntwo"));
}

public class CleanLinesTests
{
    [Fact]
    public void Blanks_and_duplicates_are_dropped()
        => Assert.Equal(
            new[] { "GitHub", "xD" },
            SettingsForm.CleanLines(new[] { " GitHub ", "", "github", "xD" }, stripExe: false));

    [Fact]
    public void Process_names_lose_their_extension()
        => Assert.Equal(
            new[] { "mstsc", "Code" },
            SettingsForm.CleanLines(new[] { "mstsc.exe", "Code.EXE", "mstsc" }, stripExe: true));
}

public class WordScriptsTests
{
    [Theory]
    [InlineData("hello", (int)WordScript.Latin)]
    [InlineData("привіт", (int)WordScript.Cyrillic)]
    [InlineData("heллo", (int)WordScript.Unknown)]
    [InlineData("123", (int)WordScript.Unknown)]
    // The enum is internal, so it crosses the public test signature as an int.
    public void Script_is_detected_from_the_letters(string word, int expected)
        => Assert.Equal((WordScript)expected, WordScripts.Detect(word));
}

public class LocalizerTests
{
    [Fact]
    public void Every_string_is_translated_into_every_language()
    {
        var table = (Dictionary<string, string[]>)typeof(Localizer)
            .GetField("Table", BindingFlags.NonPublic | BindingFlags.Static)!
            .GetValue(null)!;

        Assert.NotEmpty(table);
        foreach (var (key, row) in table)
        {
            Assert.True(row.Length == Localizer.Languages.Length, $"{key}: expected {Localizer.Languages.Length} columns");
            Assert.All(row, text => Assert.False(string.IsNullOrWhiteSpace(text), $"{key}: empty translation"));
        }
    }

    [Fact]
    public void Placeholders_survive_translation()
    {
        Localizer.Use("ru");
        Assert.Equal("Игнорировать «тест»", Localizer.T("tray.ignore", "тест"));
        Localizer.Use("en");
        Assert.Equal("Ignore “test”", Localizer.T("tray.ignore", "test"));
    }

    [Fact]
    public void An_unknown_key_falls_back_to_itself()
        => Assert.Equal("no.such.key", Localizer.T("no.such.key"));
}
