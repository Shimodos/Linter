using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Linter.Diagnostics;
using Linter.Interop.SpellCheck;
using Linter.Text;

namespace Linter.Services;

/// <summary>
/// Wraps the COM Windows Spell Checking API. One <see cref="ISpellChecker"/>
/// instance is created per language so we can probe a word against every
/// language we care about.
/// </summary>
internal sealed class SpellChecker : IDisposable
{
    private readonly ISpellCheckerFactory _factory;
    private readonly Dictionary<string, ISpellChecker?> _byLanguage = new(StringComparer.OrdinalIgnoreCase);
    private HashSet<string> _ignored;

    public SpellChecker(IEnumerable<string> languageTags, IEnumerable<string>? ignoredWords = null)
    {
        _ignored = new HashSet<string>(ignoredWords ?? Array.Empty<string>(), StringComparer.OrdinalIgnoreCase);
        var type = Type.GetTypeFromCLSID(SpellCheckClsid.SpellCheckerFactory)
                   ?? throw new InvalidOperationException("SpellCheckerFactory CLSID not registered.");
        _factory = (ISpellCheckerFactory)Activator.CreateInstance(type)!;
        foreach (var tag in languageTags) Register(tag);
    }

    /// <summary>
    /// Applies a changed language / ignore list without restarting: drops
    /// the checkers that are no longer wanted and loads the new ones.
    /// </summary>
    public void Reconfigure(IEnumerable<string> languageTags, IEnumerable<string> ignoredWords)
    {
        _ignored = new HashSet<string>(ignoredWords, StringComparer.OrdinalIgnoreCase);

        var wanted = new HashSet<string>(languageTags, StringComparer.OrdinalIgnoreCase);
        foreach (var tag in new List<string>(_byLanguage.Keys))
        {
            if (wanted.Contains(tag)) continue;
            if (_byLanguage[tag] is { } checker) Marshal.FinalReleaseComObject(checker);
            _byLanguage.Remove(tag);
        }
        foreach (var tag in wanted)
        {
            if (!_byLanguage.ContainsKey(tag)) Register(tag);
        }
    }

    /// <summary>Stops signalling <paramref name="word"/> for the rest of the session.</summary>
    public void Ignore(string word) => _ignored.Add(word);

