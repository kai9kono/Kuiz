# Kuiz - Inno Setupインストーラー作成ガイド

## ?? Inno Setupを選んだ理由

? **超シンプル**：1つのEXEファイルで完結  
? **証明書不要**：自己署名証明書なしで配布可能  
? **開発者モード不要**：ユーザー側の設定不要  
? **完全無料**：商用利用もOK  
? **日本語対応**：完全な日本語インストーラー  
? **広い互換性**：Windows 10/11で動作  

---

## ?? 必要なもの

### 1. Inno Setupをダウンロード

**公式サイト**：https://jrsoftware.org/isdl.php

**推奨バージョン**：Inno Setup 6.x（最新版）

インストール時のオプション：
- ? すべてデフォルトでOK
- ? 日本語も自動で含まれます

---

## ?? インストーラーのビルド方法

### 方法1: PowerShellスクリプトを使用（推奨・簡単）

```powershell
# リリースビルド + インストーラー作成
.\Build-InnoSetup.ps1

# デバッグビルド
.\Build-InnoSetup.ps1 -Configuration Debug

# ビルドをスキップして、既存のファイルからインストーラーを作成
.\Build-InnoSetup.ps1 -SkipBuild
```

### 方法2: 手動でビルド

#### ステップ1: アプリをパブリッシュ

```powershell
dotnet publish Kuiz.csproj -c Release -r win-x64 --self-contained false
```

#### ステップ2: Inno Setupを実行

```powershell
"C:\Program Files (x86)\Inno Setup 6\ISCC.exe" KuizSetup.iss
```

---

## ?? 生成されるファイル

```
installer/
└── KuizSetup-1.0.0.exe  ← これを配布！
```

**ファイルサイズ**：約100-150MB（.NET Runtime非同梱）

---

## ?? 配布方法

### GitHub Releases（推奨）

1. **GitHubでリリースを作成**
```
GitHub → Releases → Create a new release
→ Tag: v1.0.0
→ Title: Kuiz v1.0.0
```

2. **インストーラーをアップロード**
```
Files:
- KuizSetup-1.0.0.exe  ← メインインストーラー
- インストール手順.txt  ← ユーザー向け手順
```

3. **リリースノートを記入**
```markdown
# Kuiz v1.0.0

オンライン早押しクイズゲーム「Kuiz」の初回リリースです。

## ダウンロード
- [KuizSetup-1.0.0.exe](リンク) - Windowsインストーラー

## インストール方法
1. KuizSetup-1.0.0.exeをダウンロード
2. 実行してインストール
3. .NET 10が必要な場合、自動的にダウンロードページが開きます

## 遊び方
詳しくは[インストール手順.txt](リンク)を参照してください。
```

### その他の配布方法

- **Discord/LINEで直接送信**（100-150MB）
- **OneDrive/Google Drive/Dropbox**で共有
- **独自Webサイト**からダウンロード提供

---

## ?? ユーザー側のインストール手順

### 超シンプル！

1. **KuizSetup-1.0.0.exeをダブルクリック**
2. **「次へ」を数回クリック**
3. **完了！**

### .NET 10が必要な場合

インストーラーが自動で検出して、ダウンロードページを開きます：
```
https://dotnet.microsoft.com/download/dotnet/10.0
```

→ 「.NET Desktop Runtime 10.x.x」をダウンロード・インストール  
→ 再度Kuizインストーラーを実行

---

## ?? カスタマイズ

### バージョン番号を変更

`KuizSetup.iss`を編集：

```pascal
#define MyAppVersion "1.0.0"  ← ここを変更
```

### アイコンを変更

```pascal
SetupIconFile=Resources\icon\icon.ico  ← アイコンファイルのパス
```

### 追加のファイルを含める

```pascal
[Files]
; 新しいファイルを追加
Source: "path\to\yourfile.txt"; DestDir: "{app}"; Flags: ignoreversion
```

---

## ?? インストール先

### アプリケーション本体
```
C:\Program Files\Kuiz\
├── Kuiz.exe
├── *.dll
├── Resources\
└── README.md
```

### ユーザーデータ
```
%LocalAppData%\Kuiz\
├── profile.json
└── logs\
```

---

## ?? トラブルシューティング

### エラー: "Inno Setupが見つかりません"

**解決策**: Inno Setup 6をインストール
```
https://jrsoftware.org/isdl.php
```

### エラー: "ビルドに失敗しました"

**解決策**: 
```powershell
# クリーンビルド
dotnet clean
dotnet restore
dotnet build -c Release
```

### Windows Defenderに引っかかる

**原因**: 新しいEXEファイルのため  
**解決策**: 
1. 「詳細情報」をクリック
2. 「実行」をクリック
3. または、例外リストに追加

### コード署名証明書について

**現在**: 自己署名なし（無料）  
**将来**: EV証明書を購入すると警告なし（約5万円/年）

---

## ?? アップデート配布

### 新バージョンのリリース

1. **バージョン番号を更新**
```pascal
#define MyAppVersion "1.1.0"  ← KuizSetup.issを編集
```

2. **ビルド**
```powershell
.\Build-InnoSetup.ps1
```

3. **GitHub Releasesで公開**

### ユーザー側

- 新しいインストーラーを実行
- 既存バージョンに上書きインストール
- ユーザーデータ（profile.json等）は保持される

---

## ?? ライセンス

このインストーラー作成ツールはオープンソースです。  
Inno Setupもオープンソース（商用利用可）です。

---

## ?? サポート

問題が発生した場合：
- **GitHub Issues**: https://github.com/kai9kono/Kuiz/issues
- **Discord**: [あなたのDiscordサーバー]
- **Email**: [あなたのメール]

---

## ?? 完成！

これで友達に簡単に配布できるインストーラーができました！

**次のステップ**:
1. ? インストーラーをビルド
2. ? GitHub Releasesで公開
3. ? 友達に共有
4. ? 一緒に早押しクイズを楽しむ！??

---

## ?? 配布チェックリスト

- [ ] README.mdを更新（ダウンロードリンク追加）
- [ ] LICENSEファイルを確認
- [ ] スクリーンショットを用意
- [ ] 遊び方の動画を作成（任意）
- [ ] GitHubのReleasesページを整備
- [ ] SNSで告知
