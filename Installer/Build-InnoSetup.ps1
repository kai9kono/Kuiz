# Kuiz Inno Setup ビルドスクリプト
# Installer\Build-InnoSetup.ps1 から実行

param(
    [switch]$SkipBuild,
    [string]$Configuration = "Release"
)

Write-Host "?? Kuiz Inno Setupインストーラーをビルド中..." -ForegroundColor Cyan
Write-Host "   構成: $Configuration" -ForegroundColor Yellow
Write-Host ""

# 親ディレクトリ（プロジェクトルート）に移動
$RootDir = Split-Path -Parent $PSScriptRoot
Push-Location $RootDir

try {
    # Inno Setupのパスを検索（複数の可能性をチェック）
    $possiblePaths = @(
        "C:\Users\kai9k\AppData\Local\Programs\Inno Setup 6\ISCC.exe",
        "C:\Program Files (x86)\Inno Setup 6\ISCC.exe",
        "C:\Program Files\Inno Setup 6\ISCC.exe",
        "C:\Program Files (x86)\Inno Setup 7\ISCC.exe",
        "C:\Program Files\Inno Setup 7\ISCC.exe",
        "C:\Program Files (x86)\Inno Setup\ISCC.exe",
        "C:\Program Files\Inno Setup\ISCC.exe"
    )
    
    $InnoSetupPath = $null
    foreach ($path in $possiblePaths) {
        if (Test-Path $path) {
            $InnoSetupPath = $path
            Write-Host "? Inno Setupを検出: $path" -ForegroundColor Green
            break
        }
    }
    
    if (-not $InnoSetupPath) {
        Write-Host "? エラー: Inno Setupが見つかりません" -ForegroundColor Red
        Write-Host ""
        Write-Host "以下のパスを確認しました:" -ForegroundColor Yellow
        foreach ($path in $possiblePaths) {
            Write-Host "  - $path" -ForegroundColor Gray
        }
        Write-Host ""
        Write-Host "?? Inno Setupをインストールしてください:" -ForegroundColor Yellow
        Write-Host "   https://jrsoftware.org/isdl.php" -ForegroundColor Blue
        Write-Host ""
        Write-Host "または、カスタムパスを使用する場合:" -ForegroundColor Yellow
        Write-Host "   `$InnoSetupPath = `"あなたのパス\ISCC.exe`"" -ForegroundColor Gray
        Write-Host "   .\Installer\Build-InnoSetup.ps1" -ForegroundColor Gray
        Write-Host ""
        exit 1
    }


    # 既存のビルドをチェック
    $publishPath = "bin\$Configuration\net10.0-windows\publish\win-x64"
    $exePath = Join-Path $publishPath "Kuiz.exe"
    
    if (Test-Path $exePath) {
        $lastBuildTime = (Get-Item $exePath).LastWriteTime
        $timeSinceLastBuild = (Get-Date) - $lastBuildTime
        
        Write-Host "?? 既存のビルドを検出:" -ForegroundColor Yellow
        Write-Host "   パス: $exePath" -ForegroundColor Gray
        Write-Host "   最終ビルド: $($lastBuildTime.ToString('yyyy-MM-dd HH:mm:ss'))" -ForegroundColor Gray
        Write-Host "   経過時間: $([math]::Round($timeSinceLastBuild.TotalMinutes, 1)) 分" -ForegroundColor Gray
        Write-Host ""
        
        if ($timeSinceLastBuild.TotalMinutes -gt 5 -and -not $SkipBuild) {
            Write-Host "??  最終ビルドから5分以上経過しています" -ForegroundColor Yellow
            Write-Host "   最新版を使用することを推奨します" -ForegroundColor Yellow
            Write-Host ""
            
            $response = Read-Host "最新版をビルドしますか？ (Y/n)"
            if ($response -eq "" -or $response -eq "y" -or $response -eq "Y") {
                $SkipBuild = $false
            } else {
                $SkipBuild = $true
            }
        }
    } else {
        Write-Host "?? 既存のビルドが見つかりません" -ForegroundColor Yellow
        Write-Host "   最新版をビルドします..." -ForegroundColor Yellow
        $SkipBuild = $false
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
        
        Write-Host "`n? ビルド完了！" -ForegroundColor Green
        
        # ビルド後のファイル情報表示
        if (Test-Path $exePath) {
            $fileSize = [math]::Round((Get-Item $exePath).Length / 1MB, 2)
            Write-Host "   ビルド日時: $((Get-Item $exePath).LastWriteTime.ToString('yyyy-MM-dd HH:mm:ss'))" -ForegroundColor Gray
            Write-Host "   ファイルサイズ: ${fileSize} MB" -ForegroundColor Gray
        }
    } else {
        Write-Host "? ビルドをスキップしました（-SkipBuild または既存ビルド使用）" -ForegroundColor Yellow
    }

    # ステップ 5: Inno Setupでインストーラーを作成
    Write-Host "`n?? ステップ 5: Inno Setupでインストーラーを作成..." -ForegroundColor Cyan

    # installerディレクトリを作成
    if (-not (Test-Path "installer")) {
        New-Item -ItemType Directory -Path "installer" | Out-Null
    }

    # Inno Setupをコンパイル（Installerフォルダ内のスクリプトを実行）
    Write-Host "   コンパイル中: Installer\KuizSetup.iss" -ForegroundColor Gray
    & $InnoSetupPath "Installer\KuizSetup.iss"

    if ($LASTEXITCODE -eq 0) {
        Write-Host "`n?? インストーラーを作成しました！" -ForegroundColor Green
        Write-Host ""
        
        # 作成されたファイルを表示
        $installerFiles = Get-ChildItem "installer\*.exe" | Sort-Object LastWriteTime -Descending | Select-Object -First 1
        if ($installerFiles) {
            $fileSize = [math]::Round($installerFiles.Length / 1MB, 2)
            Write-Host "?? インストーラー情報:" -ForegroundColor Cyan
            Write-Host "   ファイル: $($installerFiles.FullName)" -ForegroundColor Yellow
            Write-Host "   サイズ: ${fileSize} MB" -ForegroundColor Yellow
            Write-Host "   作成日時: $($installerFiles.LastWriteTime.ToString('yyyy-MM-dd HH:mm:ss'))" -ForegroundColor Yellow
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
