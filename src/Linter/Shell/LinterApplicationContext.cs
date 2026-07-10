using System;
using System.Threading;
using System.Windows.Forms;
using Linter.Configuration;
using Linter.Diagnostics;
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

    /// <summary>
    /// Captured lazily on first hook callback — at construction time the
    /// WinForms sync context might not be installed yet. Used to defer
    /// COM-heavy work out of the LL hook callback (Windows forbids COM
    /// calls from input-synchronous contexts; see RPC_E_CANTCALLOUT_ININPUTSYNCCALL).
    /// </summary>
    private SynchronizationContext? _syncContext;

    public LinterApplicationContext()
    {
        _settingsStore = new SettingsStore();
        _settings = _settingsStore.Load();

        Log.Trace($"== session start | hotkey vk=0x{_settings.Hotkeys.Convert.VirtualKey:X2} mods={_settings.Hotkeys.Convert.Modifiers} | langs=[{string.Join(", ", _settings.Languages)}]");

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

        // Capture the sync context the first time we run inside the hook.
        // Inside the LL hook callback we are on the UI thread and the
        // WinForms sync context is fully installed — but we must NOT call
        // COM here (input-synchronous restriction).
        _syncContext ??= SynchronizationContext.Current ?? new SynchronizationContext();

        if (_settings.Hotkeys.IsConvertHotkey(e.Stroke))
        {
            e.Handled = true;
            var snapshot = _buffer.Snapshot();
            // Reset synchronously so a second hotkey press doesn't double-convert.
            _buffer.ResetAll();
            DeferConvert(snapshot);
            return;
        }

        var transition = _buffer.Apply(e.Stroke);
        if (transition.WordCompleted && _settings.SpellCheckEnabled)
        {
            DeferSpellCheck(transition.CompletedWord);
        }
    }

    private void DeferConvert(WordSnapshot snapshot)
    {
        _syncContext!.Post(static (state) =>
        {
            var (self, snap) = ((LinterApplicationContext, WordSnapshot))state!;
            try { self._converter.ConvertLastWord(snap); }
            catch (Exception ex) { CrashReporter.Report(ex); }
        }, (this, snapshot));
    }

    private void DeferSpellCheck(string word)
    {
        _syncContext!.Post(static (state) =>
        {
            var (self, w) = ((LinterApplicationContext, string))state!;
            try
            {
                var ok = self._spell.IsCorrect(w, self._settings.SpellCheckLanguages);
                Log.Trace($"word completed: \"{w}\" correct={ok}");
                if (!ok) self._sound.PlayMisspelledSignal();
            }
            catch (Exception ex) { CrashReporter.Report(ex); }
        }, (this, word));
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
