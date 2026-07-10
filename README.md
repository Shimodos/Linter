# Linter

Native Windows tray utility in the spirit of Punto Switcher — converts the
last word from a wrong keyboard layout to the right one with a single hotkey,
and beeps when the word you just typed is misspelled.

Supported languages out of the box: **English, Russian, Ukrainian, Spanish.**
Any keyboard layout installed in Windows can be added — the converter uses
`ToUnicodeEx` to render candidates for every installed layout and picks the
one the spell checker accepts.

## Status

Early scaffolding. The architecture is in place; behaviour is functional for
the core flow (hotkey-driven layout swap + spell-check beep). UI is a tray
icon with on/off toggle.

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
| `Shift+Pause`| Convert the current selection (planned)               |

Hotkeys are configurable in `%AppData%\Linter\settings.json`.

## Project layout

```
src/Linter/
├── Application/      — tray icon + ApplicationContext that wires everything
├── Interop/          — P/Invoke signatures, structs, COM (spell-check)
├── Keyboard/         — global hook, SendInput, layout switching, ToUnicodeEx
├── Text/             — typed-key buffer + word-boundary classification
├── Services/         — layout converter, spell checker, sound notifier
└── Configuration/    — settings model + JSON store
```

Each file owns one small responsibility — this project is meant to be
read top-to-bottom by humans, not just compiled.

## License

MIT — see [LICENSE](LICENSE).
