# KioskGuard

<p align="center">
  <img src="KioskGuard-logo.png" alt="KioskGuard logo" width="220">
</p>

KioskGuard is a portable Windows application for running one selected app on an active display while covering the other connected displays. It provides a full-screen launcher, automatic application discovery, customizable blocker screens, and a configurable hotkey for returning to the launcher.

It is designed for kiosks, demonstrations, events, shared displays, dedicated workstations, and gaming setups where one application should remain in focus during a session.

## Features

- Discovers installed Steam games from the main Steam library and additional library folders.
- Discovers applications from the current user and system-wide Windows Start Menus.
- Organizes discovered programs into Games, Applications, Browsers, Media, and Productivity categories.
- Searches and filters the local application library from a full-screen launcher.
- Launches the selected application on a chosen display and keeps its window positioned there.
- Covers every other connected display with an individual full-screen blocker.
- Blocks common task-switching shortcuts while a protected session is active.
- Returns to the launcher with a configurable modifier-key hotkey.
- Supports bundled, fixed, custom, or disabled startup video sequences.
- Includes Ocean, Midnight, and Aurora blocker styles, plus custom image or video media.
- Customizes blocker text, text color, media fit, and launcher theme colors.
- Imports replacement artwork for individual applications.
- Saves settings and imported media locally beside the application.

## Requirements

- 64-bit Windows 10 or Windows 11
- Microsoft Edge WebView2 Runtime
- .NET Framework 4.7.2 or newer
- Write access to the extracted KioskGuard folder

