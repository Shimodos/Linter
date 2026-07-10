# Linter

Native Windows tray utility in the spirit of Punto Switcher — converts a
phrase typed under the wrong keyboard layout to the right one with a single
hotkey, and beeps when the word you just typed is misspelled.

Supported languages out of the box: **English, Russian, Ukrainian, Spanish.**
Any keyboard layout installed in Windows can be added — the converter uses
`ToUnicodeEx` to render candidates for every installed layout and picks the
one the spell checker accepts.

## Status

Functional. The core flow works end-to-end: hotkey-driven phrase conversion
with one-press undo, a spell-check beep, a personal ignore list, per-app
exclusions, a configurable signal sound, and optional start-with-Windows. UI
is a tray icon with an on/off toggle.

## How it works

Linter installs a single system-wide low-level keyboard hook and, for every
keystroke, records the raw key data (virtual key, scan code, modifiers) — not
just the character. Keeping the raw data is what lets it re-type the same
keystrokes under a *different* layout later.

- **Buffering.** Keystrokes accumulate into the current *phrase* — everything
  since the last "hard" boundary (Enter, punctuation). Spaces are "soft"
  boundaries: they end a word but keep the phrase alive, so a whole sentence
  typed in the wrong layout can be repaired in one shot. Moving the caret
  (arrows, Home/End, mouse) drops the buffer, because the tool can no longer
  be sure what is on screen.

- **Conversion.** On the hotkey, Linter re-renders the buffered phrase under
  every *other* installed layout, spell-checks each rendering, and keeps the
  one with the most real words. If a candidate wins, it backspaces the
  original text, switches layout, and types the winning text back in. If no
  candidate produces a real word, the text is left untouched. Pressing the
  hotkey again — with nothing typed in between — undoes the edit.

- **Script-aware spelling.** A word is only checked against dictionaries of a
  matching script: a Cyrillic dictionary has no opinion on Latin text (it
  would otherwise call any Latin gibberish "correct" simply because none of
  the letters are in its alphabet). Words in the ignore list are always
  treated as correct.

- **Off the hook thread.** COM spell-check and input injection are *not* run
  inside the hook callback (Windows forbids COM there and penalises slow
  hooks). They are posted to the UI message loop instead, so the hook returns
  immediately and Windows never disables it.

## Requirements

- Windows 10 1809+ / Windows 11
- .NET 8 SDK to build
- Spell checking dictionaries for the languages you use (Windows installs
  EN / RU / UK / ES on demand via *Settings → Time & language → Language*)

## Build & run

```powershell
dotnet build src/Linter/Linter.csproj -c Release
dotnet run   --project src/Linter/Linter.csproj -c Release
```

A single self-contained binary:

```powershell
dotnet publish src/Linter/Linter.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true
```

## Default hotkeys

| Hotkey       | Action                                                |
|--------------|-------------------------------------------------------|
| `Right Alt`  | Convert the word currently being typed to the correct layout |
| `Right Alt` (again) | Undo the conversion that was just made         |
| `Shift+Pause`| Convert the current selection (planned)               |

Hotkeys are configurable in `%AppData%\Linter\settings.json`.

## Settings

`%AppData%\Linter\settings.json` also exposes:

| Key                 | Meaning                                                          |
|---------------------|------------------------------------------------------------------|
| `ignoredWords`      | Words to never beep on or convert (nicknames, brands, slang)     |
| `excludedProcesses` | Foreground process names (no `.exe`) where the hook stays passive |
| `soundFile`         | Path to a `.wav` played instead of the system beep               |

"Start with Windows" is toggled from the tray menu (per-user `Run` registry key).

## Project layout

```
src/Linter/
├── Shell/            — tray icon + ApplicationContext that wires everything
├── Interop/          — P/Invoke signatures, structs, COM (spell-check)
├── Keyboard/         — global hook, SendInput, layout switching, ToUnicodeEx
├── Text/             — typed-key buffer + word-boundary classification
├── Services/         — layout converter, spell checker, sound, autostart
├── Configuration/    — settings model + JSON store
└── Diagnostics/      — append-only debug log (opened from the tray menu)
```

Each file owns one small responsibility — this project is meant to be
read top-to-bottom by humans, not just compiled.

## License

MIT — see [LICENSE](LICENSE).
