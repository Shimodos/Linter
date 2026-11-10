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

    /// <summary>The layout the last dictionary-confirmed conversion went to.</summary>
    private IntPtr _lastTarget;

    public LayoutConverter(InputSimulator input, LayoutSwitcher switcher, SpellChecker spell)
    {
        _input = input;
        _switcher = switcher;
        _spell = spell;
    }

    /// <summary>
    /// Converts the snapshot in place. Returns the data needed to undo the
    /// edit, or <c>null</c> if nothing was changed (empty buffer, or the
    /// text already reads as real words and no other layout does better).
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
            Log.Trace("  -> nothing reads better than the text as it stands, leaving it");
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
    /// <para>
    /// If no rendering has a real word, the phrase may simply contain a typo
    /// ("ghdbtn" → "првиет"). The user pressed the hotkey because the text is
    /// wrong, so when the text as it stands has no real word either, it is
    /// still converted — to the other alphabet, where the spell signal and
    /// the fix hotkey can then deal with the typo. Text that already reads
    /// as real words is never rewritten into something meaningless.
    /// </para>
    /// </summary>
    internal LayoutCandidate? ChooseWinner(WordSnapshot snapshot, IntPtr currentLayout)
    {
        LayoutCandidate? best = null;
        var bestCorrect = 0;
        LayoutCandidate? fallback = null;
        var originalScript = WordScripts.Detect(snapshot.Text);

        foreach (var candidate in LayoutCandidates.ForOtherLayouts(snapshot, currentLayout))
        {
            // Russian and Ukrainian layouts type most words identically.
            // Retyping the same text just to switch the layout is not a conversion.
            if (candidate.Text == snapshot.Text) continue;

            var (correct, total) = CountCorrectWords(candidate.Text);
            Log.Trace($"  candidate layout=0x{candidate.Layout.ToInt64():X8} text=\"{candidate.Text}\" correct={correct}/{total}");
            if (correct > bestCorrect)
            {
                bestCorrect = correct;
                best = candidate;
            }

            // A fallback has to be in another alphabet: re-rendering Latin
            // text under another Latin layout changes next to nothing. Among
            // several (Russian, Ukrainian) prefer the one converted to last.
            var script = WordScripts.Detect(candidate.Text);
            if (script != WordScript.Unknown && script != originalScript
                && (fallback is null || candidate.Layout == _lastTarget))
            {
                fallback = candidate;
            }
        }

        if (bestCorrect > 0)
        {
            _lastTarget = best!.Value.Layout;
            return best;
        }

        if (fallback is not null && CountCorrectWords(snapshot.Text).Correct == 0)
        {
            Log.Trace("  -> no real words either way; converting to the other alphabet anyway");
            return fallback;
        }
        return null;
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
