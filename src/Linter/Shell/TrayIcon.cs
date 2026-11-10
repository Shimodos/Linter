using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using Linter.Configuration;
using Linter.Diagnostics;
using Linter.Localization;
using Linter.Services;

namespace Linter.Shell;

internal sealed class TrayIcon : IDisposable
{
    private readonly AppSettings _settings;
    private readonly Action _onToggle;
    private readonly Func<string?> _lastFlaggedWord;
    private readonly NotifyIcon _notify;
    private readonly Icon _iconOn;
    private readonly Icon _iconOff;

    private readonly ToolStripMenuItem _settingsItem;
    private readonly ToolStripMenuItem _enabledItem;
    private readonly ToolStripMenuItem _autostartItem;
    private readonly ToolStripMenuItem _ignoreItem;
    private readonly ToolStripMenuItem _openLogItem;
    private readonly ToolStripMenuItem _resetLogItem;
    private readonly ToolStripMenuItem _exitItem;

    public TrayIcon(
        AppSettings settings,
        Action onToggle,
        Action onSettings,
        Action onExit,
        Func<string?> lastFlaggedWord,
        Action onIgnoreLastFlagged)
    {
        _settings = settings;
        _onToggle = onToggle;
        _lastFlaggedWord = lastFlaggedWord;

        _iconOn = AppIcons.Load(AppIcons.On, SystemInformation.SmallIconSize);
        _iconOff = AppIcons.Load(AppIcons.Off, SystemInformation.SmallIconSize);

        // Texts are assigned in Refresh, so a language change only needs a Refresh.
        _settingsItem = new ToolStripMenuItem(string.Empty, null, (_, _) => onSettings());
        _enabledItem = new ToolStripMenuItem(string.Empty, null, (_, _) => _onToggle());
        _autostartItem = new ToolStripMenuItem(string.Empty, null, (_, _) => ToggleAutostart());
        _ignoreItem = new ToolStripMenuItem(string.Empty, null, (_, _) => onIgnoreLastFlagged());
        _openLogItem = new ToolStripMenuItem(string.Empty, null, (_, _) => OpenLogFolder());
        _resetLogItem = new ToolStripMenuItem(string.Empty, null, (_, _) => Log.Reset());
        _exitItem = new ToolStripMenuItem(string.Empty, null, (_, _) => onExit());

        var menu = new ContextMenuStrip();
        menu.Items.Add(_settingsItem);
        menu.Items.Add(_ignoreItem);
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add(_enabledItem);
        menu.Items.Add(_autostartItem);
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add(_openLogItem);
        menu.Items.Add(_resetLogItem);
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add(_exitItem);
        // The word to ignore changes as the user types; look it up when the menu opens.
        menu.Opening += (_, _) => RefreshIgnoreItem();

        _notify = new NotifyIcon
        {
            Icon = _iconOn,
            Visible = true,
            Text = "Linter",
            ContextMenuStrip = menu,
        };
        _notify.DoubleClick += (_, _) => _onToggle();

        Refresh();
    }

    public void Refresh()
    {
        _settingsItem.Text = Localizer.T("tray.settings");
        _enabledItem.Text = Localizer.T("tray.enabled");
        _autostartItem.Text = Localizer.T("tray.autostart");
        _openLogItem.Text = Localizer.T("tray.openLog");
        _resetLogItem.Text = Localizer.T("tray.resetLog");
        _exitItem.Text = Localizer.T("tray.exit");
        RefreshIgnoreItem();

        _enabledItem.Checked = _settings.Enabled;
        _autostartItem.Checked = AutostartManager.IsEnabled();
        _notify.Icon = _settings.Enabled ? _iconOn : _iconOff;
        _notify.Text = Localizer.T(_settings.Enabled ? "tray.on" : "tray.off");
    }

    private void RefreshIgnoreItem()
    {
        var word = _lastFlaggedWord();
        _ignoreItem.Visible = word is not null;
        // "&" would be read as a menu accelerator marker.
        if (word is not null) _ignoreItem.Text = Localizer.T("tray.ignore", word.Replace("&", "&&"));
    }

    private void ToggleAutostart()
    {
        AutostartManager.Set(!AutostartManager.IsEnabled());
        _autostartItem.Checked = AutostartManager.IsEnabled();
    }

    public void Dispose()
    {
        _notify.Visible = false;
        _notify.Dispose();
        _iconOn.Dispose();
        _iconOff.Dispose();
    }

    private static void OpenLogFolder()
    {
        var dir = Path.GetDirectoryName(Log.Path);
        if (string.IsNullOrEmpty(dir)) return;
        Directory.CreateDirectory(dir);
        Process.Start(new ProcessStartInfo("explorer.exe", $"\"{dir}\"") { UseShellExecute = true });
    }
}
