# Nova Browser

A modern, customizable WinForms web browser built with .NET 8 and Microsoft Edge WebView2.

## Features

### Tab Management
- **Dynamic tab system** with visual tab headers showing favicons and page titles.
- **New Tab** button to quickly open additional tabs.
- **Close tabs** via an X button on each tab header.
- **Overflow handling** with a "More" button when tabs exceed available width.
- **Audio indicators** on tabs — a blinking speaker icon appears when a page is playing audio.

### Navigation
- **Back / Forward / Refresh / Home** toolbar buttons for standard navigation.
- **Go button** and **Enter key** support in the URL bar to navigate to typed addresses.
- **History tracking** per tab with forward/back navigation via WebView2.
- **SPA support** — the URL bar updates correctly on single-page apps (e.g., YouTube) that use `history.pushState`.

### URL Bar
- **Custom UrlComboBox** with an integrated dropdown showing address history.
- **Address history** is persisted across sessions and shown in the dropdown.
- **Right-click context menu** with:
  - **Copy** — copies the current URL to the clipboard.
  - **Paste** — pastes clipboard text into the URL bar.
  - **Paste and Go** — pastes and immediately navigates to the URL.

### Bookmarks
- **Save bookmarks** via the Bookmark toolbar button.
- **Bookmark bar** below the toolbar for quick access to saved sites.
- **Bookmarks stored** in a local `bookmarks.json` file.
- **Favicon icons** displayed next to each bookmark.
- **Remove bookmarks** directly from the dropdown or bookmark bar.

### Settings Panel
A two-pane settings interface with a left sidebar and right content area.

#### Appearance
- **Dark Mode toggle** — switches the entire browser between dark and light themes.
- **Theme Color picker** — choose from 8 preset accent colors that apply to the active tab, buttons, and UI highlights.

#### Default Browser
- **Register as default browser** — adds Nova Browser to the Windows registry so it appears in Windows Settings as a default browser option.
- **Status indicator** showing whether Nova Browser is currently set as the system default.

#### Home Page
- **Show Home button** toggle — show or hide the Home button on the main toolbar.
- **New tab page** option — opens a blank `about:blank` page when the Home button or New Tab is clicked.
- **Set custom site** option — specify a custom URL (e.g., `https://www.bing.com`) to load instead.
- **Custom URL text box** to enter your preferred home page address.

### Theming
- **Full dark/light mode support** across all UI elements:
  - Title bar, toolbar, status bar, tabs, settings panel, bookmarks bar, and dropdowns.
- **Accent color** applied to the active tab and highlighted controls.
- **Dynamic recoloring** when switching between dark and light modes or changing the accent color.

### Title Bar & Status Bar
- **Transparent/acrylic title bar** using Windows DWM APIs (Windows 11 style).
- **Transparent status bar** with a custom renderer so the window background shows through.
- **Website Loaded In** timer displayed on the status bar showing page load duration.

### Favicons
- **Automatic favicon fetching** from websites for tabs and bookmarks.
- **Fallback icon** uses the Nova Browser icon (`images/favicon.ico`) when a site has no favicon.
- **Favicon caching** for performance.

### New Tab Behavior
- Respects the **Home Page setting**:
  - If "New tab page" is selected, all new tabs open `about:blank`.
  - If "Set custom site" is selected, new tabs open the custom URL.

## Technical Details

- **Framework:** .NET 8 (Windows Desktop)
- **Web Engine:** Microsoft Edge WebView2
- **UI:** Windows Forms with custom owner-drawn controls
- **Data Storage:** JSON files for bookmarks and address history

## File Structure

- `Form1.cs` — Main application logic, tab management, theming, and event handlers.
- `Form1.Designer.cs` — UI layout and control definitions.
- `UrlComboBox.cs` — Custom ComboBox with history dropdown and right-click context menu support.
- `ToggleSwitch.cs` — Custom animated toggle switch control for settings.
- `images/favicon.ico` — Default application icon used as a fallback for missing website favicons.

## Requirements
Nova Browser — Version 1.0.0

- Windows 10 or later
- .NET 8 Desktop Runtime
- Microsoft Edge WebView2 Runtime

## License
The MIT License (MIT)
Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the “Software”), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED “AS IS”, WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.

## Build and Run

Build and Run the code from inside Visual Studio Code.
```
DotNet Run
```

- Publish the App for Windows

```Powershell
dotnet publish -c Release -r win-x64 --self-contained false -o publish
```

Files will be in the Publish folder.
Use Inno to generate a setup.exe
