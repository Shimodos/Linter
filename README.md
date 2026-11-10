# Linter

Native Windows tray utility in the spirit of Punto Switcher — converts a
phrase typed under the wrong keyboard layout to the right one with a single
hotkey, and gives a quiet signal when the word you just typed is misspelled.

Supported languages out of the box: **English, Russian, Ukrainian, Spanish.**
Any keyboard layout installed in Windows can be added — the converter uses
`ToUnicodeEx` to render candidates for every installed layout and picks the
one the spell checker accepts.

## What it does

| Hotkey (default) | Action |
|------------------|--------|
| `Right Alt`      | Convert the phrase you just typed to the right layout. Press again to undo. |
| `Shift+Pause`    | Convert the selected text. |
| `Right Ctrl`     | Right after a signal: replace the word with the dictionary's first suggestion. |
| `F9`             | Right after a signal: add the word to the ignore list. |

`Right Ctrl` and `F9` are only intercepted in the moment right after a
signal, before anything else is typed. The rest of the time they reach the
application untouched.

Every hotkey can be changed (Settings → Hotkeys → Change, then press the key)
or cleared. The interface is available in English, Russian and Ukrainian.

## How it works

Linter installs a single system-wide low-level keyboard hook and, for every
keystroke, records the raw key data (virtual key, scan code, modifiers) — not
just the character. Keeping the raw data is what lets it re-type the same
keystrokes under a *different* layout later.

- **Buffering.** Keystrokes accumulate into the current *phrase* — everything
  since the last "hard" boundary (Enter, punctuation). Spaces are "soft"
  boundaries: they end a word but keep the phrase alive, so a whole sentence
  typed in the wrong layout can be repaired in one shot.

- **Letting go.** The buffer describes the text before the caret in one
  window. Anything that breaks that — arrows, Home/End, Delete, a mouse
  click, a switch to another window, a shortcut such as Ctrl+V or Ctrl+Z —
  drops it, because the tool can no longer be sure what is on screen and
  must not edit text somewhere else.

- **Conversion.** On the hotkey, Linter re-renders the buffered phrase under
  every *other* installed layout, spell-checks each rendering, and keeps the
  one with the most real words. If a candidate wins, it backspaces the
  original text, switches layout, and types the winning text back in. If no
  candidate produces a real word, the text is left untouched. Pressing the
  hotkey again — with nothing typed in between — undoes the edit. Holding the
  key down acts once; auto-repeat is ignored.

- **Selection.** For selected text there are no keystrokes to replay, so it
  works backwards: for each installed layout it asks which keys would have
  typed those characters (`VkKeyScanEx`), replays them under the other
  layouts, and keeps the best-spelled result — or leaves the text alone if it
  already reads fine. The text travels through the clipboard (Ctrl+C / Ctrl+V);
  the previous clipboard text is put back.

- **Script-aware spelling.** A word is only checked against dictionaries of a
  matching script: a Cyrillic dictionary has no opinion on Latin text (it
  would otherwise call any Latin gibberish "correct" simply because none of
  the letters are in its alphabet). Words in the ignore list are always
  treated as correct.

- **Off the hook thread.** COM spell-check and input injection are *not* run
  inside the hook callback (Windows forbids COM there and penalises slow
  hooks). They are posted to the UI message loop instead, so the hook returns
  immediately and Windows never disables it. The mouse hook that notices
  clicks lives on its own thread for the same reason.

## Privacy

Linter has no network code. The current phrase is kept in memory only.
Nothing is written to disk unless you enable the debug log (Settings →
General), which records typed words and exists for troubleshooting.

## Requirements

- Windows 10 1809+ / Windows 11
- .NET 8 SDK to build
- Spell checking dictionaries for the languages you use (Windows installs
  them with each language via *Settings → Time & language → Language & region*)

## Build, run, test

```powershell
dotnet build src/Linter/Linter.csproj -c Release
dotnet run   --project src/Linter/Linter.csproj -c Release
dotnet test  tests/Linter.Tests/Linter.Tests.csproj
```

A single exe (plus the `Sounds` folder, which must stay next to it):

```powershell
dotnet publish src/Linter/Linter.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -o publish
```

Pushing a tag like `v1.1.0` runs the tests and publishes both a
self-contained and a framework-dependent zip as a GitHub release
(`.github/workflows/release.yml`).

## Settings

Right-click the tray icon → **Settings…**. Changes apply as soon as you press
OK — no restart. Everything is stored in `%AppData%\Linter\settings.json`,
which can also be edited by hand:

| Key                   | Meaning                                                          |
|-----------------------|------------------------------------------------------------------|
| `languages`           | Dictionaries used to pick the right layout when converting       |
| `spellCheckLanguages` | Subset of `languages` that signals a misspelling                 |
| `ignoredWords`        | Words to never signal or count against a conversion (nicknames, brands, slang) |
| `excludedProcesses`   | Foreground process names (no `.exe`) where the hook stays passive |
| `soundFile`           | Signal sound: a built-in name (`tick.wav`), a full path to your own `.wav`, or `null` for the system beep |
| `hotkeys`             | `convert`, `convertSelection`, `fix`, `ignore` — each a virtual-key code + modifier mask; key `0` switches the action off |
| `language`            | Interface language: `en`, `ru`, `uk`, or `auto` to follow Windows |
| `debugLog`            | Write a diagnostic trace to `debug.log` (off by default — it records typed words) |

"Start with Windows" lives in the per-user `Run` registry key, not in the file.

### Sounds, icons, screenshots

The built-in sounds (`Sounds/*.wav`) and the icons (`Assets/*.ico`) are
synthesized by a script, so they carry no third-party licence. To change or
add one, edit the script and regenerate:

```powershell
node tools/gen-assets.js
```

Any `.wav` dropped into the `Sounds` folder next to `Linter.exe` shows up in
the sound list.

`tools/snapshot` renders the settings window to PNG files in every interface
language without starting the app (no hook, no tray icon):

```powershell
dotnet run --project tools/snapshot -- docs/shots
```

## Project layout

```
src/Linter/
├── Shell/            — tray icon, settings window + ApplicationContext that wires everything
├── Interop/          — P/Invoke signatures, structs, COM (spell-check)
├── Keyboard/         — keyboard hook, mouse-click watcher, SendInput, layouts, ToUnicodeEx
├── Text/             — typed-key buffer + word-boundary classification (pure, unit-tested)
├── Services/         — phrase and selection converters, spell checker, sound, autostart
├── Configuration/    — settings model, hotkeys + JSON store
├── Localization/     — interface strings (en / ru / uk) in one table
├── Diagnostics/      — opt-in debug log (opened from the tray menu)
└── Assets/, Sounds/  — generated icons and signal sounds
tests/Linter.Tests/   — xUnit tests; the ones needing Windows dictionaries skip themselves where absent
tools/                — gen-assets.js (icons, sounds), snapshot (settings screenshots)
```

Each file owns one small responsibility — this project is meant to be
read top-to-bottom by humans, not just compiled.

## License

MIT — see [LICENSE](LICENSE).
