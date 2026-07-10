namespace Linter.Text;

/// <summary>
/// Distinguishes word scripts so the spell checker only consults
/// dictionaries that *can* judge a given word. A Russian dictionary has
/// no opinion on Latin gibberish — it will report "no errors" simply
/// because none of the input characters are in its alphabet — which
/// would otherwise make every nonsense Latin word look spelled-correctly.
/// </summary>
internal static class WordScripts
{
    public static WordScript Detect(string word)
    {
        var latin = 0;
        var cyrillic = 0;
        foreach (var ch in word)
        {
            if (IsLatinLetter(ch)) latin++;
            else if (IsCyrillicLetter(ch)) cyrillic++;
        }
        if (latin > 0 && cyrillic == 0) return WordScript.Latin;
        if (cyrillic > 0 && latin == 0) return WordScript.Cyrillic;
        return WordScript.Unknown;
    }

    /// <summary>
    /// Maps a BCP-47 language tag to the script its dictionary covers.
    /// Falls back to <see cref="WordScript.Unknown"/> for languages we
    /// can't classify confidently — those are checked against all words
    /// (better permissive than ignoring the user's installed dictionary).
    /// </summary>
    public static WordScript OfLanguage(string bcp47Tag)
    {
        var lang = bcp47Tag.Split('-')[0].ToLowerInvariant();
        return lang switch
        {
            "en" or "es" or "fr" or "de" or "it" or "pt" or "pl" or "nl" or "tr" or "sv" or "no" or "da" or "fi" => WordScript.Latin,
            "ru" or "uk" or "be" or "bg" or "sr" or "mk" or "kk" or "mn" => WordScript.Cyrillic,
            _ => WordScript.Unknown,
        };
    }

    private static bool IsLatinLetter(char ch)
        => (ch >= 'a' && ch <= 'z')
        || (ch >= 'A' && ch <= 'Z')
        || (ch >= 'À' && ch <= 'ɏ'); // Latin-1 Supplement / Extended A & B

    private static bool IsCyrillicLetter(char ch)
        => (ch >= 'Ѐ' && ch <= 'ӿ')  // Cyrillic
        || (ch >= 'Ԁ' && ch <= 'ԯ'); // Cyrillic Supplement
}

internal enum WordScript
{
    Unknown,
    Latin,
    Cyrillic,
}
