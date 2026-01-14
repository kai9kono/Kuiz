# ?? Kuiz - 早押しクイズゲーム

インターネット越しに友達と一緒にプレイできる早押しクイズゲームです！

## ? 特徴

- ?? **インターネット対応**: 友達とオンラインでプレイ可能
- ?? **完全無料**: Railway.appで無料ホスティング
- ?? **モダンなUI**: WPF + Material Designで美しいインターフェース
- ?? **サウンドエフェクト**: ボタン音やゲーム音で臨場感アップ
- ?? **ダークモード**: ライト/ダークモード切り替え可能
- ?? **統計機能**: プレイ履歴や成績を記録

## ?? クイックスタート

### ローカルでプレイ（同じWi-Fi内）

1. プロジェクトをクローン:
```bash
git clone https://github.com/kai9kono/Kuiz.git
cd Kuiz
```

2. アプリを起動:
```bash
dotnet run
```

3. 「ルーム作成」でホスト、友達は「ルーム参加」でロビーコード入力！

### インターネット越しにプレイ

**サーバーをRailway.appにデプロイ**して、世界中の友達とプレイ！

?? 詳細は [RAILWAY_DEPLOY.md](RAILWAY_DEPLOY.md) を参照

## ?? 必要なもの

- **.NET 10 SDK**
- **Windows 10/11** (WPFアプリケーション)
- **SQLite** (問題データベース用、自動作成)

## ?? 遊び方

### ホスト側
1. 「ルーム作成」をクリック
2. ロビーコードを友達に共有
3. 全員が揃ったら「ゲーム開始」

### 参加側
1. 「ルーム参加」をクリック
2. ロビーコードを入力
3. ホストがゲームを開始するまで待機

### ゲーム中
1. 問題が出題される
2. 早押しボタンを押す
3. 回答を入力
4. 正解すると1ポイント、不正解はミス+1

## ?? 開発

### プロジェクト構成

```
Kuiz/
├── Kuiz/                   # WPFクライアント
│   ├── Services/          # ビジネスロジック
│   ├── Models/            # データモデル
│   ├── Resources/         # 画像・音声
│   └── MainWindow.xaml    # メインUI
│
├── KuizServer/            # ASP.NET Core サーバー
│   ├── Controllers/       # REST API
│   ├── Hubs/             # SignalR Hub
│   ├── Services/         # サーバーロジック
│   └── Models/           # データモデル
│
└── sql/                   # データベース初期化
```

### ビルド

```bash
# クライアント
cd Kuiz
dotnet build

# サーバー
cd KuizServer
dotnet build
```

### テスト

```bash
# サーバー起動
cd KuizServer
dotnet run

# 別のターミナルでクライアント起動
cd Kuiz
dotnet run
```

## ?? ドキュメント

- [Railway.app デプロイガイド](RAILWAY_DEPLOY.md) - 無料サーバーのセットアップ
- [Azure デプロイガイド](AZURE_SETUP.md) - 有料プラン向け（参考）

## ?? 技術スタック

### クライアント
- **WPF** (.NET 10)
- **Material Design In XAML**
- **SQLite** (Entity Framework Core)

### サーバー
- **ASP.NET Core** 8.0
- **SignalR** (リアルタイム通信)
- **Railway.app** (ホスティング)

## ?? 貢献

プルリクエスト歓迎！

1. フォーク
2. フィーチャーブランチ作成 (`git checkout -b feature/amazing-feature`)
3. コミット (`git commit -m 'Add amazing feature'`)
4. プッシュ (`git push origin feature/amazing-feature`)
5. プルリクエスト作成

## ?? TODO

- [ ] SignalRクライアント実装（インターネット対応）
- [ ] 問題のカテゴリー分け
- [ ] チーム戦モード
- [ ] ランキングシステム
- [ ] カスタムテーマ
- [ ] モバイル版（Xamarin/MAUI）

## ?? ライセンス

? 2026 Kai Kono

## ?? 謝辞

- **Material Design In XAML** - 美しいUIコンポーネント
- **Railway.app** - 無料ホスティング
- **SignalR** - リアルタイム通信

---

**楽しいクイズ体験を！** ??

問題があれば [Issues](https://github.com/kai9kono/Kuiz/issues) で報告してください。
