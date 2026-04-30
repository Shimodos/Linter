using System;
using System.Text;
using Linter.Interop;
using Linter.Text;

namespace Linter.Keyboard;

/// <summary>
/// Wraps <c>ToUnicodeEx</c> so we can answer "what character would this
/// virtual key produce under layout X?" — both for live capture and for
/// generating candidate words under every installed layout.
/// </summary>
internal static class LayoutRenderer
{
    // Bit 2 (since Win10 1607) tells ToUnicodeEx not to mutate the kernel
    // dead-key state. Without it, querying for "what would this key produce"
    // eats real dead-key combos that the user is in the middle of typing.
    private const uint DoNotChangeKernelState = 0x04;

    public static string Render(uint virtualKey, uint scanCode, KeyModifiers modifiers, IntPtr layout)
    {
        var keyState = ModifierState.BuildKeyboardState(modifiers);
        var buffer = new StringBuilder(8);
        var result = NativeMethods.ToUnicodeEx(
            virtualKey, scanCode, keyState, buffer, buffer.Capacity,
            DoNotChangeKernelState, layout);
        return result switch
        {
            > 0 => buffer.ToString(0, result),
            _   => string.Empty,
        };
    }
}
