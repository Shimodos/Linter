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

    /// <summary>
    /// The most recent conversion, kept so a second hotkey press undoes it.
    /// Cleared as soon as the user types anything else. Only ever touched on
    /// the UI thread (hook callback and posted work run on the same thread).
    /// </summary>
    private ConversionResult? _pendingUndo;

    public LinterApplicationContext()
    {
        _settingsStore = new SettingsStore();
        _settings = _settingsStore.Load();

        Log.Trace($"== session start | hotkey vk=0x{_settings.Hotkeys.Convert.VirtualKey:X2} mods={_settings.Hotkeys.Convert.Modifiers} | langs=[{string.Join(", ", _settings.Languages)}]");

        _input = new InputSimulator();
        _layouts = new LayoutSwitcher();
        _spell = new SpellChecker(_settings.Languages, _settings.IgnoredWords);
        _sound = new SoundNotifier(_settings.SoundFile);

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

        // Stay completely passive in excluded apps (RDP, games, terminals).
        if (_settings.ExcludedProcesses.Count > 0 && IsExcludedForeground()) return;

        // Capture the sync context the first time we run inside the hook.
        // Inside the LL hook callback we are on the UI thread and the
        // WinForms sync context is fully installed — but we must NOT call
        // COM here (input-synchronous restriction).
        _syncContext ??= SynchronizationContext.Current ?? new SynchronizationContext();

        if (_settings.Hotkeys.IsConvertHotkey(e.Stroke))
        {
            e.Handled = true;

            // A second press with nothing typed in between undoes the last
            // conversion instead of trying to convert an empty buffer.
            if (_pendingUndo is { } undo)
            {
                _pendingUndo = null;
                DeferUndo(undo);
                return;
            }

            var snapshot = _buffer.Snapshot();
            // Reset synchronously so a second hotkey press doesn't double-convert.
            _buffer.ResetAll();
            DeferConvert(snapshot);
            return;
        }

        // Any real keystroke invalidates the pending undo (but not bare
        // modifiers/dead keys, so Shift-then-hotkey still undoes).
        if (WordBoundary.Classify(e.Stroke) != StrokeKind.Ignored)
        {
            _pendingUndo = null;
        }

        var transition = _buffer.Apply(e.Stroke);
        if (transition.WordCompleted && _settings.SpellCheckEnabled)
        {
            DeferSpellCheck(transition.CompletedWord);
        }
    }

    private bool IsExcludedForeground()
    {
        var name = ForegroundApp.CurrentProcessName();
        if (name.Length == 0) return false;
        foreach (var excluded in _settings.ExcludedProcesses)
        {
            if (string.Equals(excluded, name, StringComparison.OrdinalIgnoreCase)) return true;
        }
        return false;
    }

    private void DeferConvert(WordSnapshot snapshot)
    {
        _syncContext!.Post(static (state) =>
        {
            var (self, snap) = ((LinterApplicationContext, WordSnapshot))state!;
            try
            {
                // Remember the edit so the next hotkey press can undo it.
                // Runs on the UI thread, same as the hook callback that reads it.
                self._pendingUndo = self._converter.ConvertLastWord(snap);
            }
            catch (Exception ex) { CrashReporter.Report(ex); }
        }, (this, snapshot));
    }

    private void DeferUndo(ConversionResult conversion)
    {
        _syncContext!.Post(static (state) =>
        {
            var (self, c) = ((LinterApplicationContext, ConversionResult))state!;
            try { self._converter.Undo(c); }
            catch (Exception ex) { CrashReporter.Report(ex); }
        }, (this, conversion));
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
