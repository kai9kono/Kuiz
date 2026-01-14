; Kuiz - オンライン早押しクイズゲーム
; Inno Setup インストーラースクリプト

#define MyAppName "Kuiz"
#define MyAppVersion "1.0.0"
#define MyAppPublisher "Kai Kono"
#define MyAppURL "https://github.com/kai9kono/Kuiz"
#define MyAppExeName "Kuiz.exe"

[Setup]
; アプリケーション情報
AppId={{A8C5D9E2-4B3F-4E1A-9D2C-7F8A3B6C5D9E}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppPublisher={#MyAppPublisher}
AppPublisherURL={#MyAppURL}
AppSupportURL={#MyAppURL}/issues
AppUpdatesURL={#MyAppURL}/releases
DefaultDirName={autopf}\{#MyAppName}
DefaultGroupName={#MyAppName}
DisableProgramGroupPage=yes
LicenseFile=LICENSE
OutputDir=installer
OutputBaseFilename=KuizSetup-{#MyAppVersion}
Compression=lzma2/max
SolidCompression=yes
WizardStyle=modern
SetupIconFile=Resources\icon\icon.ico
UninstallDisplayIcon={app}\{#MyAppExeName}

; 最小Windowsバージョン
MinVersion=10.0.19041

; アーキテクチャ
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible

; 特権
PrivilegesRequired=lowest
PrivilegesRequiredOverridesAllowed=dialog

[Languages]
Name: "japanese"; MessagesFile: "compiler:Languages\Japanese.isl"

[Tasks]
Name: "desktopicon"; Description: "デスクトップにショートカットを作成(&D)"; GroupDescription: "追加アイコン:"; Flags: unchecked
Name: "quicklaunchicon"; Description: "クイック起動にショートカットを作成(&Q)"; GroupDescription: "追加アイコン:"; Flags: unchecked; OnlyBelowVersion: 6.1; Check: not IsAdminInstallMode

[Files]
; アプリケーション本体
Source: "bin\Release\net10.0-windows\publish\win-x64\{#MyAppExeName}"; DestDir: "{app}"; Flags: ignoreversion
Source: "bin\Release\net10.0-windows\publish\win-x64\*.dll"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs
Source: "bin\Release\net10.0-windows\publish\win-x64\*.json"; DestDir: "{app}"; Flags: ignoreversion

; リソースファイル
Source: "bin\Release\net10.0-windows\publish\win-x64\Resources\*"; DestDir: "{app}\Resources"; Flags: ignoreversion recursesubdirs

; ドキュメント
Source: "README.md"; DestDir: "{app}"; Flags: ignoreversion isreadme
Source: "LICENSE"; DestDir: "{app}"; Flags: ignoreversion
Source: "インストール手順.txt"; DestDir: "{app}"; Flags: ignoreversion

[Icons]
Name: "{group}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"
Name: "{group}\{cm:UninstallProgram,{#MyAppName}}"; Filename: "{uninstallexe}"
Name: "{autodesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; Tasks: desktopicon
Name: "{userappdata}\Microsoft\Internet Explorer\Quick Launch\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; Tasks: quicklaunchicon

[Run]
Filename: "{app}\{#MyAppExeName}"; Description: "{cm:LaunchProgram,{#StringChange(MyAppName, '&', '&&')}}"; Flags: nowait postinstall skipifsilent

[Code]
// .NET 10 Runtimeチェック
function IsDotNet10Installed: Boolean;
var
  ResultCode: Integer;
  Output: AnsiString;
begin
  Result := False;
  if Exec('dotnet', '--list-runtimes', '', SW_HIDE, ewWaitUntilTerminated, ResultCode) then
  begin
    Result := (ResultCode = 0);
  end;
end;

function InitializeSetup(): Boolean;
var
  ErrorCode: Integer;
begin
  Result := True;
  
  // .NET 10 Runtimeチェック
  if not IsDotNet10Installed then
  begin
    if MsgBox('.NET 10 Desktop Runtimeが必要です。' + #13#10 + 
              'インストールページを開きますか？' + #13#10#13#10 +
              '※ インストール後、再度このセットアップを実行してください。', 
              mbConfirmation, MB_YESNO) = IDYES then
    begin
      ShellExec('open', 'https://dotnet.microsoft.com/download/dotnet/10.0', '', '', SW_SHOW, ewNoWait, ErrorCode);
    end;
    Result := False;
  end;
end;

procedure CurStepChanged(CurStep: TSetupStep);
begin
  if CurStep = ssPostInstall then
  begin
    // 初回起動時の設定フォルダを作成
    // （実際にはアプリ起動時に自動作成されるので不要）
  end;
end;

function InitializeUninstall(): Boolean;
begin
  Result := True;
  if MsgBox('Kuizをアンインストールしますか？' + #13#10 + 
            '※ ユーザーデータ（プロフィール・ログ）は削除されません。', 
            mbConfirmation, MB_YESNO) = IDYES then
  begin
    Result := True;
  end
  else
  begin
    Result := False;
  end;
end;
