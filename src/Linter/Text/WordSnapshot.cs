using System;
using System.Collections.Generic;

namespace Linter.Text;

/// <summary>
/// A point-in-time copy of a word for the converter to act on.
/// <para>
/// <see cref="Strokes"/> — what we replay under another layout to build
/// candidate words.<br/>
/// <see cref="Text"/> — what is currently visible on screen.<br/>
/// <see cref="Trailing"/> — character(s) typed *after* the word that
/// have to be removed and re-typed when we replace it (a trailing space,
/// punctuation, newline, etc.). Empty for "still typing the word".
/// </para>
/// </summary>
internal readonly record struct WordSnapshot(
    IReadOnlyList<KeyStroke> Strokes,
    string Text,
    string Trailing)
{
    public static readonly WordSnapshot Empty = new(Array.Empty<KeyStroke>(), string.Empty, string.Empty);

    public bool IsEmpty => Text.Length == 0;

    /// <summary>Total number of characters on screen for this snapshot.</summary>
    public int VisibleLength => Text.Length + Trailing.Length;
}
