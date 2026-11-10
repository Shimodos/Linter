using Linter.Text;
using Xunit;

namespace Linter.Tests;

public class WordBufferTests
{
    [Fact]
    public void Snapshot_holds_the_whole_phrase_being_typed()
    {
        var buffer = new WordBuffer();
        buffer.Type("ghbdtn vbh");

        var snapshot = buffer.Snapshot();

        Assert.Equal("ghbdtn vbh", snapshot.Text);
        Assert.Equal(string.Empty, snapshot.Trailing);
        Assert.Equal(10, snapshot.Strokes.Count);
    }

    [Fact]
    public void Space_completes_a_word_and_reports_its_boundary()
    {
        var buffer = new WordBuffer();
        buffer.Type("hello");

        var transition = buffer.Apply(Keys.Space);

        Assert.True(transition.WordCompleted);
        Assert.Equal("hello", transition.CompletedWord);
        Assert.Equal(" ", transition.Boundary);
    }

    [Fact]
    public void Only_the_last_word_of_the_phrase_is_reported()
    {
        var buffer = new WordBuffer();
        buffer.Type("one two");

        var transition = buffer.Apply(Keys.Comma);

        Assert.Equal("two", transition.CompletedWord);
        Assert.Equal(",", transition.Boundary);
    }

    [Fact]
    public void A_second_space_completes_nothing()
    {
        var buffer = new WordBuffer();
        buffer.Type("word ");

        Assert.False(buffer.Apply(Keys.Space).WordCompleted);
    }

    [Fact]
    public void Backspace_removes_the_last_stroke()
    {
        var buffer = new WordBuffer();
        buffer.Type("helloo");
        buffer.Apply(Keys.Backspace);

        Assert.Equal("hello", buffer.Snapshot().Text);
    }

    [Fact]
    public void Backspace_on_an_empty_buffer_is_harmless()
    {
        var buffer = new WordBuffer();
        buffer.Apply(Keys.Backspace);

        Assert.True(buffer.Snapshot().IsEmpty);
    }

    [Fact]
    public void After_punctuation_the_finished_phrase_is_still_convertible()
    {
        var buffer = new WordBuffer();
        buffer.Type("ghbdtn");
        buffer.Apply(Keys.Comma);

        var snapshot = buffer.Snapshot();

        Assert.Equal("ghbdtn", snapshot.Text);
        Assert.Equal(",", snapshot.Trailing);
        // The comma is on screen too and has to be deleted along with the word.
        Assert.Equal(7, snapshot.VisibleLength);
    }

    [Fact]
    public void Typing_on_replaces_the_finished_phrase()
    {
        var buffer = new WordBuffer();
        buffer.Type("first");
        buffer.Apply(Keys.Comma);
        buffer.Type("second");

        Assert.Equal("second", buffer.Snapshot().Text);
    }

    [Fact]
    public void Moving_the_caret_forgets_everything()
    {
        var buffer = new WordBuffer();
        buffer.Type("first");
        buffer.Apply(Keys.Comma);
        buffer.Type("second");

        buffer.Apply(Keys.Silent(VirtualKeyCodes.LeftArrow));

        Assert.True(buffer.Snapshot().IsEmpty);
    }

    [Fact]
    public void A_paste_forgets_everything()
    {
        var buffer = new WordBuffer();
        buffer.Type("typed");

        buffer.Apply(Keys.Typing('V', "\x16", KeyModifiers.Control));

        Assert.True(buffer.Snapshot().IsEmpty);
    }

    [Fact]
    public void Shift_between_letters_does_not_break_the_word()
    {
        var buffer = new WordBuffer();
        buffer.Apply(Keys.Silent(VirtualKeyCodes.LeftShift, KeyModifiers.Shift));
        buffer.Apply(Keys.Letter('H', KeyModifiers.Shift));
        buffer.Type("i");

        Assert.Equal("Hi", buffer.Snapshot().Text);
    }
}
