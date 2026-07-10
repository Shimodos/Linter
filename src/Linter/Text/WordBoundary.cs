namespace Linter.Text;

/// <summary>
/// Decides what kind of edit a keystroke is, from the buffer's point of
/// view. The buffer tracks an entire *phrase* (multiple words separated
/// by spaces), so we distinguish the boundary that ends a word but keeps
/// the phrase alive (a space) from the one that ends the whole phrase
/// (punctuation, Enter, Tab — anything that signals "I'm done with that
/// thought").
/// </summary>
internal static class WordBoundary
{
    public static StrokeKind Classify(KeyStroke stroke)
    {
        if (stroke.IsBackspace) return StrokeKind.Backspace;
        if (IsCursorMovement(stroke.VirtualKey)) return StrokeKind.Invalidates;
        if (stroke.IsEscape) return StrokeKind.Invalidates;

        if (stroke.IsEnter || stroke.VirtualKey == VirtualKeyCodes.Tab)
            return StrokeKind.HardBoundary;

        if (stroke.IsSpace) return StrokeKind.SoftBoundary;
        if (!stroke.ProducesText) return StrokeKind.Ignored;

        return IsLetterLike(stroke.Output)
            ? StrokeKind.Letter
            : StrokeKind.HardBoundary;
    }

    private static bool IsCursorMovement(uint vk)
        => vk is VirtualKeyCodes.LeftArrow
              or VirtualKeyCodes.RightArrow
              or VirtualKeyCodes.UpArrow
              or VirtualKeyCodes.DownArrow
              or VirtualKeyCodes.Home
              or VirtualKeyCodes.End;

    private static bool IsLetterLike(string output)
    {
        foreach (var ch in output)
        {
            if (!char.IsLetter(ch) && ch != '\'' && ch != '-')
                return false;
        }
        return output.Length > 0;
    }
}

internal enum StrokeKind
{
    Letter,         // extends current word (and current phrase)
    Backspace,      // shrinks current word/phrase
    SoftBoundary,   // ends a word but keeps phrase alive (space, tab)
    HardBoundary,   // ends the whole phrase (punctuation, Enter)
    Invalidates,    // cursor moved — drop everything
    Ignored,        // modifier/dead-key/F-keys, do nothing
}
