using System;
using Linter.Interop;

namespace Linter.Keyboard;

/// <summary>
/// Switches the active keyboard layout in the foreground window. Uses
/// <c>WM_INPUTLANGCHANGEREQUEST</c>, which is the path Windows itself takes
/// when the user presses Alt+Shift / Win+Space.
/// </summary>
internal sealed class LayoutSwitcher
{
    public IntPtr Current => ForegroundLayout.Get();

    public void SwitchTo(IntPtr targetLayout)
    {
        if (targetLayout == IntPtr.Zero) return;
        var hwnd = NativeMethods.GetForegroundWindow();
        if (hwnd == IntPtr.Zero) return;
        NativeMethods.PostMessage(
            hwnd,
            NativeConstants.WM_INPUTLANGCHANGEREQUEST,
            new IntPtr(NativeConstants.INPUTLANGCHANGE_FORWARD),
            targetLayout);
    }
}
