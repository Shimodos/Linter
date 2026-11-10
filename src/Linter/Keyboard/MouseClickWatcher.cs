using System;
using System.Threading;
using Linter.Interop;

namespace Linter.Keyboard;

/// <summary>
/// Notices mouse clicks and wheel turns anywhere on the desktop. A click can
/// move the caret or focus another field, after which the keystrokes we
/// buffered no longer describe what sits before the caret.
/// <para>
/// The low-level mouse hook is called for every mouse *move* too, and
/// Windows waits for the callback before moving the pointer. So the hook
/// lives on its own thread whose only job is to return instantly — it never
/// shares a message loop with the spell checker. It just raises a flag that
/// the keyboard path reads on the next keystroke.
/// </para>
/// </summary>
internal sealed class MouseClickWatcher : IDisposable
{
    private readonly NativeMethods.LowLevelKeyboardProc _proc; // same signature for every LL hook
    private readonly ManualResetEventSlim _started = new();
    private Thread? _thread;
    private uint _threadId;
    private IntPtr _hookHandle;
    private int _clicked;

    public MouseClickWatcher()
    {
        _proc = HookCallback;
    }

    public void Start()
    {
        if (_thread is not null) return;
        _thread = new Thread(Run) { IsBackground = true, Name = "Linter mouse hook" };
        _thread.Start();
        _started.Wait();
    }

    /// <summary>Returns whether a click happened since the last call, and clears the flag.</summary>
    public bool ConsumeClick() => Interlocked.Exchange(ref _clicked, 0) != 0;

    private void Run()
    {
        _threadId = NativeMethods.GetCurrentThreadId();
        _hookHandle = NativeMethods.SetWindowsHookEx(
            NativeConstants.WH_MOUSE_LL, _proc, NativeMethods.GetModuleHandle(null), 0);
        _started.Set();
        // No hook is not fatal: Linter still works, it just can't see clicks.
        if (_hookHandle == IntPtr.Zero) return;

        // A low-level hook is only called while its thread pumps messages.
        while (NativeMethods.GetMessage(out _, IntPtr.Zero, 0, 0) > 0) { }

        NativeMethods.UnhookWindowsHookEx(_hookHandle);
        _hookHandle = IntPtr.Zero;
    }

    private IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
    {
        if (nCode >= 0)
        {
            switch ((int)wParam)
            {
                case NativeConstants.WM_LBUTTONDOWN:
                case NativeConstants.WM_RBUTTONDOWN:
                case NativeConstants.WM_MBUTTONDOWN:
                case NativeConstants.WM_XBUTTONDOWN:
                    Volatile.Write(ref _clicked, 1);
                    break;
            }
        }
        return NativeMethods.CallNextHookEx(_hookHandle, nCode, wParam, lParam);
    }

    public void Dispose()
    {
        if (_thread is null) return;
        if (_threadId != 0) NativeMethods.PostThreadMessage(_threadId, NativeConstants.WM_QUIT, IntPtr.Zero, IntPtr.Zero);
        _thread.Join(500);
        _thread = null;
        _started.Dispose();
    }
}
