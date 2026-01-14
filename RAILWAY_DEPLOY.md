# ?? Railway.app デプロイガイド

このガイドでは、KuizServerをRailway.appに**完全無料**でデプロイする手順を説明します。

## ?? 前提条件

- [x] GitHubアカウント
- [x] このリポジトリがGitHubにプッシュされている
- [ ] Railway.appアカウント（これから作成）

## ?? デプロイ手順

### ステップ1: Railway.appアカウント作成

1. https://railway.app にアクセス
2. **"Login with GitHub"** をクリック
3. GitHubアカウントで認証
4. リポジトリへのアクセスを許可

### ステップ2: 新しいプロジェクト作成

1. Railway ダッシュボードで **"New Project"** をクリック
2. **"Deploy from GitHub repo"** を選択
3. **"kai9kono/Kuiz"** を選択
4. **"Deploy Now"** をクリック

### ステップ3: サービス設定（自動検出）

Railwayは`railway.toml`と`KuizServer/nixpacks.toml`を自動で検出します。

設定内容:
- **Build Command**: `cd KuizServer && dotnet publish -c Release -o out`
- **Start Command**: `cd KuizServer/out && dotnet KuizServer.dll`
- **Port**: 自動（環境変数 `$PORT` から取得）

### ステップ4: デプロイ完了を確認

1. デプロイが開始されます（3-5分）
2. ログでビルド進行状況を確認
3. 成功すると **"Active"** ステータスに変わります

### ステップ5: URLを取得

1. プロジェクトの **"Settings"** タブを開く
2. **"Domains"** セクションを探す
3. **"Generate Domain"** をクリック
4. 生成されたURLをコピー
   - 例: `https://kuiz-production.up.railway.app`

## ?? クライアント側の設定

### オプション1: 設定ファイルを使用（推奨）

`Kuiz/appsettings.json` を作成:

```json
{
  "ServerUrl": "https://your-app-name.up.railway.app"
}
```

### オプション2: コードに直接記述

`Services/AzureClientService.cs` (作成予定):

```csharp
private const string ServerUrl = "https://your-app-name.up.railway.app";
```

### SignalR クライアントのインストール

```bash
cd Kuiz
dotnet add package Microsoft.AspNetCore.SignalR.Client --version 8.0.0
```

## ?? テスト

### サーバーの動作確認

ブラウザで以下のURLにアクセス:
```
https://your-app-name.up.railway.app/api/lobby/create
```

正常に動作していれば、JSONレスポンスが返ります。

### ローカルテスト

```bash
# サーバー起動
cd KuizServer
dotnet run

# 別のターミナルでクライアント起動
cd Kuiz
dotnet run
```

## ?? 使用量の確認

Railway ダッシュボード:
1. プロジェクトを開く
2. **"Usage"** タブで使用時間を確認
3. 無料プランは **500時間/月** まで

## ?? 自動デプロイ

GitHubにプッシュすると自動でデプロイされます:

```bash
git add .
git commit -m "Update server code"
git push origin master
```

Railwayが自動で:
1. 変更を検知
2. ビルドを実行
3. デプロイを実行
4. サービスを再起動

## ?? トラブルシューティング

### デプロイが失敗する

**ログを確認**:
1. ダッシュボード → プロジェクト
2. **"Deployments"** タブ
3. 失敗したデプロイをクリック
4. **"View Logs"** でエラーを確認

**よくあるエラー**:
- ? `.NET SDK not found` → `nixpacks.toml` が正しく設定されているか確認
- ? `Build failed` → `KuizServer.csproj` のパスを確認
- ? `Port binding failed` → Program.cs で `PORT` 環境変数を使用しているか確認

### 接続できない

1. **サービスがActiveか確認**
   - ダッシュボードで "Active" ステータスを確認

2. **URLが正しいか確認**
   - `https://` で始まっているか
   - ドメインが正しいか

3. **CORSエラー**
   - `Program.cs` の CORS設定を確認
   - ブラウザのコンソールでエラーメッセージを確認

### SignalR接続エラー

**WebSocket接続の確認**:
```javascript
// ブラウザのDevToolsコンソールで
const connection = new signalR.HubConnectionBuilder()
    .withUrl("https://your-app-name.up.railway.app/gamehub")
    .build();
connection.start();
```

## ?? パフォーマンス最適化

### ログレベルの調整

`appsettings.json`:
```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Warning",
      "Microsoft.AspNetCore": "Warning"
    }
  }
}
```

### メモリ使用量の削減

- 古いロビーの自動削除
- 接続プールの最適化
- 不要なサービスの無効化

## ?? Railway CLI（上級者向け）

### インストール

```bash
npm install -g @railway/cli
```

### ログイン

```bash
railway login
```

### プロジェクトとリンク

```bash
railway link
```

### ローカルでテスト（Railway環境変数を使用）

```bash
railway run dotnet run
```

### ログをリアルタイム表示

```bash
railway logs
```

### デプロイ

```bash
railway up
```

## ?? ヒント

1. **初回デプロイは時間がかかる**
   - .NET SDKのダウンロードが必要
   - 2回目以降はキャッシュを使用するため高速

2. **環境変数の追加**
   - ダッシュボード → Variables → Add Variable

3. **複数環境の管理**
   - Production / Staging 環境を分けることも可能

4. **モニタリング**
   - Railway ダッシュボードでCPU/メモリ使用量を確認

## ?? 参考リンク

- [Railway ドキュメント](https://docs.railway.app)
- [Railway Discord](https://discord.gg/railway)
- [Nixpacks ドキュメント](https://nixpacks.com/docs)
- [ASP.NET Core デプロイガイド](https://learn.microsoft.com/aspnet/core/host-and-deploy/)

## ? チェックリスト

デプロイ前に確認:
- [ ] `railway.toml` がプロジェクトルートにある
- [ ] `KuizServer/nixpacks.toml` が存在する
- [ ] `Program.cs` が `PORT` 環境変数を使用している
- [ ] GitHubにプッシュ済み
- [ ] Railway.appアカウント作成済み

デプロイ後に確認:
- [ ] デプロイステータスが "Active"
- [ ] URLにアクセスできる
- [ ] APIエンドポイントが動作する
- [ ] クライアントから接続できる

## ?? 完了！

これで、完全無料でインターネット越しにKuizをプレイできます！

友達とロビーコードを共有して、楽しくプレイしてください！
