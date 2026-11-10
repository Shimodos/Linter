namespace Linter.Text;

/// <summary>
/// Result of feeding one keystroke into the buffer.
/// <see cref="Boundary"/> is what the stroke that ended the word typed (a
/// space, a comma, "\r" for Enter) — it sits on screen right after the word,
/// so anything that rewrites the word has to delete and re-type it too.
/// </summary>
internal readonly record struct WordTransition(bool WordCompleted, string CompletedWord, string Boundary)
{
    public static readonly WordTransition None = new(false, string.Empty, string.Empty);
    public static WordTransition Completed(string word, string boundary) => new(true, word, boundary);
}
