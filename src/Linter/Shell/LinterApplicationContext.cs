using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Linter.Configuration;
using Linter.Diagnostics;
using Linter.Interop;
using Linter.Keyboard;
using Linter.Localization;
using Linter.Services;
using Linter.Text;

namespace Linter.Shell;

/// <summary>
/// Owns the lifetime of every long-lived component and wires the keyboard
/// hook to the converter and spell checker. One file == one wiring diagram.
/// <para>
/// Everything here runs on the UI thread: the hook callback, the work it
/// posts, and the tray/settings handlers. No field needs a lock.
/// </para>
/// </summary>
internal sealed class LinterApplicationContext : ApplicationContext
{
    /// <summary>A word the spell checker just rejected, and the boundary typed after it.</summary>
    private readonly record struct FlaggedWord(string Word, string Trailing);

    private readonly AppSettings _settings;
    private readonly SettingsStore _settingsStore;

    private readonly KeyboardHook _hook;
    private readonly MouseClickWatcher _mouse;
    private readonly InputSimulator _input;
    private readonly LayoutSwitcher _layouts;

    private readonly WordBuffer _buffer;
    private readonly LayoutConverter _converter;
    private readonly SelectionConverter _selection;
    private readonly SpellChecker _spell;
    private readonly SoundNotifier _sound;

    private readonly TrayIcon _tray;
    private SettingsForm? _settingsForm;

    /// <summary>
    /// Captured lazily on first hook callback — at construction time the
    /// WinForms sync context might not be installed yet. Used to defer
    /// COM-heavy work out of the LL hook callback (Windows forbids COM
    /// calls from input-synchronous contexts; see RPC_E_CANTCALLOUT_ININPUTSYNCCALL).
    /// </summary>
    private SynchronizationContext? _syncContext;

    /// <summary>
    /// The most recent conversion, kept so a second hotkey press undoes it.
    /// Cleared as soon as the user types anything else.
    /// </summary>
    private ConversionResult? _pendingUndo;

    /// <summary>
    /// The word that was just signalled, while it is still the last thing
    /// on screen — what the Fix and Ignore hotkeys act on. Cleared by the
    /// next keystroke.
    /// </summary>
    private FlaggedWord? _pendingFix;

    /// <summary>The last signalled word, kept for the tray's "Ignore" item even after more typing.</summary>
    private string? _lastFlaggedWord;

    /// <summary>
    /// Counts keystrokes that change the text. A spell check is answered
    /// later, off the hook; comparing serials tells whether the user has
    /// typed on in the meantime, in which case the word is no longer last.
    /// </summary>
    private int _typingSerial;

    /// <summary>The window the buffered keystrokes were typed into.</summary>
    private IntPtr _lastForeground;

    /// <summary>A hotkey we swallowed and that is still held: its auto-repeats are swallowed too.</summary>
    private uint _swallowedKey;

    /// <summary>Set while the settings window is waiting for the user to press a new hotkey.</summary>
    private Action<Hotkey?>? _capture;
    private uint _captureModifier;

    public LinterApplicationContext()
    {
        _settingsStore = new SettingsStore();
        _settings = _settingsStore.Load();
        Log.Enabled = _settings.DebugLog;
        Localizer.Use(_settings.Language);

        Log.Trace($"== session start | convert={HotkeyNames.Describe(_settings.Hotkeys.Convert)} | langs=[{string.Join(", ", _settings.Languages)}]");

        _input = new InputSimulator();
        _layouts = new LayoutSwitcher();
        _spell = new SpellChecker(_settings.Languages, _settings.IgnoredWords);
        _sound = new SoundNotifier(_settings);

        _buffer = new WordBuffer();
        _converter = new LayoutConverter(_input, _layouts, _spell);
        _selection = new SelectionConverter(_input, _layouts, _spell);

        _hook = new KeyboardHook();
        _hook.KeyIntercepted += OnKeyIntercepted;
        _hook.KeyReleased += OnKeyReleased;
        _hook.Start();

        _mouse = new MouseClickWatcher();
        _mouse.Start();

        _tray = new TrayIcon(_settings, OnToggleEnabled, OnOpenSettings, OnExit,
            lastFlaggedWord: () => _lastFlaggedWord,
            onIgnoreLastFlagged: IgnoreLastFlagged);
    }

    // ---------------------------------------------------------------- keyboard

