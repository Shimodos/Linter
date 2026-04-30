using System.Collections.Generic;

namespace Linter.Text;

/// <summary>
/// A point-in-time copy of the word that is currently being typed.
/// <see cref="Strokes"/> is what we replay under another layout;
/// <see cref="Text"/> is what is currently visible on screen.
/// </summary>
internal readonly record struct WordSnapshot(IReadOnlyList<KeyStroke> Strokes, string Text)
{
    public bool IsEmpty => Text.Length == 0;
}
