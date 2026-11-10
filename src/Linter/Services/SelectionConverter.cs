using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Linter.Diagnostics;
using Linter.Interop;
using Linter.Keyboard;
using Linter.Text;

namespace Linter.Services;

/// <summary>
/// Converts the *selected* text to the right layout. Unlike the phrase
/// converter it has no record of the keystrokes, so it works backwards: for
/// each installed layout it asks "which keys would have typed this text?"
/// and replays those keys under every other layout, keeping the result the
/// spell checker likes best.
/// <para>
/// The text travels through the clipboard (Ctrl+C to read, Ctrl+V to write),
/// which is the only way to read a selection that works in every app. The
/// previous clipboard text is put back afterwards.
/// </para>
/// </summary>
internal sealed class SelectionConverter
{
    private const int ShiftBit = 1, CtrlBit = 2, AltBit = 4;

    private readonly InputSimulator _input;
    private readonly LayoutSwitcher _switcher;
    private readonly SpellChecker _spell;
    private bool _busy;

    public SelectionConverter(InputSimulator input, LayoutSwitcher switcher, SpellChecker spell)
    {
        _input = input;
        _switcher = switcher;
        _spell = spell;
    }

    /// <summary>Must be called on the UI thread; continues there after each wait.</summary>
    public async Task ConvertAsync()
    {
        if (_busy) return;
        _busy = true;
        try
        {
            // The hotkey itself may involve Shift or Alt. Ctrl+C sent while
            // one is still held would arrive as Ctrl+Shift+C.
            if (!await WaitForModifiersReleasedAsync()) return;

            var saved = ReadClipboard();
            ClearClipboard();
            _input.CtrlChord('C');

            var selected = await WaitForClipboardAsync();
            if (string.IsNullOrEmpty(selected))
            {
                Log.Trace("convert selection | nothing selected");
                RestoreClipboard(saved);
                return;
            }

            var best = Convert(selected);
            if (best is null)
            {
                Log.Trace("convert selection | no layout reads better, leaving as-is");
                RestoreClipboard(saved);
                return;
            }

            WriteClipboard(best.Value.Text);
            _input.CtrlChord('V');
            _switcher.SwitchTo(best.Value.Layout);

            // Give the app time to read the clipboard before we take it back.
            await Task.Delay(300);
            RestoreClipboard(saved);
        }
        finally
        {
            _busy = false;
        }
    }

    /// <summary>
    /// Returns the best re-reading of <paramref name="text"/>, or null if
    /// none has more real words than the text as it stands.
    /// </summary>
    internal LayoutCandidate? Convert(string text)
    {
        var layouts = InstalledLayouts.Enumerate();
        LayoutCandidate? best = null;
        var bestScore = CountCorrectWords(text);
        Log.Trace($"convert selection | \"{text}\" correct={bestScore}");

        foreach (var source in layouts)
        {
            var keys = KeysFor(text, source);
            if (keys is null) continue; // this layout could not have typed the text

            foreach (var target in layouts)
            {
                if (target == source) continue;
                var candidate = Render(text, keys, source, target);
                if (candidate == text) continue;
                var score = CountCorrectWords(candidate);
                Log.Trace($"  0x{source.ToInt64():X8} → 0x{target.ToInt64():X8} \"{candidate}\" correct={score}");
                if (score > bestScore)
                {
                    bestScore = score;
                    best = new LayoutCandidate(target, candidate);
                }
            }
        }
        return best;
    }

    /// <summary>
    /// The key (VkKeyScanEx result) for every character, or -1 where the
    /// layout has none. Null if the layout cannot type one of the *letters* —
    /// then it is not where this text came from.
    /// </summary>
    private static short[]? KeysFor(string text, IntPtr layout)
    {
        var keys = new short[text.Length];
        for (var i = 0; i < text.Length; i++)
        {
            keys[i] = NativeMethods.VkKeyScanEx(text[i], layout);
            if (keys[i] == -1 && char.IsLetter(text[i])) return null;
        }
        return keys;
    }

