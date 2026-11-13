#define MyAppName "Tomchei Torah - Daat Aharon"
#define MyAppVersion "3.2.0"
#define MyAppExeName "ClothingOrders.exe"

[Setup]
AppId={{8D8B9D57-9D56-4E58-9C7E-5A4D9E1A6C31}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppPublisher=תומכי תורה - ישיבת דעת אהרן
DefaultDirName={localappdata}\Programs\ClothingOrders
DefaultGroupName={#MyAppName}
DisableProgramGroupPage=yes
PrivilegesRequired=lowest
ArchitecturesAllowed=x64
ArchitecturesInstallIn64BitMode=x64
OutputDir=..\artifacts
OutputBaseFilename=ClothingOrders-Setup
Compression=lzma2
SolidCompression=yes
WizardStyle=modern
UninstallDisplayName={#MyAppName}

[Languages]
Name: "hebrew"; MessagesFile: "compiler:Languages\Hebrew.isl"

[Files]
Source: "..\publish\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs
Source: "WebView2Setup.exe"; DestDir: "{tmp}"; Flags: deleteafterinstall

[Icons]
Name: "{autoprograms}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"
Name: "{autodesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; Tasks: desktopicon

[Tasks]
Name: "desktopicon"; Description: "יצירת קיצור דרך בשולחן העבודה"; GroupDescription: "קיצורי דרך:"

[Run]
Filename: "{tmp}\WebView2Setup.exe"; Parameters: "/silent /install"; StatusMsg: "מתקין רכיב תצוגת האינטרנט של Microsoft..."; Flags: runhidden waituntilterminated
Filename: "{app}\{#MyAppExeName}"; Description: "הפעלת התוכנה"; Flags: nowait postinstall skipifsilent
