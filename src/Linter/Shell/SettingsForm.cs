using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Windows.Forms;
using Linter.Configuration;
using Linter.Localization;
using Linter.Services;

namespace Linter.Shell;

/// <summary>What the settings window needs from the running app.</summary>
/// <param name="HasDictionary">Whether Windows has a spelling dictionary for a language tag.</param>
/// <param name="InstalledLanguages">Every language Windows has a dictionary for.</param>
/// <param name="BeginHotkeyCapture">
/// Routes the next key press to the callback instead of the application —
/// the only way to see keys the global hook would otherwise swallow, and to
/// tell Right Alt from Left Alt. The callback gets null if Esc was pressed.
/// </param>
/// <param name="CancelHotkeyCapture">Stops waiting for a key.</param>
/// <param name="Saved">Called after OK has written the new values into the settings object.</param>
internal sealed record SettingsFormHost(
    Func<string, bool> HasDictionary,
    Func<IReadOnlyList<string>> InstalledLanguages,
    Action<Action<Hotkey?>> BeginHotkeyCapture,
    Action CancelHotkeyCapture,
    Action Saved);

/// <summary>
/// The settings window. Edits happen on the controls only; nothing touches
/// <see cref="AppSettings"/> until OK, so Cancel needs no rollback.
/// Built in code (no designer file) to keep the project a plain folder of
/// readable .cs files.
/// </summary>
internal sealed class SettingsForm : Form
{
    private sealed record LanguageItem(string Tag, bool HasDictionary)
    {
        public override string ToString()
        {
            var name = Tag;
            try { name = $"{CultureInfo.GetCultureInfo(Tag).NativeName}  ({Tag})"; }
            catch (CultureNotFoundException) { }
            return HasDictionary ? name : $"{name}  — {Localizer.T("spelling.noDictionary")}";
        }
    }

    /// <summary><c>File</c> is null for the system beep; the custom entry reads <see cref="_customSound"/>.</summary>
    private sealed record SoundChoice(string Name, string? File, bool IsCustom = false)
    {
        public override string ToString() => Name;
    }

    /// <summary><c>Code</c> is what goes into settings: "auto", "en", "ru", "uk".</summary>
    private sealed record UiLanguage(string Name, string Code)
    {
        public override string ToString() => Name;
    }

    /// <summary>One configurable hotkey: its current value and the controls that show it.</summary>
    private sealed class HotkeyRow
    {
        public HotkeyRow(string titleKey, string hintKey, Hotkey value)
        {
            TitleKey = titleKey;
            HintKey = hintKey;
            Value = value;
        }

        public string TitleKey { get; }
        public string HintKey { get; }
        public Hotkey Value { get; set; }

        public Label Display { get; } = new()
        {
            AutoSize = false,
            Size = new Size(190, 25),
            BorderStyle = BorderStyle.FixedSingle,
            BackColor = SystemColors.Window,
            TextAlign = ContentAlignment.MiddleLeft,
            Padding = new Padding(4, 0, 0, 0),
            Margin = new Padding(3, 3, 6, 3),
        };

        public void ShowValue()
        {
            Display.Text = Value.IsSet ? HotkeyNames.Describe(Value) : Localizer.T("hotkey.none");
            Display.ForeColor = Value.IsSet ? SystemColors.WindowText : SystemColors.GrayText;
        }
    }

    private readonly AppSettings _settings;
    private readonly SettingsFormHost _host;

    private readonly CheckBox _enabled = NewCheckBox("general.enable");
    private readonly CheckBox _autostart = NewCheckBox("general.autostart");
    private readonly CheckBox _debugLog = NewCheckBox("general.debugLog");
    private readonly ComboBox _uiLanguage = NewDropDownList(190);

    private readonly List<HotkeyRow> _hotkeys = new();
    private HotkeyRow? _capturing;

    private readonly CheckBox _spellCheck = NewCheckBox("spelling.signal");
    private readonly CheckedListBox _languages = new() { CheckOnClick = true, IntegralHeight = false };
    private readonly ComboBox _newLanguage = NewDropDownList(220);
    private readonly Button _addLanguage = NewButton("spelling.add");
    private readonly ComboBox _sound = NewDropDownList(170);
    private readonly Label _soundPath = new()
    {
        AutoSize = false,
        AutoEllipsis = true,
        Height = 18,
        Anchor = AnchorStyles.Left | AnchorStyles.Right,
        ForeColor = SystemColors.GrayText,
    };
    private string? _customSound;
    private int _lastSoundIndex;

