<p align="center">
  <img src="docs/icon.png" width="160" height="160" alt="First Fox Screenshot (ffshot) icon">
</p>

# First Fox Screenshot (ffshot)

[日本語版 README はこちら / Japanese README](README.md)

[![Latest release](https://img.shields.io/github/v/release/uehatsu/ffshot?label=release)](https://github.com/uehatsu/ffshot/releases/latest)
[![CI](https://github.com/uehatsu/ffshot/actions/workflows/ci.yml/badge.svg)](https://github.com/uehatsu/ffshot/actions/workflows/ci.yml)

First Fox Screenshot (ffshot) is a Windows app that lives in the system tray and saves PNG screenshots when you press a hotkey.

- Separate hotkeys for full screen and for the active window
- Two capture backends: "Normal (GDI)" and "DirectX (Direct3D / DXGI Desktop Duplication)"
  - DirectX full-screen (borderless) apps that come out black with GDI can be captured with the Direct3D backend
  - Where Direct3D capture is unavailable (Remote Desktop, etc.) it falls back to GDI automatically
- Settings are stored in `%APPDATA%\ffshot\settings.json`

## Requirements

- Windows 10 1809 or later / Windows 11 (64-bit)
- The release build bundles the .NET runtime, so nothing else needs to be installed

## Installation

There is no installer. Extract the zip and run the exe.

1. Open [Releases](https://github.com/uehatsu/ffshot/releases/latest) and download `ffshot-<version>-win-x64.zip` from Assets.
   For example, v0.1.0 is `ffshot-0.1.0-win-x64.zip`.
2. Right-click the zip and choose "Extract All..." to a folder of your choice.
   For example: `C:\Users\<you>\Apps\ffshot`
   Avoid `Program Files`, since writing there requires administrator rights.
3. Double-click the extracted `ffshot.exe`.
   The exe is not code-signed, so SmartScreen may show "Windows protected your PC" the first time. Click "More info" and then "Run anyway".
4. A fox icon in the system tray means it is running. By default `Ctrl+Shift+F12` captures the full screen, `Ctrl+Shift+F11` captures the active window, and files go to `Pictures\ffshot`.

### Verifying the zip (optional)

Each release also ships `SHA256SUMS.txt`. Compare with PowerShell:

```powershell
Get-FileHash .\ffshot-0.1.0-win-x64.zip -Algorithm SHA256
Get-Content .\SHA256SUMS.txt
```

### Updating

Extract the new zip and overwrite `ffshot.exe`. Stop the app first with "Exit" in the tray menu. Settings live in `%APPDATA%\ffshot\settings.json` and are kept.

### Uninstalling

1. If "Start automatically at Windows logon" is on, turn it off in the settings dialog and click OK. This removes the Run key or Task Scheduler entry.
2. Choose "Exit" in the tray menu and delete the folder you extracted.
3. To remove settings as well, delete the `%APPDATA%\ffshot` folder.

## Development

### Build and run

Requires the .NET 10 SDK.

```powershell
dotnet build ffshot.sln
dotnet run --project src/FFShot
dotnet test ffshot.sln
```

To produce a single self-contained exe for distribution (about 55 MB, .NET runtime included):

```powershell
dotnet publish src/FFShot -c Release -r win-x64 -o publish
```

From WSL, call the Windows SDK as `"/mnt/c/Program Files/dotnet/dotnet.exe"`.

### CI / Releases

- On every push and pull request, GitHub Actions (`ci.yml`) builds, tests, publishes, and attaches the exe as a workflow artifact.
- Pushing a tag like `v1.2.3` runs `release.yml`, which zips the self-contained exe and attaches it to a GitHub Release together with `SHA256SUMS.txt`. Release notes are generated from the commit history.

```powershell
git tag -a v0.2.0 -m "v0.2.0"
git push origin v0.2.0
```

Tests that capture the real screen (`Category=Screen`) do not work on the runner's virtual display and are excluded in CI. Locally, `dotnet test` runs everything.

## Usage

When started, a fox icon appears in the tray. Double-click it, or right-click and choose "Settings...", to change the following.

| Setting | Default |
|---|---|
| Full-screen hotkey | `Ctrl+Shift+F12` |
| Active-window hotkey | `Ctrl+Shift+F11` |
| Capture backend | Normal (GDI) |
| Save folder | `%USERPROFILE%\Pictures\ffshot` |
| File name pattern | `ffshot_{yyyyMMdd_HHmmss}` |
| Combine all monitors for full-screen capture | Off (only the monitor with the active window) |
| Include the mouse cursor | Off |
| Show a notification after saving | On |
| Start automatically at Windows logon | Off |

Inside `{ }` in the file name pattern you can use .NET date/time format strings. `{target}` expands to `full` or `window`. If a file with the same name exists, `_1`, `_2`, ... is appended.

In the hotkey box, the key combination you press is taken as-is. Backspace clears it. If the hotkey cannot be registered because another app already uses it, a tray notification tells you.

## Capturing apps that run as administrator (games, etc.)

Because of Windows UIPI, hotkeys registered by a non-elevated app are not delivered while an elevated window is in the foreground. To capture apps that start with `requireAdministrator`, such as PlayOnline Viewer, use "Restart as administrator" in the tray menu (one UAC prompt).

Where autostart is registered depends on the privilege level.

| Privilege of First Fox Screenshot (ffshot) | Registered in | Started at logon as |
|---|---|---|
| Normal user | `HKCU\...\Run` | Normal user |
| Administrator | Task Scheduler (task name `ffshot`, highest privileges) | Administrator, no UAC prompt |

To keep it resident with administrator rights, choose "Restart as administrator" first, then turn on "Start automatically at Windows logon" in the settings.

## Layout

```
src/FFShot/
  App/        TrayApplicationContext (tray, wiring), CaptureService, Elevation,
              StartupManager (Run key / Task Scheduler selection)
  Hotkeys/    HotkeyBinding (string conversion), HotkeyManager (RegisterHotKey)
  Capture/    ICaptureBackend, GdiCaptureBackend, DesktopDuplicationBackend, WindowInfo, CursorOverlay
  Output/     FileNamer, PngWriter
  Settings/   AppSettings, SettingsStore
  UI/         SettingsForm, HotkeyTextBox
tests/FFShot.Tests/   xunit (includes tests that capture the real screen, so run on Windows)
```

## Known limitations

- The Direct3D backend does not support HDR (R16G16B16A16_FLOAT) or rotated displays. In those cases it falls back to GDI.
- Hidden parts of windows cannot be captured; only what is visible on screen is cropped.
