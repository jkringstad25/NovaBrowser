# Nova Browser Installer

This folder contains the installer definition and packaging helper for Nova Browser.

## Build instructions

1. Install Inno Setup if you want a Windows installer executable.
   - https://jrsoftware.org/isinfo.php

2. Open PowerShell in the `Installer` folder.

3. Run:

   ```powershell
   .\BuildInstaller.ps1
   ```

4. If `ISCC.exe` is available on `PATH`, the script will compile `NovaBrowserSetup.exe`.
   Otherwise, the published application files are available in `Installer\Output`.

## Output

- `Installer\Output` - published application folder with `NovaBrowser.exe` and dependencies.
- `Installer\NovaBrowserInstaller.iss` - Inno Setup script for building a setup executable.
- `Installer\NovaBrowserSetup.exe` - generated installer, if Inno Setup is installed.

## Notes

- The installer installs the application to `Program Files\Nova Browser` by default.
- A desktop shortcut is optionally created during install.
- The setup runs the browser after installation.
