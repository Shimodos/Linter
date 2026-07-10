using System.Collections.Generic;
using System.Text;

namespace Linter.Text;

/// <summary>
/// Tracks the *phrase* the user is currently typing — every stroke since
/// the last hard boundary (Enter, punctuation, ...). The convert hotkey
/// works on the whole phrase, so a sentence typed in the wrong layout
/// can be repaired in one shot.
/// <para>
/// Two pieces of state:
/// </para>
/// <list type="bullet">
///   <item><b>current</b> — strokes since the last hard boundary, including
///   the spaces between words.</item>
///   <item><b>lastCompleted</b> — the previous phrase, kept around so that
///   pressing the hotkey *after* a sentence-ending character still works.</item>
/// </list>
/// </summary>
internal sealed class WordBuffer
{
    private readonly List<KeyStroke> _strokes = new();
    private WordSnapshot _lastCompleted = WordSnapshot.Empty;

    public WordTransition Apply(KeyStroke stroke)
    {
        switch (WordBoundary.Classify(stroke))
        {
            case StrokeKind.Letter:
                _strokes.Add(stroke);
                return WordTransition.None;

            case StrokeKind.Backspace:
                PopLast();
                return WordTransition.None;

            case StrokeKind.SoftBoundary:
                return OnSoftBoundary(stroke);

            case StrokeKind.HardBoundary:
                return OnHardBoundary(stroke);

            case StrokeKind.Invalidates:
                ResetAll();
                return WordTransition.None;

            case StrokeKind.Ignored:
            default:
                return WordTransition.None;
        }
    }

    /// <summary>What the converter should act on — current phrase, or the last finished one.</summary>
    public WordSnapshot Snapshot()
    {
        if (_strokes.Count > 0)
        {
            return new WordSnapshot(_strokes.ToArray(), Concat(_strokes), Trailing: string.Empty);
        }
        return _lastCompleted;
    }

    public void ResetAll()
    {
        _strokes.Clear();
        _lastCompleted = WordSnapshot.Empty;
    }

    private WordTransition OnSoftBoundary(KeyStroke stroke)
    {
        // The soft boundary completes a word but keeps the phrase alive,
        // so the boundary stroke itself joins the phrase as a separator.
        var word = ExtractTrailingWord();
        _strokes.Add(stroke);
        return word.Length == 0 ? WordTransition.None : WordTransition.Completed(word);
    }

    private WordTransition OnHardBoundary(KeyStroke stroke)
    {
        var word = ExtractTrailingWord();
        if (_strokes.Count > 0)
        {
            _lastCompleted = new WordSnapshot(_strokes.ToArray(), Concat(_strokes), stroke.Output);
        }
        _strokes.Clear();
        return word.Length == 0 ? WordTransition.None : WordTransition.Completed(word);
    }

    private void PopLast()
    {
        if (_strokes.Count == 0) return;
        _strokes.RemoveAt(_strokes.Count - 1);
    }

    /// <summary>Returns the run of letters between the last space and the end.</summary>
    private string ExtractTrailingWord()
    {
        var start = 0;
        for (var i = _strokes.Count - 1; i >= 0; i--)
        {
            if (_strokes[i].IsSpace) { start = i + 1; break; }
        }
        if (start >= _strokes.Count) return string.Empty;
        var sb = new StringBuilder();
        for (var i = start; i < _strokes.Count; i++) sb.Append(_strokes[i].Output);
        return sb.ToString();
    }

    private static string Concat(IReadOnlyList<KeyStroke> strokes)
    {
        var sb = new StringBuilder();
        foreach (var s in strokes) sb.Append(s.Output);
        return sb.ToString();
    }
}
