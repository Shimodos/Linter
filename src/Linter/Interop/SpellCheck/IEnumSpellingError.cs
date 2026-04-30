using System.Runtime.InteropServices;

namespace Linter.Interop.SpellCheck;

[ComImport]
[Guid(SpellCheckClsid.IEnumSpellingError)]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
internal interface IEnumSpellingError
{
    /// <summary>
    /// Returns the next error or S_FALSE (1) if the sequence is exhausted.
    /// </summary>
    [PreserveSig] int Next(out ISpellingError? value);
}
