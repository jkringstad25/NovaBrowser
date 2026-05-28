#define AppName "Nova Browser"
#define AppVersion "1.0.0"
#define AppPublisher "Nova Browser"
#define AppExeName "NovaBrowser.exe"
#define AppDirName "Nova Browser"
#define DefaultDirName "{pf}\Nova Browser"
#define DefaultGroupName "Nova Browser"
#define AppComments "A custom browser built with WebView2."

[Setup]
AppName={#AppName}
AppVersion={#AppVersion}
AppPublisher={#AppPublisher}
DefaultDirName={#DefaultDirName}
DefaultGroupName={#DefaultGroupName}
OutputBaseFilename=NovaBrowserSetup
Compression=lzma
SolidCompression=yes
ArchitecturesInstallIn64BitMode=x64
DisableProgramGroupPage=yes
WizardStyle=modern
DisableStartupPrompt=yes

[Tasks]
Name: "desktopicon"; Description: "Create a &desktop icon"; GroupDescription: "Additional icons:"; Flags: unchecked

[Files]
Source: "Output\*"; DestDir: "{app}"; Flags: recursesubdirs createallsubdirs ignoreversion

[Icons]
Name: "{group}\{#AppName}"; Filename: "{app}\{#AppExeName}"
Name: "{userdesktop}\{#AppName}"; Filename: "{app}\{#AppExeName}"; Tasks: desktopicon

[Run]
Filename: "{app}\{#AppExeName}"; Description: "Launch {#AppName}"; Flags: nowait postinstall skipifsilent
