using System;
using Linter.Interop;

namespace Linter.Keyboard;

internal static class InstalledLayouts
{
    /// <summary>
    /// Returns every keyboard layout (HKL) currently installed for the user.
    /// </summary>
    public static IntPtr[] Enumerate()
    {
        var count = NativeMethods.GetKeyboardLayoutList(0, Array.Empty<IntPtr>());
        if (count <= 0) return Array.Empty<IntPtr>();
        var list = new IntPtr[count];
        NativeMethods.GetKeyboardLayoutList(count, list);
        return list;
    }
}
