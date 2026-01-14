# Kuiz Inno Setup ビルドスクリプト
# Installer\Build-InnoSetup.ps1 から実行

param(
    [string]$Configuration = "Release",
    [switch]$SkipBuild
)

Write-Host "?? Kuiz Inno Setupインストーラーをビルド中..." -ForegroundColor Cyan
Write-Host "   構成: $Configuration" -ForegroundColor Yellow
Write-Host ""

# 親ディレクトリ（プロジェクトルート）に移動
$RootDir = Split-Path -Parent $PSScriptRoot
Push-Location $RootDir

try {
    # Inno Setupのパスを検索
    $InnoSetupPath = "C:\Program Files (x86)\Inno Setup 6\ISCC.exe"
    if (-not (Test-Path $InnoSetupPath)) {
        Write-Host "? エラー: Inno Setupが見つかりません" -ForegroundColor Red
        Write-Host ""
        Write-Host "?? Inno Setupをインストールしてください:" -ForegroundColor Yellow
        Write-Host "   https://jrsoftware.org/isdl.php" -ForegroundColor Blue
        Write-Host ""
        Write-Host "インストール後、再度このスクリプトを実行してください。" -ForegroundColor Yellow
        exit 1
    }

    if (-not $SkipBuild) {
        # ステップ 1: クリーン
        Write-Host "?? ステップ 1: クリーン..." -ForegroundColor Cyan
        dotnet clean -c $Configuration

        # ステップ 2: リストア
        Write-Host "`n?? ステップ 2: 依存関係をリストア..." -ForegroundColor Cyan
        dotnet restore

        # ステップ 3: ビルド
        Write-Host "`n?? ステップ 3: プロジェクトをビルド..." -ForegroundColor Cyan
        dotnet build -c $Configuration

        # ステップ 4: パブリッシュ
        Write-Host "`n?? ステップ 4: アプリをパブリッシュ..." -ForegroundColor Cyan
        dotnet publish Kuiz.csproj -c $Configuration -r win-x64 --self-contained false -p:PublishSingleFile=false

        if ($LASTEXITCODE -ne 0) {
            Write-Host "`n? ビルドに失敗しました" -ForegroundColor Red
            exit 1
        }
    }

    # ステップ 5: Inno Setupでインストーラーを作成
    Write-Host "`n?? ステップ 5: Inno Setupでインストーラーを作成..." -ForegroundColor Cyan

    # installerディレクトリを作成
    if (-not (Test-Path "installer")) {
        New-Item -ItemType Directory -Path "installer" | Out-Null
    }

    # Inno Setupをコンパイル（Installerフォルダ内のスクリプトを実行）
    & $InnoSetupPath "Installer\KuizSetup.iss"

    if ($LASTEXITCODE -eq 0) {
        Write-Host "`n?? インストーラーを作成しました！" -ForegroundColor Green
        Write-Host ""
        
        # 作成されたファイルを表示
        $installerFiles = Get-ChildItem "installer\*.exe" | Sort-Object LastWriteTime -Descending | Select-Object -First 1
        if ($installerFiles) {
            $fileSize = [math]::Round($installerFiles.Length / 1MB, 2)
            Write-Host "?? ファイル: $($installerFiles.FullName)" -ForegroundColor Yellow
            Write-Host "?? サイズ: ${fileSize} MB" -ForegroundColor Yellow
            Write-Host ""
        }
        
        Write-Host "?? 配布方法:" -ForegroundColor Cyan
        Write-Host "  1. GitHub Releasesにアップロード" -ForegroundColor White
        Write-Host "  2. または、直接友人に送信" -ForegroundColor White
        Write-Host ""
        Write-Host "?? ユーザーは開発者モードや証明書なしでインストール可能！" -ForegroundColor Green
        Write-Host ""
        
        # インストーラーを開く
        $openInstaller = Read-Host "インストーラーフォルダを開きますか？ (Y/n)"
        if ($openInstaller -ne "n" -and $openInstaller -ne "N") {
            Start-Process "explorer.exe" -ArgumentList "/select,`"$($installerFiles.FullName)`""
        }
    } else {
        Write-Host "`n? インストーラーの作成に失敗しました" -ForegroundColor Red
        exit 1
    }

    Write-Host "? 完了！" -ForegroundColor Green
}
finally {
    # 元のディレクトリに戻る
    Pop-Location
}
