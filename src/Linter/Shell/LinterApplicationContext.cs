using System;
using System.Windows.Forms;
using Linter.Configuration;
using Linter.Keyboard;
using Linter.Services;
using Linter.Text;

namespace Linter.Shell;

/// <summary>
/// Owns the lifetime of every long-lived component and wires the keyboard
/// hook to the converter and spell checker. One file == one wiring diagram.
/// </summary>
internal sealed class LinterApplicationContext : ApplicationContext
{
    private readonly AppSettings _settings;
    private readonly SettingsStore _settingsStore;

    private readonly KeyboardHook _hook;
    private readonly InputSimulator _input;
    private readonly LayoutSwitcher _layouts;

    private readonly WordBuffer _buffer;
    private readonly LayoutConverter _converter;
    private readonly SpellChecker _spell;
    private readonly SoundNotifier _sound;

    private readonly TrayIcon _tray;

    public LinterApplicationContext()
    {
        _settingsStore = new SettingsStore();
        _settings = _settingsStore.Load();

        _input = new InputSimulator();
        _layouts = new LayoutSwitcher();
        _spell = new SpellChecker(_settings.Languages);
        _sound = new SoundNotifier();

        _buffer = new WordBuffer();
        _converter = new LayoutConverter(_input, _layouts, _spell);

        _hook = new KeyboardHook();
        _hook.KeyIntercepted += OnKeyIntercepted;
        _hook.Start();

        _tray = new TrayIcon(_settings, OnToggleEnabled, OnExit);
    }

    private void OnKeyIntercepted(object? sender, KeyInterceptedEventArgs e)
    {
        if (!_settings.Enabled) return;

        if (_settings.Hotkeys.IsConvertHotkey(e.Stroke))
        {
            e.Handled = true;
            _converter.ConvertLastWord(_buffer.Snapshot());
            _buffer.Reset();
            return;
        }

        var transition = _buffer.Apply(e.Stroke);
        if (transition.WordCompleted && _settings.SpellCheckEnabled)
        {
            if (!_spell.IsCorrect(transition.CompletedWord))
            {
                _sound.PlayMisspelledSignal();
            }
        }
    }

    private void OnToggleEnabled()
    {
        _settings.Enabled = !_settings.Enabled;
        _settingsStore.Save(_settings);
        _tray.Refresh();
    }

    private void OnExit()
    {
        ExitThread();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _hook.KeyIntercepted -= OnKeyIntercepted;
            _hook.Dispose();
            _tray.Dispose();
            _spell.Dispose();
        }
        base.Dispose(disposing);
    }
}
