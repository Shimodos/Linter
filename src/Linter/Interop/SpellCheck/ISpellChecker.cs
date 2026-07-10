using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;

namespace Linter.Interop.SpellCheck;

/// <summary>
/// We declare only the leading slots of the vtable that we actually call.
/// Methods beyond <see cref="Add"/> exist in the real interface but are
/// intentionally omitted — that is safe as long as we never invoke them.
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
}
