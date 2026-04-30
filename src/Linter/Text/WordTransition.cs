namespace Linter.Text;

/// <summary>
/// Result of feeding one keystroke into the buffer.
/// </summary>
internal readonly record struct WordTransition(bool WordCompleted, string CompletedWord)
{
    public static readonly WordTransition None = new(false, string.Empty);
    public static WordTransition Completed(string word) => new(true, word);
}
