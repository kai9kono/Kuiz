#define MyAppName "Kuiz"
#ifndef MyAppVersion
  #error MyAppVersion must be provided by Build-InnoSetup.ps1
#endif
#ifndef PublishDir
  #error PublishDir must be provided by Build-InnoSetup.ps1
#endif
[Setup]
#ifdef VerificationBuild
AppId={{2FDC2962-087C-42BF-B62C-3147B11DA44A}
#else
AppId={{A8C5D9E2-4B3F-4E1A-9D2C-7F8A3B6C5D9E}
#endif
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppPublisher=Kai Kono
AppPublisherURL=https://github.com/kai9kono/Kuiz
AppSupportURL=https://github.com/kai9kono/Kuiz/issues
DefaultDirName={autopf}\Kuiz
DefaultGroupName=Kuiz
DisableProgramGroupPage=yes
LicenseFile=..\LICENSE
#ifdef VerificationBuild
OutputDir=..\artifacts
OutputBaseFilename=KuizVerify-{#MyAppVersion}
#else
OutputDir=.
OutputBaseFilename=KuizSetup-{#MyAppVersion}
#endif
Compression=lzma2/max
SolidCompression=yes
WizardStyle=modern
SetupIconFile=..\Resources\icon\icon.ico
UninstallDisplayIcon={app}\Kuiz.exe
MinVersion=10.0.19041
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
PrivilegesRequired=lowest
PrivilegesRequiredOverridesAllowed=dialog
CloseApplications=yes
[Languages]
Name: "japanese"; MessagesFile: "compiler:Languages\Japanese.isl"
[Tasks]
Name: "desktopicon"; Description: "デスクトップにショートカットを作成(&D)"; Flags: unchecked
[Files]
Source: "{#PublishDir}\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs
Source: "..\LICENSE"; DestDir: "{app}"; Flags: ignoreversion
[Icons]
Name: "{group}\Kuiz"; Filename: "{app}\Kuiz.exe"
Name: "{autodesktop}\Kuiz"; Filename: "{app}\Kuiz.exe"; Tasks: desktopicon
[Run]
Filename: "{app}\Kuiz.exe"; Description: "Kuizを起動する"; Flags: nowait postinstall skipifsilent
