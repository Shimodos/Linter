using System;

namespace Linter.Interop.SpellCheck;

/// <summary>
/// CLSIDs / IIDs for the Windows Spell Checking API. Lives in its own file
/// so that interface declarations stay free of magic strings.
/// </summary>
internal static class SpellCheckClsid
{
    public static readonly Guid SpellCheckerFactory = new("7AB36653-1796-484B-BDFA-E74F1DB7C1DC");
    public const string ISpellCheckerFactory = "8E018A9D-2415-4677-BF08-794EA61F94BB";
    public const string ISpellChecker        = "B6FD0B71-E2BC-4653-8D05-F197E412770B";
    public const string IEnumSpellingError   = "803E3BD4-2828-4410-8290-418D1D73C762";
    public const string ISpellingError       = "B7C82D61-FBE8-4B47-9B27-6C0D2E0DE0A3";
}