    private readonly TextBox _ignoredWords = NewListBox();
    private readonly TextBox _excludedApps = NewListBox();

    public SettingsForm(AppSettings settings, SettingsFormHost host)
    {
        _settings = settings;
        _host = host;

        var keys = settings.Hotkeys;
        _hotkeys.Add(new HotkeyRow("hotkey.convert", "hotkey.convertHint", keys.Convert));
        _hotkeys.Add(new HotkeyRow("hotkey.selection", "hotkey.selectionHint", keys.ConvertSelection));
        _hotkeys.Add(new HotkeyRow("hotkey.fix", "hotkey.fixHint", keys.Fix));
        _hotkeys.Add(new HotkeyRow("hotkey.ignore", "hotkey.ignoreHint", keys.Ignore));

        SuspendLayout();
        AutoScaleDimensions = new SizeF(96F, 96F);
        AutoScaleMode = AutoScaleMode.Dpi;
        Text = Localizer.T("settings.title");
        Icon = AppIcons.Load(AppIcons.On, SystemInformation.IconSize);
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        StartPosition = FormStartPosition.CenterScreen;
        ClientSize = new Size(500, 560);

        var tabs = new TabControl { Dock = DockStyle.Fill };
        tabs.TabPages.Add(BuildGeneralTab());
        tabs.TabPages.Add(BuildHotkeysTab());
        tabs.TabPages.Add(BuildSpellingTab());
        tabs.TabPages.Add(BuildExceptionsTab());
        tabs.Selecting += (_, _) => StopCapture();

        var ok = NewButton("button.ok");
        var cancel = NewButton("button.cancel");
        ok.MinimumSize = cancel.MinimumSize = new Size(84, 28);
        ok.Click += (_, _) => SaveAndClose();
        cancel.Click += (_, _) => Close();
        AcceptButton = ok;
        CancelButton = cancel;

        var buttons = new FlowLayoutPanel
        {
            Dock = DockStyle.Bottom,
            FlowDirection = FlowDirection.RightToLeft,
            AutoSize = true,
            Padding = new Padding(8),
        };
        buttons.Controls.Add(cancel);
        buttons.Controls.Add(ok);

        var body = new Panel { Dock = DockStyle.Fill, Padding = new Padding(8, 8, 8, 0) };
        body.Controls.Add(tabs);
        Controls.Add(body);
        Controls.Add(buttons);

        LoadFromSettings();
        ResumeLayout(performLayout: true);
    }

