# Kuiz Server - Railway.app デプロイガイド

## ?? Railway.appへのデプロイ手順

### 1. Railway CLIのインストール
```bash
npm install -g @railway/cli
```

### 2. Railwayにログイン
```bash
railway login
```

### 3. プロジェクトの初期化
```bash
# リポジトリのルートディレクトリで実行
railway init
```

### 4. デプロイ
```bash
railway up
```

### 5. ドメインの設定
1. Railway ダッシュボードにアクセス
2. プロジェクトを選択
3. "Settings" → "Domains" → "Generate Domain"
4. 生成されたURLをメモ（例: `your-app.up.railway.app`）

## ?? 環境変数の設定（必要に応じて）

Railway ダッシュボードの "Variables" セクションで設定:

```
ASPNETCORE_ENVIRONMENT=Production
ASPNETCORE_URLS=http://+:8080
```

## ?? クライアント側の接続設定

KuizクライアントアプリケーションのHostServiceを更新:

```csharp
// 既存のHTTPListener方式からSignalR接続に変更
private const string ServerUrl = "https://your-app.up.railway.app";
```

## ? ヘルスチェック

デプロイ後、以下のURLでサーバーの稼働状況を確認:

```
https://your-app.up.railway.app/health
```

期待されるレスポンス:
```json
{
  "status": "healthy",
  "timestamp": "2025-01-29T12:00:00.000Z"
}
```

## ?? ログの確認

```bash
railway logs
```

## ??? ローカルでのテスト

```bash
cd KuizServer
dotnet run
```

ブラウザで `http://localhost:8080/health` にアクセスして動作確認

## ?? 含まれる機能

- ? SignalRによるリアルタイム通信
- ? ロビー管理（作成・参加・退出）
- ? ゲーム状態管理
- ? プレイヤー管理
- ? ヘルスチェックエンドポイント
- ? Swagger UI（開発環境）

## ?? エンドポイント

### SignalR Hub
- `wss://your-app.up.railway.app/gamehub`

### REST API
- `GET /health` - ヘルスチェック
- `GET /api/lobby/{lobbyCode}` - ロビー情報取得

### SignalR メソッド
- `CreateLobby(hostName)` - ロビー作成
- `JoinLobby(lobbyCode, playerName)` - ロビー参加
- `LeaveLobby(lobbyCode, playerName)` - ロビー退出
- `SendBuzz(lobbyCode, playerName)` - バズ送信
- `SendAnswer(lobbyCode, playerName, answer)` - 回答送信

## ?? トラブルシューティング

### ビルドエラー
```bash
dotnet restore
dotnet build
```

### 接続エラー
1. Railwayダッシュボードでログを確認
2. ドメインが正しく生成されているか確認
3. CORSポリシーを確認

### タイムアウト
Railway無料プランでは15分の非アクティブ後にスリープします。
初回接続時に起動までに数秒かかる場合があります。

## ?? 料金情報

Railway無料プラン:
- ? 月500実行時間
- ? 512MB RAM
- ? カスタムドメイン
- ? HTTPS自動化

## ?? 参考リンク

- [Railway Documentation](https://docs.railway.app/)
- [ASP.NET Core SignalR](https://docs.microsoft.com/aspnet/core/signalr/)
- [Railway CLI](https://docs.railway.app/develop/cli)
