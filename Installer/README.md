# Kuiz インストーラー作成手順

このディレクトリには、Kuizアプリケーションの Windows インストーラー（Inno Setup）を作成するためのファイルが含まれています。

## ?? 必要なもの

1. **Inno Setup 6 以降**
   - ダウンロード: https://jrsoftware.org/isdl.php
   - インストール後、`ISCC.exe` のパスを確認

2. **.NET 10 Desktop Runtime インストーラー**
   - 自動ダウンロードスクリプトを実行（下記参照）
   - または手動ダウンロード: https://aka.ms/dotnet/10.0/windowsdesktop-runtime-win-x64.exe

## ?? インストーラー作成手順

### ステップ1: .NET Runtime インストーラーをダウンロード

プロジェクトルートから以下を実行：

```powershell
.\Installer\Download-DotNetRuntime.ps1
```

これにより、`Installer\Dependencies\` フォルダに .NET 10 Desktop Runtime インストーラーがダウンロードされます。

### ステップ2: Kuiz インストーラーをビルド

プロジェクトルートから以下を実行：

```powershell
.\Installer\Build-InnoSetup.ps1
```

オプション：
- `-SkipBuild`: アプリのビルドをスキップ（既存のビルドを使用）
- `-Configuration Debug`: デバッグ構成でビルド（デフォルト: Release）

例：
```powershell
# 最新のアプリをビルドしてからインストーラーを作成
.\Installer\Build-InnoSetup.ps1

# 既存のビルドを使用してインストーラーのみ作成
.\Installer\Build-InnoSetup.ps1 -SkipBuild

# デバッグビルドでインストーラーを作成
.\Installer\Build-InnoSetup.ps1 -Configuration Debug
```

## ?? 出力ファイル

インストーラーは以下の場所に作成されます：

```
installer/KuizSetup-1.0.1.exe
```

## ?? トラブルシューティング

### Inno Setup が見つからない場合

エラーメッセージに表示されるパスを確認し、Inno Setup を正しくインストールしてください。

### .NET Runtime インストーラーのダウンロードに失敗する場合

手動でダウンロード：
1. https://aka.ms/dotnet/10.0/windowsdesktop-runtime-win-x64.exe にアクセス
2. ダウンロードしたファイルを `Installer\Dependencies\windowsdesktop-runtime-10-win-x64.exe` として保存

### ビルドエラーが発生する場合

1. プロジェクトが正常にビルドできることを確認：
   ```powershell
   dotnet build -c Release
   ```

2. 発行（publish）が正常に完了することを確認：
   ```powershell
   dotnet publish -c Release -r win-x64 --self-contained false
   ```

## ?? インストーラーの機能

作成されるインストーラーには以下の機能が含まれます：

- ? .NET 10 Desktop Runtime の自動インストール（未インストールの場合）
- ? Kuiz アプリケーション本体のインストール
- ? リソースファイルの配置
- ? スタートメニューへのショートカット作成
- ? デスクトップアイコン作成（オプション）
- ? アンインストーラーの自動生成

## ?? バージョン更新時の注意

新しいバージョンをリリースする際は、以下のファイルを更新してください：

1. `AppVersion.cs` - アプリケーションバージョン
2. `KuizSetup.iss` - `#define MyAppVersion` の値

## ?? 参考リンク

- [Inno Setup 公式ドキュメント](https://jrsoftware.org/ishelp/)
- [.NET ダウンロード](https://dotnet.microsoft.com/download/dotnet/10.0)
- [Kuiz GitHub リポジトリ](https://github.com/kai9kono/Kuiz)
