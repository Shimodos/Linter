using System;
using Linter.Interop;

namespace Linter.Keyboard;

/// <summary>
/// Thin wrapper over <c>SendInput</c>. Two surfaces:
/// <list type="bullet">
///   <item><see cref="Backspace"/> — virtual-key path, used to delete the
///   word that is about to be replaced.</item>
///   <item><see cref="TypeUnicode"/> — Unicode path, lets us emit any char
///   regardless of which keyboard layout the foreground window thinks is
///   active.</item>
/// </list>
/// </summary>
internal sealed class InputSimulator
{
    public void Backspace(int count)
    {
        if (count <= 0) return;
        var inputs = new INPUT[count * 2];
        for (var i = 0; i < count; i++)
        {
            inputs[i * 2]     = KeyDown(NativeConstants.VK_BACK);
            inputs[i * 2 + 1] = KeyUp  (NativeConstants.VK_BACK);
        }
        Send(inputs);
    }

    public void TypeUnicode(string text)
    {
        if (string.IsNullOrEmpty(text)) return;
        var inputs = new INPUT[text.Length * 2];
        for (var i = 0; i < text.Length; i++)
        {
            inputs[i * 2]     = UnicodeDown(text[i]);
            inputs[i * 2 + 1] = UnicodeUp  (text[i]);
        }
        Send(inputs);
    }

    /// <summary>Presses <paramref name="key"/> while holding Ctrl — Ctrl+C, Ctrl+V.</summary>
    public void CtrlChord(char key)
    {
        Send(new[]
        {
            KeyDown(NativeConstants.VK_CONTROL),
            KeyDown(key),
            KeyUp(key),
            KeyUp(NativeConstants.VK_CONTROL),
        });
    }

    private static void Send(INPUT[] inputs)
    {
        if (inputs.Length == 0) return;
        NativeMethods.SendInput((uint)inputs.Length, inputs, INPUT.Size);
    }

    private static INPUT KeyDown(int vk) => new()
    {
        Type = NativeConstants.INPUT_KEYBOARD,
        U = new InputUnion { Keyboard = new KEYBDINPUT { Vk = (ushort)vk } },
    };

    private static INPUT KeyUp(int vk) => new()
    {
        Type = NativeConstants.INPUT_KEYBOARD,
        U = new InputUnion
        {
            Keyboard = new KEYBDINPUT { Vk = (ushort)vk, Flags = NativeConstants.KEYEVENTF_KEYUP },
        },
    };

    private static INPUT UnicodeDown(char ch) => new()
    {
        Type = NativeConstants.INPUT_KEYBOARD,
        U = new InputUnion
        {
            Keyboard = new KEYBDINPUT
            {
                Scan = ch,
                Flags = NativeConstants.KEYEVENTF_UNICODE,
            },
        },
    };

    private static INPUT UnicodeUp(char ch) => new()
    {
        Type = NativeConstants.INPUT_KEYBOARD,
        U = new InputUnion
        {
            Keyboard = new KEYBDINPUT
            {
                Scan = ch,
                Flags = NativeConstants.KEYEVENTF_UNICODE | NativeConstants.KEYEVENTF_KEYUP,
            },
        },
    };
}
