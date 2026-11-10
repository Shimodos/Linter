namespace Linter.Text;

/// <summary>
/// The handful of virtual-key codes the text layer needs to reason about
/// boundaries. Kept here (and not in Interop) so the text layer stays free
/// of Win32 dependencies — the integers are part of a stable Windows ABI.
/// </summary>
internal static class VirtualKeyCodes
{
    public const uint Back   = 0x08;
    public const uint Return = 0x0D;
    public const uint Escape = 0x1B;
    public const uint Space  = 0x20;
    public const uint Pause  = 0x13;
    public const uint LeftArrow  = 0x25;
    public const uint UpArrow    = 0x26;
    public const uint RightArrow = 0x27;
    public const uint DownArrow  = 0x28;
    public const uint Home = 0x24;
    public const uint End  = 0x23;
    public const uint Tab  = 0x09;
    public const uint PageUp     = 0x21;
    public const uint PageDown   = 0x22;
    public const uint Delete     = 0x2E;
    public const uint Insert     = 0x2D;
    public const uint F1         = 0x70; // F1..F12 are contiguous: F1 + n
    public const uint ScrollLock = 0x91;

    public const uint LeftShift   = 0xA0;
    public const uint RightShift  = 0xA1;
    public const uint LeftCtrl    = 0xA2;
    public const uint RightCtrl   = 0xA3;
    public const uint LeftAlt     = 0xA4;
    public const uint RightAlt    = 0xA5;
    public const uint LeftWin     = 0x5B;
    public const uint RightWin    = 0x5C;

    // Side-agnostic codes: what an application sees, and occasionally what
    // the hook reports for injected or remapped keys.
    public const uint Shift    = 0x10;
    public const uint Control  = 0x11;
    public const uint Alt      = 0x12;
    public const uint CapsLock = 0x14;

    /// <summary>Keys that only change the meaning of other keys and type nothing themselves.</summary>
    public static bool IsModifier(uint vk)
        => vk is Shift or Control or Alt or CapsLock
              or LeftShift or RightShift
              or LeftCtrl or RightCtrl
              or LeftAlt or RightAlt
              or LeftWin or RightWin;
}
