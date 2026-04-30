using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;

namespace Linter.Interop.SpellCheck;

[ComImport]
[Guid(SpellCheckClsid.ISpellCheckerFactory)]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
internal interface ISpellCheckerFactory
{
    [PreserveSig] int get_SupportedLanguages(out IEnumString value);

    [PreserveSig] int IsSupported(
        [MarshalAs(UnmanagedType.LPWStr)] string languageTag,
        [MarshalAs(UnmanagedType.Bool)] out bool value);

    [PreserveSig] int CreateSpellChecker(
        [MarshalAs(UnmanagedType.LPWStr)] string languageTag,
        out ISpellChecker value);
}
