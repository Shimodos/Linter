using Linter.Interop;
using Linter.Text;

namespace Linter.Keyboard;

/// <summary>
/// Snapshots the state of modifier keys at the moment of a hook callback.
/// Pulled out of <see cref="KeyboardHook"/> to keep that file small.
/// </summary>
internal static class ModifierState
{
    public static KeyModifiers Capture()
    {
        var mods = KeyModifiers.None;
        if (IsDown(NativeConstants.VK_SHIFT))   mods |= KeyModifiers.Shift;
        if (IsDown(NativeConstants.VK_CONTROL)) mods |= KeyModifiers.Control;
        if (IsDown(NativeConstants.VK_MENU))    mods |= KeyModifiers.Alt;
        if (IsDown(NativeConstants.VK_LWIN) ||
            IsDown(NativeConstants.VK_RWIN))    mods |= KeyModifiers.Win;
        if (IsToggled(NativeConstants.VK_CAPITAL)) mods |= KeyModifiers.CapsLock;
        return mods;
    }

    public static byte[] BuildKeyboardState(KeyModifiers mods)
    {
        var state = new byte[256];
        if (mods.HasFlag(KeyModifiers.Shift))    state[NativeConstants.VK_SHIFT]   = 0x80;
        if (mods.HasFlag(KeyModifiers.Control))  state[NativeConstants.VK_CONTROL] = 0x80;
        if (mods.HasFlag(KeyModifiers.Alt))      state[NativeConstants.VK_MENU]    = 0x80;
        if (mods.HasFlag(KeyModifiers.CapsLock)) state[NativeConstants.VK_CAPITAL] = 0x01;
        return state;
    }

    private static bool IsDown(int vk)
        => (NativeMethods.GetKeyState(vk) & NativeConstants.KEY_PRESSED) != 0;

    private static bool IsToggled(int vk)
        => (NativeMethods.GetKeyState(vk) & NativeConstants.KEY_TOGGLED) != 0;
}
