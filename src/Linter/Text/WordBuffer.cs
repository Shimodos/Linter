using System.Collections.Generic;
using System.Text;

namespace Linter.Text;

/// <summary>
/// Tracks the word the user is currently typing. This is a pure model: it
/// holds no Win32 state, takes <see cref="KeyStroke"/>s in, returns
/// <see cref="WordTransition"/>s out, and exposes a <see cref="Snapshot"/>
/// for components that need to replay the word under a different layout.
/// </summary>
internal sealed class WordBuffer
{
    private readonly List<KeyStroke> _strokes = new();
    private readonly StringBuilder _text = new();

    public WordTransition Apply(KeyStroke stroke)
    {
        switch (WordBoundary.Classify(stroke))
        {
            case StrokeKind.Letter:
                Append(stroke);
                return WordTransition.None;

            case StrokeKind.Backspace:
                PopLast();
                return WordTransition.None;

            case StrokeKind.Boundary:
                var completed = _text.ToString();
                Reset();
                return completed.Length == 0
                    ? WordTransition.None
                    : WordTransition.Completed(completed);

            case StrokeKind.Invalidates:
                Reset();
                return WordTransition.None;

            case StrokeKind.Ignored:
            default:
                return WordTransition.None;
        }
    }

    public WordSnapshot Snapshot() => new(_strokes.ToArray(), _text.ToString());

    public void Reset()
    {
        _strokes.Clear();
        _text.Clear();
    }

    private void Append(KeyStroke stroke)
    {
        _strokes.Add(stroke);
        _text.Append(stroke.Output);
    }

    private void PopLast()
    {
        if (_strokes.Count == 0) return;
        var last = _strokes[^1];
        _strokes.RemoveAt(_strokes.Count - 1);
        if (last.Output.Length > 0 && _text.Length >= last.Output.Length)
        {
            _text.Length -= last.Output.Length;
        }
    }
}
