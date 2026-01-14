# Railway.app 無料デプロイ - Kuiz

## 概要
このプロジェクトは、**Railway.app（完全無料）**上にデプロイされたサーバーを使用して、インターネット越しに複数のプレイヤーが早押しクイズゲームをプレイできるアプリケーションです。

## プロジェクト構成

### クライアント (Kuiz)
- **技術**: WPF (.NET 10)
- **役割**: ゲームUI、プレイヤー操作、サーバーとの通信

### サーバー (KuizServer)
- **技術**: ASP.NET Core Web API + SignalR
- **役割**: ロビー管理、ゲーム状態管理、リアルタイム通信
- **デプロイ先**: Railway.app (無料プラン)

## ?? 無料プランの特徴

- **料金**: ?0/月 (最初$5クレジット付き)
- **制限**: 500時間/月の実行時間 (個人利用には十分)
- **スリープ**: なし (常時起動)
- **自動デプロイ**: GitHubと連携

## セットアップ手順

### 1. GitHubリポジトリの準備

既にGitHubにプッシュ済みなので、このステップは完了です！
Repository: https://github.com/kai9kono/Kuiz

### 2. Railway.appアカウント作成

1. https://railway.app にアクセス
2. **"Login with GitHub"** をクリック
3. GitHubアカウントで認証

### 3. プロジェクトのデプロイ

#### ステップ1: 新しいプロジェクト作成
1. Railway ダッシュボードで **"New Project"** をクリック
2. **"Deploy from GitHub repo"** を選択
3. リポジトリ一覧から **"kai9kono/Kuiz"** を選択

#### ステップ2: サービス設定
1. **"Add a service"** → **"GitHub Repo"**
2. **Root Directory** を `KuizServer` に設定
3. **Build Command**: `dotnet publish -c Release -o out`
4. **Start Command**: `dotnet out/KuizServer.dll`

#### ステップ3: 環境変数設定（任意）
Railway ダッシュボードで環境変数を追加:
- `ASPNETCORE_ENVIRONMENT`: `Production`
- `PORT`: `8080` (Railwayが自動設定)

#### ステップ4: デプロイ実行
1. **"Deploy"** ボタンをクリック
2. ビルドとデプロイが自動で開始されます（3-5分）
3. 完了後、**"Settings"** → **"Domains"** でURLを取得
   - 例: `https://kuiz-production.up.railway.app`

### 4. クライアントの設定

デプロイしたサーバーのURLをクライアントに設定します。

#### オプション1: appsettings.json を作成
`Kuiz/appsettings.json`:
```json
{
  "ServerUrl": "https://your-app-name.up.railway.app"
}
```

#### オプション2: コードに直接記述（開発用）
`Services/AzureClientService.cs` の `ServerUrl` を変更:
```csharp
private const string ServerUrl = "https://your-app-name.up.railway.app";
```

### 5. NuGet パッケージの追加

クライアント側に SignalR クライアントを追加:

```bash
cd Kuiz
dotnet add package Microsoft.AspNetCore.SignalR.Client --version 8.0.0
```

## 使用方法

### ローカルテスト
```bash
# サーバー起動
cd KuizServer
dotnet run

# クライアント起動 (別のターミナル)
cd Kuiz
dotnet run
```

### Railway環境
1. Railway.appにデプロイ（自動）
2. クライアントの接続先URLをRailwayのURLに変更
3. クライアントアプリケーションを実行
4. **友達とロビーコードを共有してプレイ！**

## アーキテクチャ

```
┌─────────────┐         HTTPS          ┌──────────────────┐
│ クライアント1  │ ?──────────────────? │   Railway.app    │
│   (WPF)     │                        │   (Web API)      │
└─────────────┘   SignalR/HTTP        └──────────────────┘
                                               ▲
┌─────────────┐                               │
│ クライアント2  │ ?─────────────────────────────┘
│   (WPF)     │         HTTPS
└─────────────┘
```

## API エンドポイント

### REST API
- `POST /api/lobby/create` - ロビー作成
- `POST /api/lobby/join` - ロビー参加
- `GET /api/lobby/{code}` - ロビー情報取得
- `POST /api/lobby/{code}/settings` - 設定更新

