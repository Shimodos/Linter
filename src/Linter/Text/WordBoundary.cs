namespace Linter.Text;

/// <summary>
/// Decides what kind of edit a keystroke is, from the buffer's point of
/// view: does it extend the current word, end it, erase part of it, or
/// invalidate the buffer entirely (because the cursor has moved away)?
/// </summary>
internal static class WordBoundary
{
    public static StrokeKind Classify(KeyStroke stroke)
    {
        if (stroke.IsBackspace) return StrokeKind.Backspace;

        if (IsCursorMovement(stroke.VirtualKey)) return StrokeKind.Invalidates;
        if (stroke.IsEscape) return StrokeKind.Invalidates;

        if (stroke.IsEnter || stroke.IsSpace) return StrokeKind.Boundary;
        if (stroke.VirtualKey == VirtualKeyCodes.Tab) return StrokeKind.Boundary;

        if (!stroke.ProducesText) return StrokeKind.Ignored;

        return IsLetterLike(stroke.Output)
            ? StrokeKind.Letter
            : StrokeKind.Boundary;
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
    Letter,        // extends current word
    Backspace,     // shrinks current word
    Boundary,      // ends current word (space, punctuation, enter)
    Invalidates,   // cursor moved — drop buffer state
    Ignored,       // modifier/dead-key/F-keys etc., do nothing
}
