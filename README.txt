KioskGuard Version 1
====================

Installation
------------
1. Extract the entire KioskGuard-v1 folder to a writable location such as Documents.
2. Keep KioskGuard.exe, the DLL files, ui folder, and media folder together.
3. Double-click KioskGuard.exe.

KioskGuard discovers applications separately on every computer. It scans that user's
Windows Start Menu shortcuts and locally installed game libraries when it starts. No
application paths, media paths, artwork cache, browser profile, or user data from the
developer's computer are included in this package.

Requirements
------------
- 64-bit Windows 10 or Windows 11
- Microsoft Edge WebView2 Runtime
- .NET Framework 4.7.2 or newer

WebView2 is normally already present on supported Windows installations. If the app
does not open, install the Microsoft Edge WebView2 Runtime from Microsoft.

Important
---------
- Windows may display a SmartScreen warning because this build is not code-signed.
- Do not run KioskGuard directly from inside the ZIP file.
- Custom settings and imported media are saved inside the extracted KioskGuard folder.
- Test the configured unlock hotkey before using the lock with other people.
