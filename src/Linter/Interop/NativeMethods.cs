using System;
using System.Runtime.InteropServices;
using System.Text;

namespace Linter.Interop;

/// <summary>
/// Raw P/Invoke surface. Anything allocating native handles or invoking the
/// Win32 input/layout APIs lives here so that higher layers can be coded
/// against safe wrappers.
/// </summary>
internal static class NativeMethods
{
    public delegate IntPtr LowLevelKeyboardProc(int nCode, IntPtr wParam, IntPtr lParam);

    // ---- Hooks ----
    [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    public static extern IntPtr SetWindowsHookEx(int idHook, LowLevelKeyboardProc lpfn, IntPtr hMod, uint dwThreadId);

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool UnhookWindowsHookEx(IntPtr hhk);

    [DllImport("user32.dll")]
    public static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

    [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    public static extern IntPtr GetModuleHandle(string? lpModuleName);

    // ---- Message loop for the mouse-hook thread ----
    [DllImport("user32.dll")]
    public static extern int GetMessage(out MSG lpMsg, IntPtr hWnd, uint wMsgFilterMin, uint wMsgFilterMax);

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool PostThreadMessage(uint idThread, uint msg, IntPtr wParam, IntPtr lParam);

    [DllImport("kernel32.dll")]
    public static extern uint GetCurrentThreadId();

    // ---- Input ----
    [DllImport("user32.dll", SetLastError = true)]
    public static extern uint SendInput(uint nInputs, [In] INPUT[] pInputs, int cbSize);

    [DllImport("user32.dll")]
    public static extern short GetKeyState(int nVirtKey);

    /// <summary>Negative (top bit set) while the key is physically down right now.</summary>
    [DllImport("user32.dll")]
    public static extern short GetAsyncKeyState(int vKey);

    /// <summary>
    /// Which key types <paramref name="ch"/> under a layout: low byte is the
    /// virtual key, high byte the shift state (1 Shift, 2 Ctrl, 4 Alt).
    /// Returns -1 when the layout cannot type the character.
    /// </summary>
    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    public static extern short VkKeyScanEx(char ch, IntPtr dwhkl);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    public static extern uint MapVirtualKeyEx(uint uCode, uint uMapType, IntPtr dwhkl);

    // CharSet.Unicode is REQUIRED here. Without it, StringBuilder is
    // marshaled as ANSI: ToUnicodeEx writes UTF-16, but the marshaling
    // layer reads each byte as a char, so 'в' (U+0432) comes back as
    // '2' (its low byte 0x32) and 'ы' (U+044B) as 'K' — turning "вы"
    // into "2K".
    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    public static extern int ToUnicodeEx(
        uint wVirtKey,
        uint wScanCode,
        byte[] lpKeyState,
        [Out] StringBuilder pwszBuff,
        int cchBuff,
        uint wFlags,
        IntPtr dwhkl);

    // ---- Keyboard layouts ----
    [DllImport("user32.dll")]
    public static extern IntPtr GetKeyboardLayout(uint idThread);

    [DllImport("user32.dll")]
    public static extern int GetKeyboardLayoutList(int nBuff, [Out] IntPtr[] lpList);

    [DllImport("user32.dll")]
    public static extern IntPtr ActivateKeyboardLayout(IntPtr hkl, uint flags);

    [DllImport("user32.dll", SetLastError = true)]
    public static extern IntPtr GetForegroundWindow();

    [DllImport("user32.dll", SetLastError = true)]
    public static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

    [DllImport("user32.dll", CharSet = CharSet.Auto)]
    public static extern IntPtr PostMessage(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);

    // ---- Sound ----
    [DllImport("winmm.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool PlaySound(string? pszSound, IntPtr hmod, uint fdwSound);

    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool Beep(uint dwFreq, uint dwDuration);

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool MessageBeep(uint uType);
}
