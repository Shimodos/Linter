using System.Runtime.InteropServices;

namespace Linter.Interop.SpellCheck;

[ComImport]
[Guid(SpellCheckClsid.ISpellingError)]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
internal interface ISpellingError
{
    [PreserveSig] int get_StartIndex(out uint value);
    [PreserveSig] int get_Length(out uint value);
    [PreserveSig] int get_CorrectiveAction(out CorrectiveAction value);
    [PreserveSig] int get_Replacement([MarshalAs(UnmanagedType.LPWStr)] out string value);
}
