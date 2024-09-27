#define MyAppName "PicView"
#define MyAppExeName "PicView.exe"

[Setup]
AppId={{F102E394-0FA6-4AEA-826D-9FE699115BAB}
AppName=PicView
AppVersion={#MyAppVersion}
AppPublisher=Ruben2776
AppPublisherURL=https://picview.org/
AppSupportURL=https://github.com/Ruben2776/PicView/issues
AppUpdatesURL=https://picview.org/download
DefaultDirName={sd}\PicView
DisableProgramGroupPage=yes
LicenseFile={#LicenseFile}
OutputDir={#MyAppOutputDir}
PrivilegesRequired=lowest
OutputBaseFilename={#MyAppName}-{#MyAppVersion}
SetupIconFile={#AppIcon}
Compression=lzma
SolidCompression=yes
WizardStyle=modern
UninstallDisplayIcon={app}\{#MyAppExeName}
UninstallDisplayName={#MyAppExeName}
ChangesAssociations=yes
VersionInfoVersion={#MyAppVersion}

[Messages]
SetupWindowTitle=Setup - {#MyAppName} v{#MyAppVersion}
SetupAppTitle=Setup - {#MyAppName} v{#MyAppVersion}

[Languages]
Name: "en"; MessagesFile: "compiler:Default.isl"
Name: "de"; MessagesFile: "compiler:Languages\German.isl"
Name: "da"; MessagesFile: "compiler:Languages\Danish.isl"
Name: "es"; MessagesFile: "compiler:Languages\Spanish.isl"
Name: "fr"; MessagesFile: "compiler:Languages\French.isl"
Name: "ru"; MessagesFile: "compiler:Languages\Russian.isl"
Name: "ko"; MessagesFile: "compiler:Languages\Korean.isl"
Name: "pl"; MessagesFile: "compiler:Languages\Polish.isl"

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked
Name: "openwith"; Description: "Open with {#MyAppName}"; GroupDescription: "Context menu:"; Flags: unchecked
Name: "browsefolder"; Description: "Browse folder with {#MyAppName}"; GroupDescription: "Context menu:"; Flags: unchecked

[Files]
Source: "{#MyFileSource}\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{autoprograms}\PicView"; Filename: "{app}\PicView.exe"
Name: "{autodesktop}\PicView"; Filename: "{app}\PicView.exe"; Tasks: desktopicon

[Run]
Filename: "{app}\PicView.exe"; Description: "{cm:LaunchProgram,PicView}"; Flags: nowait postinstall skipifsilent

#include 'uninstallPrev.iss'
#include 'registry.iss'
