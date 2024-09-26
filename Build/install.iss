#define MyAppName "PicView"
#define MyAppVersion "{#MyAppVersion}"
#define MyAppPublisher "Ruben2776"
#define MyAppExeName "{#MyAppExeName}"
#define AppIcon "{#MyAppIcon}"
#define LicenseFile "{#MyAppLicenseFile}"

[Setup]
AppId={{F102E394-0FA6-4AEA-826D-9FE699115BAB}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppPublisher={#MyAppPublisher}
AppPublisherURL=https://picview.org/
AppSupportURL=https://github.com/Ruben2776/PicView/issues
AppUpdatesURL=https://picview.org/download
DefaultDirName={sd}\PicView
DisableProgramGroupPage=yes
LicenseFile={#LicenseFile}
PrivilegesRequired=lowest
OutputDir={#MyAppOutputDir}
OutputBaseFilename={#MyAppName}-{#MyAppVersion}
SetupIconFile={#AppIcon}
Compression=lzma
SolidCompression=yes
WizardStyle=modern
UninstallDisplayIcon={app}\{#MyAppExeName}
UninstallDisplayName={#MyAppName}
ChangesAssociations=yes
VersionInfoVersion={#MyAppVersion}

[Files]
Source: "{#MyAppOutputDir}\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{autoprograms}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"
Name: "{autodesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; Tasks: desktopicon

[Run]
Filename: "{app}\{#MyAppExeName}"; Description: "{cm:LaunchProgram,{#StringChange(MyAppName, '&', '&&')}}"; Flags: nowait postinstall skipifsilent

#include 'uninstallPrev.iss'
#include 'registry.iss'
