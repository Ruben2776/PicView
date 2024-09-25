#define MyAppName "PicView"
#define MyAppVersion "{#MyAppVersion}"
#define MyAppPublisher "Ruben2776"
#define MyAppURL "https://picview.org/"
#define MyAppExeName "{#MyAppExeName}"
#define AppIcon "{#MyAppIcon}"
#define LicenseFile "{#MyAppLicenseFile}"

[Setup]
AppId={{F102E394-0FA6-4AEA-826D-9FE699115BAB}
AppName={#MyAppName}
AppVersion=3.0
AppPublisher={#MyAppPublisher}
AppPublisherURL={#MyAppURL}
AppSupportURL=https://github.com/Ruben2776/PicView/issues
AppUpdatesURL={#MyAppURL}
DefaultDirName={sd}\PicView
DisableProgramGroupPage=yes
LicenseFile={#LicenseFile}
PrivilegesRequired=lowest
OutputDir={#MyAppOutputDir}
OutputBaseFilename={#MyAppName}
SetupIconFile={#AppIcon}
Compression=lzma
SolidCompression=yes
WizardStyle=modern
UninstallDisplayIcon={app}\{#MyAppExeName}
UninstallDisplayName={#MyAppName}-3.0
ChangesAssociations=yes
VersionInfoVersion=3.0.0.3

[Files]
Source: "{#MyAppOutputDir}\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{autoprograms}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"
Name: "{autodesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; Tasks: desktopicon

[Run]
Filename: "{app}\{#MyAppExeName}"; Description: "{cm:LaunchProgram,{#StringChange(MyAppName, '&', '&&')}}"; Flags: nowait postinstall skipifsilent

#include 'uninstallPrev.iss'
#include 'registry.iss'