### SignalR Hub (`/gamehub`)
- `JoinLobby(lobbyCode, playerName)` - ロビー参加
- `Buzz(lobbyCode, playerName)` - 早押し
- `SubmitAnswer(lobbyCode, playerName, answer)` - 回答送信
- `NextQuestion(lobbyCode, question, answer)` - 次の問題
- `StartGame(lobbyCode)` - ゲーム開始

## Railway.app 設定ファイル

自動デプロイのため、以下のファイルを使用します：

### `railway.toml` (プロジェクトルートに配置)
```toml
[build]
builder = "NIXPACKS"
buildCommand = "cd KuizServer && dotnet publish -c Release -o out"

[deploy]
startCommand = "cd KuizServer/out && dotnet KuizServer.dll"
restartPolicyType = "ON_FAILURE"
restartPolicyMaxRetries = 10
```

### `nixpacks.toml` (KuizServerフォルダに配置)
```toml
[phases.setup]
nixPkgs = ["dotnet-sdk_8"]

[phases.build]
cmds = ["dotnet publish -c Release -o out"]

[start]
cmd = "dotnet out/KuizServer.dll"
```

## セキュリティ

- ? HTTPS通信を自動使用
- ? CORS設定で特定のオリジンのみ許可
- ? Railway環境変数で機密情報を管理

## コスト見積もり

### Railway.app 無料プラン
- **料金**: ?0/月
- **制限**: 500時間/月（約21日間常時稼働可能）
- **スペック**: 
  - 512 MB RAM
  - 1 vCPU shared
  - 1GB storage

**合計**: **?0/月** ??

### 使用量の目安
- **軽い使用** (1日2時間): 60時間/月 ?
- **中程度** (1日8時間): 240時間/月 ?
- **ヘビー使用** (常時稼働): 720時間/月 ?? ($5クレジット使用)

## トラブルシューティング

### デプロイが失敗する場合
1. Railway のログを確認:
   - ダッシュボード → プロジェクト → **"Deployments"** → ログを確認
2. ビルドコマンドが正しいか確認
3. .NET 8 SDKがインストールされているか確認

### 接続できない場合
1. Railway のURLが正しいか確認
2. クライアントの `ServerUrl` を確認
3. Railway ダッシュボードでサービスが **"Active"** か確認

### SignalR接続エラー
1. WebSocket が有効になっているか確認（Railwayは自動で有効）
2. CORS設定を確認
3. クライアントのFirewall設定を確認

### ログの確認
Railway ダッシュボード:
1. プロジェクトを開く
2. **"Deployments"** タブをクリック
3. 最新のデプロイメントを選択
4. **"View Logs"** でリアルタイムログを確認

## 次のステップ

### 1. ローカルでテスト
```bash
cd KuizServer
dotnet run
```
ブラウザで `http://localhost:5000/swagger` を開いてAPIを確認

### 2. Railwayにデプロイ
- GitHubにプッシュ（自動デプロイ）
- または Railway CLIを使用:
```bash
npm install -g @railway/cli
railway login
railway link
railway up
```

### 3. クライアント側の実装
- SignalR クライアントの統合
- サーバーURLの設定
- エラーハンドリング

## よくある質問

### Q: 本当に完全無料？
**A**: はい！Railway.appの無料プランは月500時間まで無料です。個人利用には十分です。

### Q: スリープしない？
**A**: はい、Railwayは常時起動なので、Azureの無料プランのようにスリープしません。

### Q: デプロイは難しい？
**A**: いいえ！GitHubと連携するだけで、プッシュするたびに自動デプロイされます。

### Q: カスタムドメインは使える？
**A**: 有料プランでのみ可能です。無料プランは `*.up.railway.app` のサブドメインのみ。

### Q: データベースは必要？
**A**: このアプリではメモリ内でデータを管理しているため、不要です。

## サポート

問題が発生した場合:
1. Railway ドキュメント: https://docs.railway.app
2. Railway Discord: https://discord.gg/railway
3. GitHub Issues: https://github.com/kai9kono/Kuiz/issues

## ライセンス
? 2026 Kai Kono
