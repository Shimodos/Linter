using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using Linter.Configuration;
using Linter.Diagnostics;
using Linter.Services;

namespace Linter.Shell;

internal sealed class TrayIcon : IDisposable
{
    private readonly AppSettings _settings;
    private readonly Action _onToggle;
    private readonly Action _onExit;
    private readonly NotifyIcon _notify;
    private readonly ToolStripMenuItem _enabledItem;
    private readonly ToolStripMenuItem _autostartItem;

    public TrayIcon(AppSettings settings, Action onToggle, Action onExit)
    {
        _settings = settings;
        _onToggle = onToggle;
        _onExit = onExit;

        _enabledItem = new ToolStripMenuItem("Enabled", null, (_, _) => _onToggle());
        _autostartItem = new ToolStripMenuItem("Start with Windows", null, (_, _) => ToggleAutostart());

        var menu = new ContextMenuStrip();
        menu.Items.Add(_enabledItem);
        menu.Items.Add(_autostartItem);
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add(new ToolStripMenuItem("Open log folder", null, (_, _) => OpenLogFolder()));
        menu.Items.Add(new ToolStripMenuItem("Reset log", null, (_, _) => Log.Reset()));
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add(new ToolStripMenuItem("Exit", null, (_, _) => _onExit()));

        _notify = new NotifyIcon
        {
            Icon = SystemIcons.Application,
            Visible = true,
            Text = "Linter",
            ContextMenuStrip = menu,
        };
        _notify.DoubleClick += (_, _) => _onToggle();

        Refresh();
    }

    public void Refresh()
    {
        _enabledItem.Checked = _settings.Enabled;
        _autostartItem.Checked = AutostartManager.IsEnabled();
        _notify.Text = _settings.Enabled ? "Linter — on" : "Linter — off";
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
    }

    private static void OpenLogFolder()
    {
        var dir = Path.GetDirectoryName(Log.Path);
        if (string.IsNullOrEmpty(dir)) return;
        Directory.CreateDirectory(dir);
        Process.Start(new ProcessStartInfo("explorer.exe", $"\"{dir}\"") { UseShellExecute = true });
    }
}