    // While we wait for a key the global hook swallows every key press. That
    // must never outlive the user's attention to this window.
    protected override void OnDeactivate(EventArgs e)
    {
        StopCapture();
        base.OnDeactivate(e);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing) Icon?.Dispose();
        base.Dispose(disposing);
    }

    // ---------------------------------------------------------------- tabs

    private TabPage BuildGeneralTab()
    {
        var page = NewPage("tab.general", out var rows);
        AddRow(rows, _enabled);
        AddRow(rows, _autostart);
        AddRow(rows, NewHeading("general.language"));
        AddRow(rows, _uiLanguage);
        AddRow(rows, NewHeading("general.diagnostics"));
        AddRow(rows, _debugLog);
        AddRow(rows, NewHint("general.debugLogHint"));
        return page;
    }

    private TabPage BuildHotkeysTab()
    {
        var page = NewPage("tab.hotkeys", out var rows);
        foreach (var hotkey in _hotkeys)
        {
            var change = NewButton("hotkey.change");
            var clear = NewButton("hotkey.clear");
            change.Click += (_, _) => StartCapture(hotkey);
            clear.Click += (_, _) =>
            {
                StopCapture();
                hotkey.Value = Hotkey.None;
                hotkey.ShowValue();
            };

            var line = NewRow();
            line.Controls.AddRange(new Control[] { hotkey.Display, change, clear });

            AddRow(rows, NewHeading(hotkey.TitleKey));
            AddRow(rows, line);
            AddRow(rows, NewHint(hotkey.HintKey));
        }

        var note = NewHint("hotkey.passThrough");
        note.Margin = new Padding(3, 14, 3, 0);
        AddRow(rows, note);
        return page;
    }

    private TabPage BuildSpellingTab()
    {
        var remove = NewButton("spelling.remove");
        _addLanguage.Click += (_, _) => AddLanguage();
        remove.Click += (_, _) => RemoveLanguage();

        var languageButtons = NewRow();
        languageButtons.Controls.AddRange(new Control[] { _newLanguage, _addLanguage, remove });

        var test = NewButton("spelling.test");
        var browse = NewButton("spelling.browse");
        test.Click += (_, _) => SoundNotifier.Play(SelectedSoundFile());
        browse.Click += (_, _) => BrowseForSound();
        _sound.SelectionChangeCommitted += (_, _) => OnSoundPicked();

        var soundRow = NewRow();
        soundRow.Controls.AddRange(new Control[] { _sound, test, browse });

        var page = NewPage("tab.spelling", out var rows);
        AddRow(rows, _spellCheck);
        AddRow(rows, NewHeading("spelling.languages"));
        AddRow(rows, NewHint("spelling.languagesHint"));
        AddRow(rows, _languages, fill: true);
        AddRow(rows, languageButtons);
        AddRow(rows, NewHeading("spelling.sound"));
        AddRow(rows, soundRow);
        AddRow(rows, _soundPath);
        return page;
    }

    private TabPage BuildExceptionsTab()
    {
        var page = NewPage("tab.exceptions", out var rows);
        AddRow(rows, NewHeading("exceptions.words"));
        AddRow(rows, NewHint("exceptions.wordsHint"));
        AddRow(rows, _ignoredWords, fill: true);
        AddRow(rows, NewHeading("exceptions.apps"));
        AddRow(rows, NewHint("exceptions.appsHint"));
        AddRow(rows, _excludedApps, fill: true);
        return page;
    }

    // ---------------------------------------------------------------- load / save

    private void LoadFromSettings()
    {
        _enabled.Checked = _settings.Enabled;
        _autostart.Checked = AutostartManager.IsEnabled();
        _debugLog.Checked = _settings.DebugLog;
        _spellCheck.Checked = _settings.SpellCheckEnabled;

        _uiLanguage.Items.Add(new UiLanguage(Localizer.T("general.langAuto"), "auto"));
        for (var i = 0; i < Localizer.Languages.Length; i++)
        {
            _uiLanguage.Items.Add(new UiLanguage(Localizer.NativeNames[i], Localizer.Languages[i]));
        }
        _uiLanguage.SelectedIndex = Math.Max(0, Array.IndexOf(Localizer.Languages, _settings.Language) + 1);

        foreach (var hotkey in _hotkeys) hotkey.ShowValue();

        foreach (var tag in _settings.Languages)
        {
            var signals = _settings.SpellCheckLanguages.Exists(t => string.Equals(t, tag, StringComparison.OrdinalIgnoreCase));
            _languages.Items.Add(new LanguageItem(tag, _host.HasDictionary(tag)), signals);
        }
        RefillInstalledLanguages();

        _sound.Items.Add(new SoundChoice(Localizer.T("spelling.systemBeep"), null));
        foreach (var file in SoundNotifier.BuiltInSounds())
        {
            _sound.Items.Add(new SoundChoice(DisplayName(file), file));
        }
        _sound.Items.Add(new SoundChoice(Localizer.T("spelling.customFile"), null, IsCustom: true));
        SelectSound(_settings.SoundFile);

        _ignoredWords.Lines = _settings.IgnoredWords.ToArray();
        _excludedApps.Lines = _settings.ExcludedProcesses.ToArray();
    }

    private void SaveAndClose()
    {
        StopCapture();

        _settings.Enabled = _enabled.Checked;
        _settings.DebugLog = _debugLog.Checked;
        _settings.SpellCheckEnabled = _spellCheck.Checked;
        if (_uiLanguage.SelectedItem is UiLanguage language) _settings.Language = language.Code;

        _settings.Hotkeys.Convert = _hotkeys[0].Value;
        _settings.Hotkeys.ConvertSelection = _hotkeys[1].Value;
        _settings.Hotkeys.Fix = _hotkeys[2].Value;
        _settings.Hotkeys.Ignore = _hotkeys[3].Value;

        var languages = new List<string>();
        var signalling = new List<string>();
        for (var i = 0; i < _languages.Items.Count; i++)
        {
            if (_languages.Items[i] is not LanguageItem item) continue;
            languages.Add(item.Tag);
            if (_languages.GetItemChecked(i)) signalling.Add(item.Tag);
        }
        _settings.Languages = languages;
        _settings.SpellCheckLanguages = signalling;

        _settings.SoundFile = SelectedSoundFile();
        _settings.IgnoredWords = CleanLines(_ignoredWords.Lines, stripExe: false);
        _settings.ExcludedProcesses = CleanLines(_excludedApps.Lines, stripExe: true);

        // The registry is the single source of truth for autostart.
        if (_autostart.Checked != AutostartManager.IsEnabled()) AutostartManager.Set(_autostart.Checked);

        _host.Saved();
        Close();
    }

    /// <summary>Trims, drops blanks and case-insensitive duplicates; optionally strips ".exe".</summary>
    internal static List<string> CleanLines(string[] lines, bool stripExe)
    {
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var result = new List<string>();
        foreach (var raw in lines)
        {
            var line = raw.Trim();
            if (stripExe && line.EndsWith(".exe", StringComparison.OrdinalIgnoreCase)) line = line[..^4];
            if (line.Length > 0 && seen.Add(line)) result.Add(line);
        }
        return result;
    }

    // ---------------------------------------------------------------- hotkeys

    private void StartCapture(HotkeyRow row)
    {
        StopCapture();
        _capturing = row;
        row.Display.Text = Localizer.T("hotkey.press");
        row.Display.ForeColor = SystemColors.HotTrack;

        // The callback runs inside the keyboard hook: keep it short, no dialogs.
        _host.BeginHotkeyCapture(captured =>
        {
            _capturing = null;
            if (captured is not null)
            {
                // A key can only do one thing: taking it here frees it elsewhere.
                foreach (var other in _hotkeys)
                {
                    if (other == row || !other.Value.SameAs(captured)) continue;
                    other.Value = Hotkey.None;
                    other.ShowValue();
                }
                row.Value = captured;
            }
            row.ShowValue();
        });
    }

    private void StopCapture()
    {
        if (_capturing is null) return;
        _host.CancelHotkeyCapture();
        _capturing.ShowValue();
        _capturing = null;
    }

    // ---------------------------------------------------------------- languages

    /// <summary>Offers the installed dictionaries that are not in the list yet.</summary>
    private void RefillInstalledLanguages()
    {
        var listed = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var item in _languages.Items)
        {
            if (item is LanguageItem language) listed.Add(language.Tag);
        }

        var available = new List<LanguageItem>();
        foreach (var tag in _host.InstalledLanguages())
        {
            if (!listed.Contains(tag)) available.Add(new LanguageItem(tag, HasDictionary: true));
        }
        available.Sort((a, b) => string.Compare(a.ToString(), b.ToString(), StringComparison.CurrentCultureIgnoreCase));

        _newLanguage.Items.Clear();
        _newLanguage.Items.AddRange(available.ToArray());
        if (available.Count > 0) _newLanguage.SelectedIndex = 0;
        _newLanguage.Enabled = _addLanguage.Enabled = available.Count > 0;
    }

    private void AddLanguage()
    {
        if (_newLanguage.SelectedItem is not LanguageItem language) return;
        _languages.SelectedIndex = _languages.Items.Add(language, isChecked: false);
        RefillInstalledLanguages();
    }

    private void RemoveLanguage()
    {
        var index = _languages.SelectedIndex;
        if (index < 0) return;
        _languages.Items.RemoveAt(index);
        if (_languages.Items.Count > 0) _languages.SelectedIndex = Math.Min(index, _languages.Items.Count - 1);
        RefillInstalledLanguages();
    }

    // ---------------------------------------------------------------- sound

    private string? SelectedSoundFile()
        => _sound.SelectedItem is SoundChoice choice
            ? (choice.IsCustom ? _customSound : choice.File)
            : null;

    private void SelectSound(string? soundFile)
    {
        var index = 0; // system beep
        if (!string.IsNullOrWhiteSpace(soundFile))
        {
            if (Path.IsPathRooted(soundFile))
            {
                _customSound = soundFile;
                index = _sound.Items.Count - 1;
            }
            else
            {
                for (var i = 0; i < _sound.Items.Count; i++)
                {
                    if (_sound.Items[i] is SoundChoice choice
                        && string.Equals(choice.File, soundFile, StringComparison.OrdinalIgnoreCase))
                    {
                        index = i;
                    }
                }
            }
        }
        _sound.SelectedIndex = index;
        _lastSoundIndex = index;
        UpdateSoundPath();
    }

    private void OnSoundPicked()
    {
        if (_sound.SelectedItem is SoundChoice { IsCustom: true } && _customSound is null)
        {
            BrowseForSound();
            return;
        }
        _lastSoundIndex = _sound.SelectedIndex;
        UpdateSoundPath();
        // Picking a sound previews it — quicker than reaching for Test each time.
        SoundNotifier.Play(SelectedSoundFile());
    }

    private void BrowseForSound()
    {
        using var dialog = new OpenFileDialog
        {
            Title = Localizer.T("spelling.chooseSound"),
            Filter = Localizer.T("spelling.waveFilter"),
            CheckFileExists = true,
        };
        if (_customSound is not null && File.Exists(_customSound))
        {
            dialog.InitialDirectory = Path.GetDirectoryName(_customSound);
            dialog.FileName = Path.GetFileName(_customSound);
        }

        if (dialog.ShowDialog(this) == DialogResult.OK)
        {
            _customSound = dialog.FileName;
            _sound.SelectedIndex = _sound.Items.Count - 1;
            _lastSoundIndex = _sound.SelectedIndex;
            SoundNotifier.Play(_customSound);
        }
        else
        {
            // Cancelled: don't leave "Custom file…" selected with no file behind it.
            _sound.SelectedIndex = _lastSoundIndex;
        }
        UpdateSoundPath();
    }

    private void UpdateSoundPath()
    {
        _soundPath.Text = _sound.SelectedItem is SoundChoice { IsCustom: true } ? _customSound ?? string.Empty : string.Empty;
    }

    /// <summary>"tick.wav" → "Tick".</summary>
    private static string DisplayName(string file)
    {
        var name = Path.GetFileNameWithoutExtension(file).Replace('-', ' ').Replace('_', ' ');
        return name.Length == 0 ? file : char.ToUpperInvariant(name[0]) + name[1..];
    }

    // ---------------------------------------------------------------- layout helpers

    /// <summary>A tab whose content is a single top-to-bottom column.</summary>
    private static TabPage NewPage(string titleKey, out TableLayoutPanel rows)
    {
        rows = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            Padding = new Padding(10),
        };
        rows.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        var page = new TabPage(Localizer.T(titleKey)) { UseVisualStyleBackColor = true };
        page.Controls.Add(rows);
        return page;
    }

    /// <summary>
    /// Appends a control as the next row. Rows size to their content; a
    /// <paramref name="fill"/> row instead shares the tab's leftover height
    /// with the other filling rows.
    /// </summary>
    private static void AddRow(TableLayoutPanel rows, Control control, bool fill = false)
    {
        if (fill) control.Dock = DockStyle.Fill;
        rows.RowStyles.Add(fill ? new RowStyle(SizeType.Percent, 100F) : new RowStyle(SizeType.AutoSize));
        rows.RowCount = rows.RowStyles.Count;
        rows.Controls.Add(control, 0, rows.RowStyles.Count - 1);
    }

    private static FlowLayoutPanel NewRow() => new()
    {
        AutoSize = true,
        WrapContents = false,
        Margin = new Padding(0, 0, 0, 2),
    };

    private static CheckBox NewCheckBox(string textKey) => new()
    {
        Text = Localizer.T(textKey),
        AutoSize = true,
        Margin = new Padding(3, 4, 12, 4),
    };

    private static Button NewButton(string textKey) => new() { Text = Localizer.T(textKey), AutoSize = true };

    private static ComboBox NewDropDownList(int width) => new() { DropDownStyle = ComboBoxStyle.DropDownList, Width = width };

    private static TextBox NewListBox() => new()
    {
        Multiline = true,
        AcceptsReturn = true,
        ScrollBars = ScrollBars.Vertical,
        WordWrap = false,
    };

    private static Label NewHeading(string textKey) => new()
    {
        Text = Localizer.T(textKey),
        AutoSize = true,
        Font = new Font(SystemFonts.MessageBoxFont ?? Control.DefaultFont, FontStyle.Bold),
        Margin = new Padding(3, 12, 3, 4),
    };

    private static Label NewHint(string textKey) => new()
    {
        Text = Localizer.T(textKey),
        AutoSize = true,
        MaximumSize = new Size(450, 0),
        ForeColor = SystemColors.GrayText,
        Margin = new Padding(3, 0, 3, 6),
    };
}
