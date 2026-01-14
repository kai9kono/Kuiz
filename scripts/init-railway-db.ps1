# Railwayデータベースに問題データを投入するスクリプト

param(
    [string]$DatabaseUrl = "postgresql://postgres:QIxzKtmrJAPhGdElKcCTKerHLhMrZZiG@yamanote.proxy.rlwy.net:29357/railway"
)

Write-Host "?? Railwayデータベースに問題データを投入中..." -ForegroundColor Cyan
Write-Host ""

# PostgreSQLクライアント（psql）が利用可能かチェック
$psqlPath = $null
$possiblePaths = @(
    "psql",  # PATHに含まれている場合
    "C:\Program Files\PostgreSQL\16\bin\psql.exe",
    "C:\Program Files\PostgreSQL\15\bin\psql.exe",
    "C:\Program Files\PostgreSQL\14\bin\psql.exe",
    "C:\Program Files\PostgreSQL\13\bin\psql.exe",
    "C:\Program Files (x86)\PostgreSQL\16\bin\psql.exe",
    "C:\Program Files (x86)\PostgreSQL\15\bin\psql.exe",
    "C:\Program Files (x86)\PostgreSQL\14\bin\psql.exe",
    "C:\Program Files (x86)\PostgreSQL\13\bin\psql.exe"
)

foreach ($path in $possiblePaths) {
    try {
        $result = Get-Command $path -ErrorAction SilentlyContinue
        if ($result) {
            $psqlPath = $path
            Write-Host "? PostgreSQLクライアントを検出: $path" -ForegroundColor Green
            break
        }
    } catch {
        continue
    }
}

if (-not $psqlPath) {
    Write-Host "? エラー: PostgreSQLクライアント (psql) が見つかりません" -ForegroundColor Red
    Write-Host ""
    Write-Host "?? PostgreSQLをインストールしてください:" -ForegroundColor Yellow
    Write-Host "   https://www.postgresql.org/download/windows/" -ForegroundColor Blue
    Write-Host ""
    Write-Host "または、以下の代替方法を使用してください:" -ForegroundColor Yellow
    Write-Host "   1. Railway Web UIのデータベース管理画面からSQLを実行" -ForegroundColor White
    Write-Host "   2. pgAdminなどのGUIツールを使用" -ForegroundColor White
    Write-Host "   3. DBeaver、TablePlusなどのDBクライアントを使用" -ForegroundColor White
    Write-Host ""
    Write-Host "SQLファイルの場所: sql\init_railway_db.sql" -ForegroundColor Cyan
    Write-Host ""
    
    # SQLファイルを開く
    $openSql = Read-Host "SQLファイルを開きますか？ (Y/n)"
    if ($openSql -ne "n" -and $openSql -ne "N") {
        $sqlPath = Join-Path $PSScriptRoot "..\sql\init_railway_db.sql"
        if (Test-Path $sqlPath) {
            Start-Process "notepad.exe" -ArgumentList $sqlPath
        }
    }
    
    exit 1
}

# SQLファイルのパス
$RootDir = Split-Path -Parent $PSScriptRoot
$sqlFile = Join-Path $RootDir "sql\init_railway_db.sql"

if (-not (Test-Path $sqlFile)) {
    Write-Host "? エラー: SQLファイルが見つかりません: $sqlFile" -ForegroundColor Red
    exit 1
}

# 一時ファイルを作成してUTF-8に変換
$tempSqlFile = Join-Path $env:TEMP "init_railway_db_utf8.sql"
Write-Host "?? SQLファイルをUTF-8に変換中..." -ForegroundColor Cyan
try {
    # Shift-JISとして読み込み、UTF-8で保存
    $content = Get-Content -Path $sqlFile -Encoding Default
    $utf8NoBom = New-Object System.Text.UTF8Encoding $false
    [System.IO.File]::WriteAllLines($tempSqlFile, $content, $utf8NoBom)
    Write-Host "? UTF-8変換完了" -ForegroundColor Green
} catch {
    Write-Host "?? エンコーディング変換に失敗、元のファイルを使用します" -ForegroundColor Yellow
    $tempSqlFile = $sqlFile
}

Write-Host "?? SQLファイル: $tempSqlFile" -ForegroundColor Gray
Write-Host "?? データベース: $DatabaseUrl" -ForegroundColor Gray
Write-Host ""

# psqlコマンドを実行
Write-Host "?? データを投入中..." -ForegroundColor Cyan
Write-Host ""

try {
    # psqlコマンドを実行（UTF-8エンコーディング指定）
    $env:PGCLIENTENCODING = "UTF8"
    & $psqlPath $DatabaseUrl -f $tempSqlFile
    
    if ($LASTEXITCODE -eq 0) {
        Write-Host ""
        Write-Host "?? 問題データの投入が完了しました！" -ForegroundColor Green
        Write-Host ""
        
        # 問題数を確認
        Write-Host "?? データベースの問題数を確認中..." -ForegroundColor Cyan
        $countQuery = "SELECT COUNT(*) as count FROM questions;"
        $result = & $psqlPath $DatabaseUrl -t -c $countQuery
        
        if ($LASTEXITCODE -eq 0) {
            $count = $result.Trim()
            Write-Host "   問題数: $count 問" -ForegroundColor Yellow
        }
        
        Write-Host ""
        Write-Host "? これでアプリから問題が利用できるようになりました！" -ForegroundColor Green
        Write-Host ""
        
        # 一時ファイルをクリーンアップ
        if ((Test-Path $tempSqlFile) -and ($tempSqlFile -ne $sqlFile)) {
            Remove-Item $tempSqlFile -ErrorAction SilentlyContinue
        }
    } else {
        Write-Host ""
        Write-Host "? データ投入に失敗しました" -ForegroundColor Red
        Write-Host ""
        Write-Host "?? トラブルシューティング:" -ForegroundColor Yellow
        Write-Host "   1. データベースURLが正しいか確認" -ForegroundColor White
        Write-Host "   2. ネットワーク接続を確認" -ForegroundColor White
        Write-Host "   3. データベースのアクセス権限を確認" -ForegroundColor White
        Write-Host ""
        
        # 一時ファイルをクリーンアップ
        if ((Test-Path $tempSqlFile) -and ($tempSqlFile -ne $sqlFile)) {
            Remove-Item $tempSqlFile -ErrorAction SilentlyContinue
        }
        
        exit 1
    }
} catch {
    Write-Host ""
    Write-Host "? エラーが発生しました: $_" -ForegroundColor Red
    
    # 一時ファイルをクリーンアップ
    if ((Test-Path $tempSqlFile) -and ($tempSqlFile -ne $sqlFile)) {
        Remove-Item $tempSqlFile -ErrorAction SilentlyContinue
    }
    
    exit 1
}

# 一時ファイルをクリーンアップ
if ((Test-Path $tempSqlFile) -and ($tempSqlFile -ne $sqlFile)) {
    Remove-Item $tempSqlFile -ErrorAction SilentlyContinue
}
