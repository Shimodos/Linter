using System;
using Linter.Diagnostics;
using Linter.Keyboard;
using Linter.Text;

namespace Linter.Services;

/// <summary>
/// Phrase-level Punto: render the user's whole phrase under every other
/// installed layout, pick the one where the most words are real, and
/// rewrite it in place.
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

    /// <summary>
    /// Converts the snapshot in place. Returns the data needed to undo the
    /// edit, or <c>null</c> if nothing was changed (empty buffer, or no
    /// candidate produced a real word).
    /// </summary>
    public ConversionResult? ConvertLastWord(WordSnapshot snapshot)
    {
        Log.Trace($"convert hotkey | buffer=\"{snapshot.Text}\" trailing=\"{Visible(snapshot.Trailing)}\" strokes={snapshot.Strokes.Count}");
        if (snapshot.IsEmpty)
        {
            Log.Trace("  -> buffer empty, nothing to do");
            return null;
        }

        var current = _switcher.Current;
        Log.Trace($"  current layout=0x{current.ToInt64():X8}");

        var winner = ChooseWinner(snapshot, current);
        if (winner is null)
        {
            Log.Trace("  -> no candidate has any real words, leaving text as-is");
            return null;
        }

        Log.Trace($"  -> winner layout=0x{winner.Value.Layout.ToInt64():X8} text=\"{winner.Value.Text}\"");
        Replace(snapshot, winner.Value);
        return new ConversionResult(
            FromLayout: current,
            FromText:   snapshot.Text,
            ToLayout:   winner.Value.Layout,
            ToText:     winner.Value.Text,
            Trailing:   snapshot.Trailing);
    }

    /// <summary>Reverses a previous <see cref="ConvertLastWord"/> edit.</summary>
    public void Undo(ConversionResult conversion)
    {
        Log.Trace($"undo | restore \"{conversion.FromText}\" (was \"{conversion.ToText}\")");
        _input.Backspace(conversion.VisibleLength);
        _switcher.SwitchTo(conversion.FromLayout);
        _input.TypeUnicode(conversion.FromText + conversion.Trailing);
    }

    /// <summary>
    /// Picks the layout whose rendering has the most spell-correct words.
    /// Returns null if no candidate produces any real word — in that case
    /// the original text is left alone (better than rewriting it into
    /// something equally meaningless).
    /// </summary>
    private LayoutCandidate? ChooseWinner(WordSnapshot snapshot, IntPtr currentLayout)
    {
        LayoutCandidate? best = null;
        var bestCorrect = 0;

        foreach (var candidate in LayoutCandidates.ForOtherLayouts(snapshot, currentLayout))
        {
            var (correct, total) = CountCorrectWords(candidate.Text);
            Log.Trace($"  candidate layout=0x{candidate.Layout.ToInt64():X8} text=\"{candidate.Text}\" correct={correct}/{total}");
            if (correct > bestCorrect)
            {
                bestCorrect = correct;
                best = candidate;
            }
        }

        return bestCorrect > 0 ? best : null;
    }

    private (int Correct, int Total) CountCorrectWords(string phrase)
    {
        var words = phrase.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (words.Length == 0) return (0, 0);
        var correct = 0;
        foreach (var w in words)
        {
            if (_spell.IsCorrect(w)) correct++;
        }
        return (correct, words.Length);
    }

    private void Replace(WordSnapshot original, LayoutCandidate winner)
    {
        _input.Backspace(original.VisibleLength);
        _switcher.SwitchTo(winner.Layout);
        _input.TypeUnicode(winner.Text + original.Trailing);
    }

    private static string Visible(string s)
        => s.Replace("\r", "\\r").Replace("\n", "\\n").Replace("\t", "\\t");
}
