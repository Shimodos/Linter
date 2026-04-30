using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Linter.Interop.SpellCheck;

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

    public SpellChecker(IEnumerable<string> languageTags)
    {
        var type = Type.GetTypeFromCLSID(SpellCheckClsid.SpellCheckerFactory)
                   ?? throw new InvalidOperationException("SpellCheckerFactory CLSID not registered.");
        _factory = (ISpellCheckerFactory)Activator.CreateInstance(type)!;
        foreach (var tag in languageTags) Register(tag);
    }

    public bool IsCorrect(string word) => IsCorrectIn(word, _byLanguage.Keys);

    public bool IsCorrectIn(string word, IEnumerable<string> languageTags)
    {
        if (string.IsNullOrWhiteSpace(word)) return true;
        foreach (var tag in languageTags)
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
            return;
        }
        if (_factory.CreateSpellChecker(languageTag, out var checker) == 0)
        {
            _byLanguage[languageTag] = checker;
        }
    }

    private bool CheckOne(string languageTag, string word)
    {
        if (!_byLanguage.TryGetValue(languageTag, out var checker) || checker is null)
            return false;
        if (checker.Check(word, out var errors) != 0 || errors is null)
            return true;
        try
        {
            return errors.Next(out var first) != 0 || first is null;
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
