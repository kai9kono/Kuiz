# Kuiz Inno Setup インストーラー

## ? 簡単！1コマンドでインストーラー作成

### ?? クイックスタート

**プロジェクトルート**で以下を実行するだけ：

```powershell
.\Installer\Build-InnoSetup.ps1
```

これだけで：
- ? **最新版を自動ビルド**（古いビルドの場合は確認）
- ? **publish\win-x64 フォルダに出力**
- ? **インストーラーを自動作成**
- ? **installer\KuizSetup-1.0.0.exe を生成**

**5分以上経過したビルドがある場合**、スクリプトが自動的に検出して、最新版をビルドするか確認します！

---

## ?? 最新版を常に使用

### 自動ビルドチェック機能

```
?? 既存のビルドを検出:
   パス: bin\Release\net10.0-windows\publish\win-x64\Kuiz.exe
   最終ビルド: 2025-01-30 14:30:00
   経過時間: 12.5 分

??  最終ビルドから5分以上経過しています
   最新版を使用することを推奨します

最新版をビルドしますか？ (Y/n)
```

---

## ?? 重要: KuizSetup.issを直接コンパイルしないでください！

**? ダメな例**:
```
ISCC.exe Installer\KuizSetup.iss  ← 古いビルドを使う可能性
```

**? 正しい例**:
```powershell
.\Installer\Build-InnoSetup.ps1  ← 最新版を自動ビルド
```

---

## ?? オプション

```powershell
# 既存のビルドをそのまま使用
.\Installer\Build-InnoSetup.ps1 -SkipBuild

# Debug構成でビルド
.\Installer\Build-InnoSetup.ps1 -Configuration Debug
```

---

## ?? 詳細ドキュメント

- [インストール手順](インストール手順.txt)
- [GitHub Repository](https://github.com/kai9kono/Kuiz)
