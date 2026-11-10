using System;
using System.Runtime.InteropServices;
using Linter.Interop;
using Linter.Text;

namespace Linter.Keyboard;

/// <summary>
/// Owns a system-wide low-level keyboard hook. Translates each WM_KEYDOWN /
/// WM_SYSKEYDOWN into a <see cref="KeyStroke"/> and raises
/// <see cref="KeyIntercepted"/>. If a handler sets <c>Handled = true</c>,
/// the key is swallowed before reaching the foreground application.
/// Key releases are never swallowed; they are only reported.
/// </summary>
internal sealed class KeyboardHook : IDisposable
{
    private readonly NativeMethods.LowLevelKeyboardProc _proc;
    private IntPtr _hookHandle;

    /// <summary>
    /// The key most recently pressed and not yet released. Auto-repeat only
    /// ever repeats the last key pressed, so one slot is enough to tell a
    /// repeat from a fresh press.
    /// </summary>
    private uint _heldKey;

    public event EventHandler<KeyInterceptedEventArgs>? KeyIntercepted;

    /// <summary>Raised with the virtual-key code when a physical key is released.</summary>
    public event Action<uint>? KeyReleased;

    public KeyboardHook()
    {
        _proc = HookCallback;
    }

    public void Start()
    {
        if (_hookHandle != IntPtr.Zero) return;
        var module = NativeMethods.GetModuleHandle(null);
        _hookHandle = NativeMethods.SetWindowsHookEx(NativeConstants.WH_KEYBOARD_LL, _proc, module, 0);
        if (_hookHandle == IntPtr.Zero)
        {
            throw new InvalidOperationException(
                $"SetWindowsHookEx failed (Win32 error {Marshal.GetLastWin32Error()})");
        }
    }

    public void Stop()
    {
        if (_hookHandle == IntPtr.Zero) return;
        NativeMethods.UnhookWindowsHookEx(_hookHandle);
        _hookHandle = IntPtr.Zero;
    }

    private IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
    {
        if (nCode < 0)
            return NativeMethods.CallNextHookEx(_hookHandle, nCode, wParam, lParam);

        var msg = (int)wParam;
        var isKeyDown = msg is NativeConstants.WM_KEYDOWN or NativeConstants.WM_SYSKEYDOWN;
        var isKeyUp = msg is NativeConstants.WM_KEYUP or NativeConstants.WM_SYSKEYUP;
        if (!isKeyDown && !isKeyUp)
            return NativeMethods.CallNextHookEx(_hookHandle, nCode, wParam, lParam);

        var data = Marshal.PtrToStructure<KBDLLHOOKSTRUCT>(lParam);

        // Ignore programmatically-injected keys to avoid feedback loops with
        // our own InputSimulator.
        if ((data.Flags & NativeConstants.LLKHF_INJECTED) != 0)
            return NativeMethods.CallNextHookEx(_hookHandle, nCode, wParam, lParam);

        if (isKeyUp)
        {
            if (data.VkCode == _heldKey) _heldKey = 0;
            KeyReleased?.Invoke(data.VkCode);
            return NativeMethods.CallNextHookEx(_hookHandle, nCode, wParam, lParam);
        }

        var isRepeat = data.VkCode == _heldKey;
        _heldKey = data.VkCode;

        var stroke = StrokeFromHook(data);
        var args = new KeyInterceptedEventArgs(stroke, isRepeat);
        KeyIntercepted?.Invoke(this, args);

        return args.Handled
            ? new IntPtr(1)
            : NativeMethods.CallNextHookEx(_hookHandle, nCode, wParam, lParam);
    }

    private static KeyStroke StrokeFromHook(KBDLLHOOKSTRUCT data)
    {
        var modifiers = ModifierState.Capture();
        var layout = ForegroundLayout.Get();
        var output = LayoutRenderer.Render(data.VkCode, data.ScanCode, modifiers, layout);
        return new KeyStroke(
            VirtualKey: data.VkCode,
            ScanCode:   data.ScanCode,
            Modifiers:  modifiers,
            Output:     output,
            Layout:     layout);
    }

    public void Dispose() => Stop();
}
