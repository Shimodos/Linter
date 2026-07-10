using System;
using System.Diagnostics;
using Linter.Interop;

namespace Linter.Keyboard;

/// <summary>
/// Resolves the process name of the current foreground window. Called on
/// the LL keyboard-hook thread for every keystroke, so the (relatively
/// expensive) process lookup is cached by window handle — it only re-runs
/// when the foreground window actually changes.
/// </summary>
internal static class ForegroundApp
{
    private static IntPtr _cachedHwnd = IntPtr.Zero;
    private static string _cachedName = string.Empty;

    /// <summary>Process name without the <c>.exe</c> suffix, or "" if unknown.</summary>
    public static string CurrentProcessName()
    {
        var hwnd = NativeMethods.GetForegroundWindow();
        if (hwnd == _cachedHwnd) return _cachedName;

        _cachedHwnd = hwnd;
        _cachedName = Resolve(hwnd);
        return _cachedName;
    }

    private static string Resolve(IntPtr hwnd)
    {
        if (hwnd == IntPtr.Zero) return string.Empty;
        try
        {
            NativeMethods.GetWindowThreadProcessId(hwnd, out var pid);
            if (pid == 0) return string.Empty;
            using var process = Process.GetProcessById((int)pid);
            return process.ProcessName;
        }
        catch
        {
            // Process may have exited between the two calls, or be protected.
            return string.Empty;
        }
    }
}
