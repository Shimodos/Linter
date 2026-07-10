using System;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;

namespace Linter.Interop.SpellCheck;

/// <summary>
/// Vtable order is mandatory — every slot we want to call must be
/// declared in position. The interface has 14 methods (slots 0–13);
/// note that there is no <c>SetOptionValue</c> on <c>ISpellChecker</c>.
/// We only invoke <see cref="Check"/>, <see cref="Suggest"/>,
/// <see cref="Add"/>, and <see cref="ComprehensiveCheck"/>; the rest
/// are placeholders to align the vtable.
/// </summary>
[ComImport]
[Guid(SpellCheckClsid.ISpellChecker)]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
internal interface ISpellChecker
{
    // 0 — get_LanguageTag
    [PreserveSig] int get_LanguageTag([MarshalAs(UnmanagedType.LPWStr)] out string value);

    // 1 — Check
    [PreserveSig] int Check(
        [MarshalAs(UnmanagedType.LPWStr)] string text,
        [MarshalAs(UnmanagedType.Interface)] out IEnumSpellingError errors);

    // 2 — Suggest
    [PreserveSig] int Suggest(
        [MarshalAs(UnmanagedType.LPWStr)] string word,
        [MarshalAs(UnmanagedType.Interface)] out IEnumString suggestions);

    // 3 — Add
    [PreserveSig] int Add([MarshalAs(UnmanagedType.LPWStr)] string word);

    // 4 — Ignore
    [PreserveSig] int Ignore([MarshalAs(UnmanagedType.LPWStr)] string word);

    // 5 — AutoCorrect
    [PreserveSig] int AutoCorrect(
        [MarshalAs(UnmanagedType.LPWStr)] string from,
        [MarshalAs(UnmanagedType.LPWStr)] string to);

    // 6 — GetOptionValue
    [PreserveSig] int GetOptionValue(
        [MarshalAs(UnmanagedType.LPWStr)] string optionId,
        out byte value);

    // 7 — get_OptionIds (placeholder)
    [PreserveSig] int _get_OptionIds(out IntPtr value);

    // 8 — get_Id (placeholder)
    [PreserveSig] int _get_Id(out IntPtr value);

    // 9 — get_LocalizedName (placeholder)
    [PreserveSig] int _get_LocalizedName(out IntPtr value);

    // 10 — add_SpellCheckerChanged (placeholder)
    [PreserveSig] int _add_SpellCheckerChanged(IntPtr handler, out uint cookie);

    // 11 — remove_SpellCheckerChanged (placeholder)
    [PreserveSig] int _remove_SpellCheckerChanged(uint cookie);

    // 12 — GetOptionDescription (placeholder)
    [PreserveSig] int _GetOptionDescription(IntPtr optionId, out IntPtr value);

    // 13 — ComprehensiveCheck — the slot we want
    [PreserveSig] int ComprehensiveCheck(
        [MarshalAs(UnmanagedType.LPWStr)] string text,
        [MarshalAs(UnmanagedType.Interface)] out IEnumSpellingError errors);
}
