using System;
using System.Collections.Generic;
using System.Globalization;

namespace Linter.Services;

/// <summary>
/// Decides which dictionaries get to judge a word, from the keyboard layout
/// it was typed under.
/// <para>
/// Russian and Ukrainian share an alphabet, so "any Cyrillic dictionary
/// accepts it" lets a Russian typo pass whenever it happens to be a
/// Ukrainian word ("ва"). The layout says which language was meant.
/// </para>
/// </summary>
internal static class SignalLanguages
{
    /// <summary>
    /// The language tag of a layout handle ("ru-RU"), or null if it has
    /// none. The low word of the handle is the Windows language id.
    /// </summary>
    public static string? OfLayout(IntPtr layout)
    {
        var languageId = (int)(layout.ToInt64() & 0xFFFF);
        if (languageId == 0) return null;
        try { return CultureInfo.GetCultureInfo(languageId).Name; }
        catch (CultureNotFoundException) { return null; }
        catch (ArgumentException) { return null; }
    }

    /// <summary>
    /// The signalling languages that match the layout's language ("en-GB"
    /// layout → "en-US" dictionary). An unknown layout cannot narrow the
    /// choice, so every signalling language applies.
    /// </summary>
    public static IReadOnlyList<string> For(IReadOnlyList<string> signalling, string? layoutTag)
    {
        if (string.IsNullOrEmpty(layoutTag)) return signalling;

        var language = Primary(layoutTag);
        var matching = new List<string>();
        foreach (var tag in signalling)
        {
            if (string.Equals(Primary(tag), language, StringComparison.OrdinalIgnoreCase)) matching.Add(tag);
        }
        return matching;
    }

    private static string Primary(string tag)
    {
        var dash = tag.IndexOf('-');
        return dash < 0 ? tag : tag[..dash];
    }
}
