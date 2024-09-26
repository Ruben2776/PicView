[Setup]
AppId={{F102E394-0FA6-4AEA-826D-9FE699115BAB}
AppName={param:appName|PicView}
AppVersion={param:version|1.0}
AppPublisher="Ruben2776"
AppPublisherURL=https://picview.org/
AppSupportURL=https://github.com/Ruben2776/PicView/issues
AppUpdatesURL=https://picview.org/download
DefaultDirName={sd}\PicView
DisableProgramGroupPage=yes
LicenseFile={param:appLicense|license.txt}
PrivilegesRequired=lowest
OutputDir={param:outputDir|/}
OutputBaseFilename={param:appName|PicView}-{param:version|1.0}
SetupIconFile={param:appIcon|icon.ico}
Compression=lzma
SolidCompression=yes
WizardStyle=modern
UninstallDisplayIcon={app}\{param:appExe|PicView.exe}
UninstallDisplayName={param:appName|PicView}
ChangesAssociations=yes
VersionInfoVersion={param:version|1.0}

[Files]
Source: "{param:outputDir|/}\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{autoprograms}\{param:appName|PicView}"; Filename: "{app}\{param:appExe|PicView.exe}"
Name: "{autodesktop}\{param:appName|PicView}"; Filename: "{app}\{param:appExe|PicView.exe}"; Tasks: desktopicon

[Run]
Filename: "{app}\{param:appExe|PicView.exe}"; Description: "{cm:LaunchProgram,{#StringChange(MyAppName, '&', '&&')}}"; Flags: nowait postinstall skipifsilent

#include 'uninstallPrev.iss'
#include 'registry.iss'