    private void OnKeyIntercepted(object? sender, KeyInterceptedEventArgs e)
    {
        if (_capture is not null)
        {
            CaptureHotkey(e);
            return;
        }

        // Holding a hotkey must act once, not flip back and forth at the
        // keyboard's repeat rate.
        if (e.IsRepeat && e.Stroke.VirtualKey == _swallowedKey)
        {
            e.Handled = true;
            return;
        }

        if (!_settings.Enabled) return;

        // Stay completely passive in excluded apps (RDP, games, terminals).
        if (_settings.ExcludedProcesses.Count > 0 && IsExcludedForeground()) return;

        // Capture the sync context the first time we run inside the hook.
        // Inside the LL hook callback we are on the UI thread and the
        // WinForms sync context is fully installed — but we must NOT call
        // COM here (input-synchronous restriction).
        _syncContext ??= SynchronizationContext.Current ?? new SynchronizationContext();

        // What we buffered describes the text before the caret in one
        // particular window. After a click or a window switch it describes
        // nothing — acting on it would delete text somewhere else.
        // (Non-short-circuit "|": both checks must run so both reset.)
        if (_mouse.ConsumeClick() | ForegroundChanged()) DropTypingState();

        var stroke = e.Stroke;
        var hotkeys = _settings.Hotkeys;

        if (hotkeys.Convert.Matches(stroke))
        {
            Swallow(e);

            // A second press with nothing typed in between undoes the last
            // conversion instead of trying to convert an empty buffer.
            if (_pendingUndo is { } undo)
            {
                _pendingUndo = null;
                Defer(() => _converter.Undo(undo));
                return;
            }

            var snapshot = _buffer.Snapshot();
            // Reset synchronously so a second hotkey press doesn't double-convert.
            DropTypingState();
            // Remember the edit so the next hotkey press can undo it.
            Defer(() => _pendingUndo = _converter.ConvertLastWord(snapshot));
            return;
        }

        if (hotkeys.ConvertSelection.Matches(stroke))
        {
            Swallow(e);
            DropTypingState();
            Defer(() => _ = ConvertSelectionAsync());
            return;
        }

        // Fix and Ignore only exist right after a signal. Otherwise the key
        // belongs to the application, so an everyday key can be used here.
        if (_pendingFix is { } flagged)
        {
            if (hotkeys.Fix.Matches(stroke))
            {
                Swallow(e);
                DropTypingState();
                Defer(() => FixWord(flagged));
                return;
            }
            if (hotkeys.Ignore.Matches(stroke))
            {
                Swallow(e);
                _pendingFix = null;
                Defer(() => IgnoreWord(flagged.Word));
                return;
            }
        }

        // Any real keystroke invalidates the pending undo and fix (but not
        // bare modifiers/dead keys, so Shift-then-hotkey still works).
        if (WordBoundary.Classify(stroke) != StrokeKind.Ignored)
        {
            _pendingUndo = null;
            _pendingFix = null;
            _typingSerial++;
        }

        var transition = _buffer.Apply(stroke);
        if (transition.WordCompleted && _settings.SpellCheckEnabled)
        {
            var serial = _typingSerial;
            Defer(() => CheckSpelling(transition.CompletedWord, transition.Boundary, serial));
        }
    }

    private void OnKeyReleased(uint virtualKey)
    {
        if (virtualKey == _swallowedKey) _swallowedKey = 0;

        // A modifier pressed and released on its own is a hotkey by itself (Right Alt).
        if (_capture is not null && virtualKey == _captureModifier)
        {
            FinishCapture(new Hotkey(virtualKey, KeyModifiers.None));
        }
    }

    private void Swallow(KeyInterceptedEventArgs e)
    {
        e.Handled = true;
        _swallowedKey = e.Stroke.VirtualKey;
    }

    private void DropTypingState()
    {
        _buffer.ResetAll();
        _pendingUndo = null;
        _pendingFix = null;
    }

    private bool ForegroundChanged()
    {
        var hwnd = NativeMethods.GetForegroundWindow();
        if (hwnd == _lastForeground) return false;
        _lastForeground = hwnd;
        return true;
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

    /// <summary>Runs <paramref name="work"/> on the UI thread after the hook callback has returned.</summary>
    private void Defer(Action work)
    {
        _syncContext!.Post(static state =>
        {
            try { ((Action)state!)(); }
            catch (Exception ex) { CrashReporter.Report(ex); }
        }, work);
    }

    // ---------------------------------------------------------------- deferred work

    private void CheckSpelling(string word, string boundary, int serial)
    {
        var ok = _spell.IsCorrect(word, _settings.SpellCheckLanguages);
        Log.Trace($"word completed: \"{word}\" correct={ok}");
        if (ok) return;

        _sound.PlayMisspelledSignal();
        _lastFlaggedWord = word;

        // Offer the fix only while the word is still the last thing typed,
        // and only if we can put its boundary back (a space or punctuation —
        // not Enter, which may already have sent the message).
        if (serial == _typingSerial && IsRetypable(boundary))
        {
            _pendingFix = new FlaggedWord(word, boundary);
        }
    }

    private static bool IsRetypable(string boundary)
    {
        if (boundary.Length == 0) return false;
        foreach (var ch in boundary)
        {
            if (char.IsControl(ch)) return false;
        }
        return true;
    }

    private void FixWord(FlaggedWord flagged)
    {
        var suggestion = _spell.Suggest(flagged.Word, _settings.SpellCheckLanguages);
        Log.Trace($"fix | \"{flagged.Word}\" → {(suggestion is null ? "no suggestion" : $"\"{suggestion}\"")}");
        if (suggestion is null) return;

        _input.Backspace(flagged.Word.Length + flagged.Trailing.Length);
        _input.TypeUnicode(suggestion + flagged.Trailing);
    }

