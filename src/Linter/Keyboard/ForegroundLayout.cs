using System;
using Linter.Interop;

namespace Linter.Keyboard;

/// <summary>
/// Returns the keyboard layout currently active in the foreground window.
/// A user can have a different layout per window, so we always query the
/// foreground thread rather than caching.
/// </summary>
internal static class ForegroundLayout
{
    public static IntPtr Get()
    {
        var hwnd = NativeMethods.GetForegroundWindow();
        if (hwnd == IntPtr.Zero) return NativeMethods.GetKeyboardLayout(0);
        var threadId = NativeMethods.GetWindowThreadProcessId(hwnd, out _);
        return NativeMethods.GetKeyboardLayout(threadId);
    }
}
