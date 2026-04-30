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
    [PreserveSig] int get_LanguageTag([MarshalAs(UnmanagedType.LPWStr)] out string value);

    [PreserveSig] int Check(
        [MarshalAs(UnmanagedType.LPWStr)] string text,
        out IEnumSpellingError errors);

    [PreserveSig] int Suggest(
        [MarshalAs(UnmanagedType.LPWStr)] string word,
        out IEnumString suggestions);

    [PreserveSig] int Add([MarshalAs(UnmanagedType.LPWStr)] string word);
}
