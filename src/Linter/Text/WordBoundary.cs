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
        // A bare modifier changes nothing on screen. Checked first because
        // its own press already carries its modifier bit.
        if (VirtualKeyCodes.IsModifier(stroke.VirtualKey)) return StrokeKind.Ignored;

        // Ctrl+V, Ctrl+Z, Ctrl+Backspace, Alt+F, Win+D...: the application
        // does something we cannot follow, so what we buffered may no longer
        // match what is on screen.
        if (IsShortcut(stroke)) return StrokeKind.Invalidates;

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
              or VirtualKeyCodes.End
              or VirtualKeyCodes.PageUp
              or VirtualKeyCodes.PageDown
              // Delete removes text *after* the caret — text we never saw.
              or VirtualKeyCodes.Delete;

    private static bool IsShortcut(KeyStroke stroke)
    {
        var mods = stroke.Modifiers;
        if (mods.HasFlag(KeyModifiers.Win)) return true;

        var ctrl = mods.HasFlag(KeyModifiers.Control);
        var alt = mods.HasFlag(KeyModifiers.Alt);
        if (!ctrl && !alt) return false;

        // Ctrl+Alt together is AltGr, which types real characters on many
        // layouts (Spanish "@", Polish "ą"). Only then is it ordinary typing.
        if (ctrl && alt && stroke.ProducesText && !char.IsControl(stroke.Output[0])) return false;
        return true;
    }

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
    Invalidates,    // cursor moved or a shortcut ran — drop everything
    Ignored,        // modifier/dead-key/F-keys, do nothing
}
