using Linter.Text;

namespace Linter.Configuration;

internal sealed class HotkeyConfig
{
    public Hotkey Convert { get; set; } = new(VirtualKeyCodes.Pause, KeyModifiers.None);

    public bool IsConvertHotkey(KeyStroke stroke) => Convert.Matches(stroke);
}