    /// <summary>
    /// The dictionary's best replacement for a misspelled word, or null if
    /// no dictionary of a matching script has one. Consults the same
    /// dictionaries, in the same order, as <see cref="IsCorrect(string, IEnumerable{string})"/>.
    /// </summary>
    public string? Suggest(string word, IEnumerable<string> restrictTo)
    {
        if (string.IsNullOrWhiteSpace(word)) return null;
        var script = WordScripts.Detect(word);
        foreach (var tag in restrictTo)
        {
            if (!_byLanguage.TryGetValue(tag, out var checker) || checker is null) continue;
            var langScript = WordScripts.OfLanguage(tag);
            if (script != WordScript.Unknown && langScript != WordScript.Unknown && script != langScript) continue;

            // A dictionary that accepts the word means there is nothing to fix.
            // Asked anyway, the API may still offer near-misses ("receive" → "received").
            if (CheckOne(tag, word)) return null;

            if (checker.Suggest(word, out var suggestions) != 0 || suggestions is null) continue;
            try
            {
                var first = FirstOf(suggestions);
                Log.Trace($"    suggest({tag}, \"{word}\") → {(first is null ? "nothing" : $"\"{first}\"")}");
                if (first is not null && !string.Equals(first, word, StringComparison.Ordinal)) return first;
            }
            finally
            {
                Marshal.FinalReleaseComObject(suggestions);
            }
        }
        return null;
    }

    /// <summary>Every language Windows currently has a spelling dictionary for.</summary>
    public IReadOnlyList<string> InstalledLanguages()
    {
        var result = new List<string>();
        if (_factory.get_SupportedLanguages(out var languages) != 0 || languages is null) return result;
        try
        {
            var one = new string[1];
            while (languages.Next(1, one, IntPtr.Zero) == 0 && one[0] is not null) result.Add(one[0]);
        }
        finally
        {
            Marshal.FinalReleaseComObject(languages);
        }
        return result;
    }

    private static string? FirstOf(System.Runtime.InteropServices.ComTypes.IEnumString strings)
    {
        var one = new string[1];
        return strings.Next(1, one, IntPtr.Zero) == 0 ? one[0] : null;
    }

    /// <summary>Whether Windows has a spelling dictionary installed for the tag.</summary>
    public bool IsLanguageSupported(string languageTag)
        => _factory.IsSupported(languageTag, out var supported) == 0 && supported;

    public bool IsCorrect(string word) => IsCorrect(word, _byLanguage.Keys);

    /// <summary>
    /// Checks <paramref name="word"/> only against the dictionaries listed
    /// in <paramref name="restrictTo"/>, further filtered by script match.
    /// If no listed dictionary matches the word's script, returns
    /// <c>true</c> (we have no opinion — better than spurious beeps).
    /// </summary>
    public bool IsCorrect(string word, IEnumerable<string> restrictTo)
    {
        if (string.IsNullOrWhiteSpace(word)) return true;
        if (_ignored.Contains(word)) return true;
        var script = WordScripts.Detect(word);
        var matching = new List<string>();
        foreach (var tag in restrictTo)
        {
            if (!_byLanguage.ContainsKey(tag)) continue;
            var langScript = WordScripts.OfLanguage(tag);
            if (script == WordScript.Unknown
                || langScript == WordScript.Unknown
                || script == langScript)
            {
                matching.Add(tag);
            }
        }
        if (matching.Count == 0) return true;
        foreach (var tag in matching)
        {
            if (CheckOne(tag, word)) return true;
        }
        return false;
    }

    public IReadOnlyCollection<string> SupportedLanguages => _byLanguage.Keys;

    private void Register(string languageTag)
    {
        if (_factory.IsSupported(languageTag, out var supported) != 0 || !supported)
        {
            _byLanguage[languageTag] = null;
            Log.Trace($"spell: {languageTag} NOT supported (dictionary not installed?)");
            return;
        }
        if (_factory.CreateSpellChecker(languageTag, out var checker) == 0)
        {
            _byLanguage[languageTag] = checker;
            Log.Trace($"spell: {languageTag} ready");
        }
        else
        {
            Log.Trace($"spell: {languageTag} CreateSpellChecker failed");
        }
    }

    private bool CheckOne(string languageTag, string word)
    {
        if (!_byLanguage.TryGetValue(languageTag, out var checker) || checker is null)
        {
            Log.Trace($"    spell({languageTag}, \"{word}\"): no dict registered");
            return false;
        }
        var hr = checker.Check(word, out var errors);
        if (hr != 0)
        {
            Log.Trace($"    spell({languageTag}, \"{word}\"): Check hr=0x{hr:X8} → assume correct");
            return true;
        }
        if (errors is null)
        {
            Log.Trace($"    spell({languageTag}, \"{word}\"): Check returned null enumerator → assume correct");
            return true;
        }
        try
        {
            var nextHr = errors.Next(out var first);
            var noErrors = nextHr != 0 || first is null;
            Log.Trace($"    spell({languageTag}, \"{word}\"): nextHr=0x{nextHr:X8} first={(first is null ? "null" : "ERROR")} → {(noErrors ? "correct" : "MISSPELLED")}");
            return noErrors;
        }
        finally
        {
            Marshal.FinalReleaseComObject(errors);
        }
    }

    public void Dispose()
    {
        foreach (var checker in _byLanguage.Values)
        {
            if (checker is not null) Marshal.FinalReleaseComObject(checker);
        }
        Marshal.FinalReleaseComObject(_factory);
    }
}
