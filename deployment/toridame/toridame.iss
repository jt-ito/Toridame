; Inno Setup script. iscc /DAppVersion=... /DSourceDir=... /DOutDir=... /DIconFile=... toridame.iss
#define AppName "Toridame"
#define RunArgs "--datadir ""{localappdata}\Toridame\data"" --configdir ""{localappdata}\Toridame\config"" --cachedir ""{localappdata}\Toridame\cache"" --logdir ""{localappdata}\Toridame\log"" --webdir ""{app}\jellyfin-web"" --ffmpeg ""{app}\ffmpeg\ffmpeg.exe"""

[Setup]
AppId={{6D2F8C1A-4B7E-4E55-9C0A-3F6B1D2E7A90}
AppName={#AppName}
AppVersion={#AppVersion}
AppPublisher=jt-ito
AppPublisherURL=https://github.com/jt-ito/Toridame
DefaultDirName={autopf}\{#AppName}
DefaultGroupName={#AppName}
ArchitecturesInstallIn64BitMode=x64compatible
ArchitecturesAllowed=x64compatible
PrivilegesRequiredOverridesAllowed=dialog
OutputDir={#OutDir}
OutputBaseFilename=toridame-{#AppVersion}-win-x64-setup
SetupIconFile={#IconFile}
UninstallDisplayIcon={app}\toridame.ico
Compression=lzma2
SolidCompression=yes

[Files]
Source: "{#SourceDir}\*"; DestDir: "{app}"; Excludes: "run.cmd"; Flags: recursesubdirs createallsubdirs ignoreversion
Source: "{#IconFile}"; DestDir: "{app}"; DestName: "toridame.ico"; Flags: ignoreversion

[Tasks]
Name: "desktopicon"; Description: "Create a desktop shortcut"; Flags: unchecked

[Icons]
Name: "{group}\{#AppName}"; Filename: "{app}\jellyfin\jellyfin.exe"; Parameters: "{#RunArgs}"; IconFilename: "{app}\toridame.ico"
Name: "{group}\Open Toridame in browser"; Filename: "http://localhost:8096"
Name: "{autodesktop}\{#AppName}"; Filename: "{app}\jellyfin\jellyfin.exe"; Parameters: "{#RunArgs}"; IconFilename: "{app}\toridame.ico"; Tasks: desktopicon