    private static string Render(string text, short[] keys, IntPtr source, IntPtr target)
    {
        var result = new StringBuilder(text.Length);
        for (var i = 0; i < text.Length; i++)
        {
            // Line breaks, tabs and anything the layout has no key for stay put.
            if (keys[i] == -1 || char.IsControl(text[i]) || char.IsWhiteSpace(text[i]))
            {
                result.Append(text[i]);
                continue;
            }

            var vk = (uint)(keys[i] & 0xFF);
            var state = keys[i] >> 8;
            var modifiers = KeyModifiers.None;
            if ((state & ShiftBit) != 0) modifiers |= KeyModifiers.Shift;
            if ((state & CtrlBit) != 0) modifiers |= KeyModifiers.Control;
            if ((state & AltBit) != 0) modifiers |= KeyModifiers.Alt;

            var scan = NativeMethods.MapVirtualKeyEx(vk, NativeConstants.MAPVK_VK_TO_VSC, source);
            var rendered = LayoutRenderer.Render(vk, scan, modifiers, target);
            result.Append(rendered.Length > 0 ? rendered : text[i].ToString());
        }
        return result.ToString();
    }

    private int CountCorrectWords(string text)
    {
        var correct = 0;
        foreach (var word in SplitWords(text))
        {
            if (_spell.IsCorrect(word)) correct++;
        }
        return correct;
    }

    /// <summary>Runs of letters (with inner apostrophes and hyphens), punctuation stripped.</summary>
    internal static IEnumerable<string> SplitWords(string text)
    {
        var start = -1;
        for (var i = 0; i <= text.Length; i++)
        {
            var inWord = i < text.Length && (char.IsLetter(text[i]) || (start >= 0 && text[i] is '\'' or '-'));
            if (inWord)
            {
                if (start < 0) start = i;
            }
            else if (start >= 0)
            {
                var word = text[start..i].TrimEnd('\'', '-');
                if (word.Length > 0) yield return word;
                start = -1;
            }
        }
    }

    // ---------------------------------------------------------------- waiting

    private static async Task<bool> WaitForModifiersReleasedAsync()
    {
        for (var i = 0; i < 40; i++) // up to a second
        {
            if (!IsDown(NativeConstants.VK_SHIFT) && !IsDown(NativeConstants.VK_MENU)
                && !IsDown(NativeConstants.VK_CONTROL)
                && !IsDown(NativeConstants.VK_LWIN) && !IsDown(NativeConstants.VK_RWIN))
            {
                return true;
            }
            await Task.Delay(25);
        }
        return false;
    }

    private static bool IsDown(int vk) => NativeMethods.GetAsyncKeyState(vk) < 0;

    private static async Task<string?> WaitForClipboardAsync()
    {
        for (var i = 0; i < 12; i++) // apps answer Ctrl+C in a few milliseconds; allow ~350
        {
            await Task.Delay(30);
            var text = ReadClipboard();
            if (!string.IsNullOrEmpty(text)) return text;
        }
        return null;
    }

    // ---------------------------------------------------------------- clipboard
    // Another process can hold the clipboard open at any moment; every call
    // may throw, and none of those failures is worth more than giving up.

    private static string? ReadClipboard()
    {
        try { return Clipboard.ContainsText() ? Clipboard.GetText() : null; }
        catch (Exception) { return null; }
    }

    private static void WriteClipboard(string text)
    {
        try { Clipboard.SetText(text); }
        catch (Exception) { }
    }

    private static void ClearClipboard()
    {
        try { Clipboard.Clear(); }
        catch (Exception) { }
    }

    private static void RestoreClipboard(string? saved)
    {
        if (string.IsNullOrEmpty(saved)) ClearClipboard();
        else WriteClipboard(saved);
    }
}
