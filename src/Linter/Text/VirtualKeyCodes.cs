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
}
