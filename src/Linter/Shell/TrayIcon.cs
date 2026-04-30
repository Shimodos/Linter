using System;
using System.Drawing;
using System.Windows.Forms;
using Linter.Configuration;

namespace Linter.Shell;

internal sealed class TrayIcon : IDisposable
{
    private readonly AppSettings _settings;
    private readonly Action _onToggle;
    private readonly Action _onExit;
    private readonly NotifyIcon _notify;
    private readonly ToolStripMenuItem _enabledItem;

    public TrayIcon(AppSettings settings, Action onToggle, Action onExit)
    {
        _settings = settings;
        _onToggle = onToggle;
        _onExit = onExit;

        _enabledItem = new ToolStripMenuItem("Enabled", null, (_, _) => _onToggle());

        var menu = new ContextMenuStrip();
        menu.Items.Add(_enabledItem);
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
        _notify.Text = _settings.Enabled ? "Linter — on" : "Linter — off";
    }

    public void Dispose()
    {
        _notify.Visible = false;
        _notify.Dispose();
    }
}
