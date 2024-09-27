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

[Files]
Source: "{#MyAppOutputDir}\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{autoprograms}\PicView"; Filename: "{app}\PicView.exe"
Name: "{autodesktop}\PicView"; Filename: "{app}\PicView.exe"; Tasks: desktopicon

[Run]
Filename: "{app}\PicView.exe"; Description: "{cm:LaunchProgram,PicView}"; Flags: nowait postinstall skipifsilent

#include 'uninstallPrev.iss'
#include 'registry.iss'
