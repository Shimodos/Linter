namespace Linter.Interop;

/// <summary>
/// Plain Win32 numeric constants. Grouped by API surface so each
/// section can be cross-referenced against MSDN at a glance.
/// </summary>
internal static class NativeConstants
{
    // -------- SetWindowsHookEx --------
    public const int WH_KEYBOARD_LL = 13;

    public const int WM_KEYDOWN    = 0x0100;
    public const int WM_KEYUP      = 0x0101;
    public const int WM_SYSKEYDOWN = 0x0104;
    public const int WM_SYSKEYUP   = 0x0105;

    // KBDLLHOOKSTRUCT.flags
    public const uint LLKHF_EXTENDED = 0x01;
    public const uint LLKHF_INJECTED = 0x10;
    public const uint LLKHF_ALTDOWN  = 0x20;
    public const uint LLKHF_UP       = 0x80;

    // -------- SendInput --------
    public const uint INPUT_MOUSE    = 0;
    public const uint INPUT_KEYBOARD = 1;
    public const uint INPUT_HARDWARE = 2;

    public const uint KEYEVENTF_EXTENDEDKEY = 0x0001;
    public const uint KEYEVENTF_KEYUP       = 0x0002;
    public const uint KEYEVENTF_UNICODE     = 0x0004;
    public const uint KEYEVENTF_SCANCODE    = 0x0008;

    // -------- Virtual keys we care about --------
    public const int VK_BACK    = 0x08;
    public const int VK_RETURN  = 0x0D;
    public const int VK_SHIFT   = 0x10;
    public const int VK_CONTROL = 0x11;
    public const int VK_MENU    = 0x12;        // Alt
    public const int VK_PAUSE   = 0x13;
    public const int VK_CAPITAL = 0x14;        // CapsLock
    public const int VK_ESCAPE  = 0x1B;
    public const int VK_SPACE   = 0x20;
    public const int VK_LWIN    = 0x5B;
    public const int VK_RWIN    = 0x5C;
    public const int VK_LSHIFT  = 0xA0;
    public const int VK_RSHIFT  = 0xA1;
    public const int VK_LCONTROL = 0xA2;
    public const int VK_RCONTROL = 0xA3;
    public const int VK_LMENU    = 0xA4;
    public const int VK_RMENU    = 0xA5;

    // -------- GetKeyState --------
    public const int KEY_PRESSED = 0x8000;
    public const int KEY_TOGGLED = 0x0001;

    // -------- ActivateKeyboardLayout / PostMessage --------
    public const uint WM_INPUTLANGCHANGEREQUEST = 0x0050;
    public const uint INPUTLANGCHANGE_FORWARD   = 0x0002;

    // -------- PlaySound --------
    public const uint SND_ASYNC    = 0x0001;
    public const uint SND_FILENAME = 0x00020000;
    public const uint SND_ALIAS    = 0x00010000;
}