WebView2 is normally included with supported Windows installations. If KioskGuard opens without an interface or does not start, install the current [Microsoft Edge WebView2 Runtime](https://developer.microsoft.com/microsoft-edge/webview2/).

## Installation

1. Download the repository as a ZIP file from GitHub and extract it completely.
2. Move the extracted folder to a writable location, such as Documents.
3. Keep `KioskGuard.exe`, the DLL files, and the `ui` and `media` folders together.
4. Double-click `KioskGuard.exe`.

KioskGuard is portable and does not use an installer. Do not run it from inside the ZIP archive.

Windows SmartScreen may display a warning because the current executable is not code-signed. Review the repository contents and proceed only if you trust the download source.

## Quick start

1. Open **Settings** and choose the display that should run the selected application.
2. Open **Controls**, set an exit hotkey, and remember it before starting a session.
3. Adjust the startup sequence, protected-display appearance, and theme if desired.
4. Select an application from the library and choose **Launch**, or double-click its card.
5. Use the configured exit hotkey to end the protected session and return to KioskGuard.

The default configuration included in this repository uses `Ctrl + ArrowUp` as the exit hotkey. Always test the configured combination before using KioskGuard with other people.

## How a protected session works

When an application is launched, KioskGuard:

1. Starts the selected executable, or attaches to it if the same executable is already running.
2. Moves and sizes its main window to the working area of the selected display.
3. Places a blocker window on every other detected display.
4. Keeps the selected application in the foreground while the session is active.
5. Intercepts the Windows keys and common switching combinations such as `Alt + Tab`, `Alt + Esc`, `Ctrl + Esc`, and `Alt + F4`.
6. Restores the KioskGuard launcher when the configured exit hotkey is pressed.

If the selected application closes unexpectedly, KioskGuard covers all displays and asks the user to use the exit hotkey.

## Application discovery

KioskGuard builds its library locally each time it starts or the library is refreshed.

### Steam

It reads the current user's Steam installation path and `libraryfolders.vdf`, then scans installed app manifests in all detected Steam libraries. Available Steam artwork is copied into the local artwork cache.

### Windows applications

It scans shortcuts in the current user's Start Menu and the shared Windows Start Menu. Only shortcuts that resolve to an existing `.exe` file are included.

Applications are deduplicated by display name. KioskGuard does not upload the discovered paths or library information.

## Settings

### Experience

- **Startup sequence:** Play a random bundled movie, always use one bundled movie, import a custom movie, or disable the sequence.
- **Startup media fit:** Stretch to fill, fill and crop, or fit with bars.
- **Protected-display appearance:** Choose Ocean, Midnight, Aurora, or imported custom media.
- **Blocker media fit:** Stretch to fill, fill and crop, or fit with bars.
- **Protected-display message:** Show, hide, and edit the text displayed above the blocker.
- **Blocker text color:** Choose the color used by the protected-display message.

Custom startup videos support MP4, WMV, AVI, and MOV. Custom blocker media supports those video formats plus PNG, JPG, JPEG, and GIF.

### Displays

Choose the active display by its Windows device name and resolution. Every other detected display receives a separate blocker window during a session.

### Controls

Click the exit-hotkey button and press a key combination containing `Ctrl`, `Alt`, or `Shift` plus another key. The combination is saved immediately.

### Theme

Customize the launcher background, surface, accent, and text colors. Theme changes apply immediately and persist in `settings.txt`.

### Application artwork

Open an application's details and choose **Change artwork**. KioskGuard accepts PNG, JPG, JPEG, BMP, and WebP files and stores the selected artwork locally.

## Local data and privacy

KioskGuard runs locally. It does not require an account, analytics service, or cloud connection. It reads installed-program information from Steam libraries and Windows Start Menu shortcuts to create the launcher catalog.

The following data is created or updated inside the KioskGuard folder:

- `settings.txt` — saved preferences and hotkey configuration
- `media/*-custom.*` — imported startup or blocker media
- `ui/overrides/` — imported application artwork
- `ui/cache/` — generated artwork cache
- `WebView2Data/`, `WebView2SplashData/`, and `WebView2BlockerData/` — local WebView2 browser profiles
- `KioskGuard-crash.log` and `KioskGuard-video-error.log` — diagnostic files created after relevant failures

Browser profiles, artwork caches, logs, backups, and temporary files are excluded from version control. Imported custom media and artwork remain local unless deliberately added to a commit.

## File layout

| Path | Purpose |
| --- | --- |
| `KioskGuard.exe` | Portable Windows application |
| `Microsoft.Web.WebView2.*.dll` | WebView2 .NET integration libraries |
| `WebView2Loader.dll` | Native WebView2 loader |
| `ui/` | Launcher, blocker, and startup interface assets |
| `media/` | Bundled startup videos and imported custom media |
| `settings.txt` | Local configuration |
| `KioskGuard.decompiled.cs` | C# source snapshot provided for inspection and reference |
| `README.txt` | Short offline installation notes |

## Troubleshooting

### KioskGuard does not open

- Confirm that the folder was fully extracted and is writable.
- Keep the executable, DLL files, `ui`, and `media` folders together.
- Install or repair the Microsoft Edge WebView2 Runtime.
- Check `KioskGuard-crash.log` or `KioskGuard-video-error.log` if either file exists.

### An application is missing

- Confirm that it has a valid shortcut in the user or shared Windows Start Menu, or that it is installed in a detected Steam library.
- Use the refresh button after installing or moving an application.
- Confirm that the shortcut resolves directly to an existing `.exe` file.

### Artwork is missing or incorrect

Open the application's details and import replacement artwork. Steam artwork is available only when it exists in Steam's local library cache.

### The wrong display is active

Open **Settings → Displays** and select the correct device. If display numbering changed after connecting or disconnecting a monitor, select it again.

### A session cannot be exited

Try the exit hotkey shown in **Settings → Controls**. If necessary, use Windows' secure attention screen (`Ctrl + Alt + Delete`) to recover control, then close KioskGuard or sign out.

## Security scope

KioskGuard is intended to control the presentation and interaction flow of a local session. It blocks several common switching shortcuts, but it is not a hardened security boundary and does not replace Windows Assigned Access, Group Policy, a restricted user account, or other operating-system kiosk controls. Use Windows security features when the device must resist an untrusted or technically capable user.

## License

KioskGuard is available under the [MIT License](LICENSE). You may use, copy, modify, merge, publish, distribute, sublicense, and sell copies subject to the license terms.

Copyright © 2026 Sebastian Brown.