    private void IgnoreWord(string word)
    {
        Log.Trace($"ignore | \"{word}\"");
        _spell.Ignore(word);
        if (!_settings.IgnoredWords.Exists(w => string.Equals(w, word, StringComparison.OrdinalIgnoreCase)))
        {
            _settings.IgnoredWords.Add(word);
            _settingsStore.Save(_settings);
        }
        if (string.Equals(_lastFlaggedWord, word, StringComparison.OrdinalIgnoreCase)) _lastFlaggedWord = null;
    }

    private void IgnoreLastFlagged()
    {
        if (_lastFlaggedWord is { } word) IgnoreWord(word);
        _pendingFix = null;
    }

    private async Task ConvertSelectionAsync()
    {
        try { await _selection.ConvertAsync(); }
        catch (Exception ex) { CrashReporter.Report(ex); }
    }

    // ---------------------------------------------------------------- hotkey capture

    private void BeginHotkeyCapture(Action<Hotkey?> done)
    {
        _capture = done;
        _captureModifier = 0;
    }

    private void CancelHotkeyCapture() => _capture = null;

    private void CaptureHotkey(KeyInterceptedEventArgs e)
    {
        var vk = e.Stroke.VirtualKey;
        if (VirtualKeyCodes.IsModifier(vk))
        {
            // Let it through: swallowing Shift would hide it from the
            // "Shift + Pause" that may follow. Alone, it completes on release.
            _captureModifier = vk;
            return;
        }

        Swallow(e);
        if (e.IsRepeat) return;
        if (vk == VirtualKeyCodes.Escape)
        {
            FinishCapture(null);
            return;
        }

        const KeyModifiers chord = KeyModifiers.Shift | KeyModifiers.Control | KeyModifiers.Alt | KeyModifiers.Win;
        FinishCapture(new Hotkey(vk, e.Stroke.Modifiers & chord));
    }

    private void FinishCapture(Hotkey? hotkey)
    {
        var done = _capture;
        _capture = null;
        _captureModifier = 0;
        done?.Invoke(hotkey);
    }

    // ---------------------------------------------------------------- tray / settings

    private void OnToggleEnabled()
    {
        _settings.Enabled = !_settings.Enabled;
        _settingsStore.Save(_settings);
        // Keystrokes typed while off were not seen; the buffer is stale.
        DropTypingState();
        _tray.Refresh();
    }

    private void OnOpenSettings()
    {
        if (_settingsForm is not null)
        {
            _settingsForm.Activate();
            return;
        }

        var host = new SettingsFormHost(
            HasDictionary: _spell.IsLanguageSupported,
            InstalledLanguages: _spell.InstalledLanguages,
            BeginHotkeyCapture: BeginHotkeyCapture,
            CancelHotkeyCapture: CancelHotkeyCapture,
            Saved: OnSettingsSaved);

        _settingsForm = new SettingsForm(_settings, host);
        _settingsForm.FormClosed += (_, _) =>
        {
            CancelHotkeyCapture();
            _settingsForm?.Dispose();
            _settingsForm = null;
        };
        _settingsForm.Show();
        _settingsForm.Activate();
    }

    /// <summary>
    /// The settings window has already written the new values into
    /// <see cref="_settings"/>; persist them and push them into the parts
    /// that cache configuration. Everything else reads the object live.
    /// </summary>
    private void OnSettingsSaved()
    {
        _settingsStore.Save(_settings);
        Log.Enabled = _settings.DebugLog;
        Localizer.Use(_settings.Language);
        _spell.Reconfigure(_settings.Languages, _settings.IgnoredWords);
        DropTypingState();
        _tray.Refresh();
    }

    private void OnExit()
    {
        _settingsForm?.Close();
        ExitThread();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _hook.KeyIntercepted -= OnKeyIntercepted;
            _hook.KeyReleased -= OnKeyReleased;
            _hook.Dispose();
            _mouse.Dispose();
            _tray.Dispose();
            _spell.Dispose();
        }
        base.Dispose(disposing);
    }
}
