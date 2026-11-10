using System;

namespace Linter.Text;

/// <summary>
/// Result of feeding one keystroke into the buffer.
/// <see cref="Boundary"/> is what the stroke that ended the word typed (a
/// space, a comma, "\r" for Enter) — it sits on screen right after the word,
/// so anything that rewrites the word has to delete and re-type it too.
/// <see cref="Layout"/> is the keyboard layout the word's last letter was
/// typed under: it says which language the user meant to write in.
/// </summary>
internal readonly record struct WordTransition(bool WordCompleted, string CompletedWord, string Boundary, IntPtr Layout)
{
    public static readonly WordTransition None = new(false, string.Empty, string.Empty, IntPtr.Zero);
    public static WordTransition Completed(string word, string boundary, IntPtr layout) => new(true, word, boundary, layout);
}
