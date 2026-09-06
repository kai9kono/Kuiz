; Kuiz - オンライン早押しクイズゲーム
; Inno Setup インストーラースクリプト
;
; ?? 重要: このファイルを直接コンパイルしないでください！
; 
; ?? インストーラーを作成するには:
;    1. PowerShellを開く
;    2. プロジェクトルートで以下を実行:
;       .\Installer\Build-InnoSetup.ps1
;    
;    このスクリプトは自動的に以下を行います:
;    - 最新版をビルド（古いビルドの場合は確認）
;    - publish\win-x64 フォルダに出力
;    - Inno Setupでインストーラーを作成
;    - installer\KuizSetup-1.0.0.exe を生成
;
; ?? オプション:
;    -SkipBuild        : ビルドをスキップ（既存のビルドを使用）
;    -Configuration    : Debug または Release（デフォルト: Release）
;
; 例:
;    .\Installer\Build-InnoSetup.ps1
;    .\Installer\Build-InnoSetup.ps1 -SkipBuild
;    .\Installer\Build-InnoSetup.ps1 -Configuration Debug
;
; ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

#define MyAppName "Kuiz"
#define MyAppVersion "1.0.2"
#define MyAppPublisher "Kai Kono"
#define MyAppURL "https://github.com/kai9kono/Kuiz"
#define MyAppExeName "Kuiz.exe"
#define DotNetRuntimeUrl "https://aka.ms/dotnet/10.0/windowsdesktop-runtime-win-x64.exe"


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
LicenseFile=..\LICENSE
OutputDir=..\installer
OutputBaseFilename=KuizSetup-{#MyAppVersion}
Compression=lzma2/max
SolidCompression=yes
WizardStyle=modern
SetupIconFile=..\Resources\icon\icon.ico
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
Name: "quicklaunchicon"; Description: "クイックスタートにショートカットを作成(&Q)"; GroupDescription: "追加アイコン:"; Flags: unchecked; OnlyBelowVersion: 6.1; Check: not IsAdminInstallMode

[Files]
; アプリケーション本体
Source: "..\bin\Release\net10.0-windows\win-x64\publish\{#MyAppExeName}"; DestDir: "{app}"; Flags: ignoreversion
Source: "..\bin\Release\net10.0-windows\win-x64\publish\*.dll"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs
Source: "..\bin\Release\net10.0-windows\win-x64\publish\*.json"; DestDir: "{app}"; Flags: ignoreversion

; Hosted API and SignalR endpoints for each installed user.
Source: "config.json"; DestDir: "{userappdata}\Kuiz"; DestName: "config.json"; Flags: ignoreversion

; リソースファイル
Source: "..\Resources\*"; DestDir: "{app}\Resources"; Flags: ignoreversion recursesubdirs

; ドキュメント
Source: "..\LICENSE"; DestDir: "{app}"; Flags: ignoreversion

; .NET 10 Desktop Runtime インストーラー（同梱）
; 注意: 事前にダウンロードして Installer\Dependencies\ に配置してください
; ダウンロードURL: https://aka.ms/dotnet/10.0/windowsdesktop-runtime-win-x64.exe
Source: "Dependencies\windowsdesktop-runtime-10-win-x64.exe"; DestDir: "{tmp}"; Flags: deleteafterinstall; Check: not IsDotNet10Installed

[Icons]
Name: "{group}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"
Name: "{group}\{cm:UninstallProgram,{#MyAppName}}"; Filename: "{uninstallexe}"
Name: "{autodesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; Tasks: desktopicon
Name: "{userappdata}\Microsoft\Internet Explorer\Quick Launch\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; Tasks: quicklaunchicon

[Run]
; .NET 10 Runtime インストール（必要な場合のみ）
Filename: "{tmp}\windowsdesktop-runtime-10-win-x64.exe"; Parameters: "/quiet /norestart"; StatusMsg: ".NET 10 Desktop Runtimeをインストールしています..."; Flags: waituntilterminated; Check: not IsDotNet10Installed

; アプリケーション起動
Filename: "{app}\{#MyAppExeName}"; Description: "{cm:LaunchProgram,{#StringChange(MyAppName, '&', '&&')}}"; Flags: nowait postinstall skipifsilent

[Code]
// .NET 10 Runtime チェック（より正確な検出）
function IsDotNet10Installed: Boolean;
var
  ResultCode: Integer;
  TempFile: String;
  Lines: TArrayOfString;
  I: Integer;
begin
  Result := False;
  
  // dotnetコマンドで確認
  TempFile := ExpandConstant('{tmp}\dotnet-check.txt');
  
  if Exec('cmd.exe', '/c dotnet --list-runtimes > "' + TempFile + '" 2>&1', '', SW_HIDE, ewWaitUntilTerminated, ResultCode) then
  begin
    if LoadStringsFromFile(TempFile, Lines) then
    begin
      for I := 0 to GetArrayLength(Lines) - 1 do
      begin
        // "Microsoft.WindowsDesktop.App 10." を検索
        if Pos('Microsoft.WindowsDesktop.App 10.', Lines[I]) > 0 then
        begin
          Result := True;
          Break;
        end;
      end;
    end;
    DeleteFile(TempFile);
  end;
end;

function InitializeSetup(): Boolean;
var
  ErrorCode: Integer;
  MsgResult: Integer;
begin
  Result := True;
  
  // .NET 10 Runtime チェック
  if not IsDotNet10Installed then
  begin
    MsgResult := MsgBox('.NET 10 Desktop Runtimeが必要です。' + #13#10#13#10 + 
              'このインストーラーに同梱されています。' + #13#10 +
              'セットアップを続行しますか？' + #13#10#13#10 +
              '※ インターネット接続が必要な場合があります。', 
              mbConfirmation, MB_YESNO);
    
    if MsgResult = IDNO then
    begin
      Result := False;
    end;
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
