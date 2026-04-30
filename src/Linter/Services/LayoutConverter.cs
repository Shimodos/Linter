using System;
using Linter.Keyboard;
using Linter.Text;

namespace Linter.Services;

/// <summary>
/// The Punto-style core: given the word the user just typed under the
/// "wrong" layout, find the layout under which it spells a real word, and
/// rewrite it in place — backspace the original, switch layouts, retype.
/// </summary>
internal sealed class LayoutConverter
{
    private readonly InputSimulator _input;
    private readonly LayoutSwitcher _switcher;
    private readonly SpellChecker _spell;

    public LayoutConverter(InputSimulator input, LayoutSwitcher switcher, SpellChecker spell)
    {
        _input = input;
        _switcher = switcher;
        _spell = spell;
    }

    public bool ConvertLastWord(WordSnapshot snapshot)
    {
        if (snapshot.IsEmpty) return false;

        var current = _switcher.Current;
        var winner = ChooseBestCandidate(snapshot, current);
        if (winner is null) return false;

        Replace(snapshot.Text, winner.Value);
        return true;
    }

    private LayoutCandidate? ChooseBestCandidate(WordSnapshot snapshot, IntPtr currentLayout)
    {
        LayoutCandidate? fallback = null;

        foreach (var candidate in LayoutCandidates.ForOtherLayouts(snapshot, currentLayout))
        {
            if (_spell.IsCorrect(candidate.Text)) return candidate;
            fallback ??= candidate;
        }

        // No candidate spells a real word. If the original text is itself
        // misspelled we still offer the first alternative; otherwise we
        // refuse to "fix" something that wasn't broken.
        return _spell.IsCorrect(snapshot.Text) ? null : fallback;
    }

    private void Replace(string original, LayoutCandidate winner)
    {
        _input.Backspace(original.Length);
        _switcher.SwitchTo(winner.Layout);
        _input.TypeUnicode(winner.Text);
    }
}
