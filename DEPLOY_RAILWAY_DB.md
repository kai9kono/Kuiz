# Kuiz Server - Railway.app デプロイガイド

[既存の内容は省略...]

---

## ?? PostgreSQL データベースのセットアップ

### 1. Railway でPostgreSQLサービスを追加

1. **Railwayダッシュボード**で「+ New」をクリック
2. 「Database」 → 「PostgreSQL」を選択
3. プロジェクトにPostgreSQLサービスが追加されます

### 2. 環境変数の自動設定

RailwayはPostgreSQLを追加すると、自動的に以下の環境変数を設定します：

```
DATABASE_URL=postgres://user:password@host:port/database
```

**? 重要**: `DATABASE_URL`環境変数は自動的にKuizServerに設定され、Npgsql形式への接続文字列変換も自動で行われます。

### 3. データベース初期化の確認

デプロイ後、ログで以下を確認してください：

```bash
railway logs
```

**期待されるログ出力**：
```
?? Initializing database...
?? Using Railway DATABASE_URL: Host=xxxxx.railway.app, Database=railway
? Database connection established
? Questions table ready
?? Total questions in database: 0
```

### 4. ダミー問題の追加

データベースに問題を追加するには、以下のいずれかの方法を使用：

#### 方法1: Railway Webコンソールから

1. PostgreSQLサービスをクリック
2. 「Data」タブを開く
3. SQLクエリを実行

```sql
INSERT INTO questions (text, answer, author, created_at) VALUES
('日本の首都は？', '東京', 'System', NOW()),
('1 + 1 = ?', '2', 'System', NOW()),
('地球の衛星の名前は？', '月', 'System', NOW()),
('富士山の高さは約何メートル？', '3776メートル', 'System', NOW()),
('日本の国技は？', '相撲', 'System', NOW());
```

#### 方法2: Kuiz クライアントから

1. Kuizアプリを起動
2. 「問題マネージャー」→「作問」
3. 問題を作成して「DBにインポート」をクリック

### 5. トラブルシューティング

#### ? 問題が0件の場合

**原因**: データベースが空

**解決策**:
1. Railway WebコンソールでSQLを実行して問題を追加
2. または、Kuizクライアントの「問題マネージャー」から作問してインポート

#### ? クライアントで「利用可能問題数: 0」と表示される

**原因**: 
1. データベースが空
2. クライアントがRailway APIに接続できていない

**解決策**:
1. **データベースに問題を追加**（上記方法1または2）
2. **ブラウザでAPI動作確認**:
   ```
   https://kuiz-production.up.railway.app/api/question
   ```
   → JSON形式で問題リストが返ってくるはずです

## ?? 完全なデプロイチェックリスト

- [ ] KuizServer をRailwayにデプロイ
- [ ] PostgreSQLサービスを追加
- [ ] `DATABASE_URL`環境変数が設定されていることを確認
- [ ] `railway logs`でデータベース初期化成功を確認
- [ ] Railway WebコンソールまたはKuizクライアントから問題を追加
- [ ] ブラウザで `https://kuiz-production.up.railway.app/api/question` にアクセスして問題リストを確認
- [ ] Kuizクライアントで「問題読み込み」をテスト
- [ ] ゲームを開始して問題が表示されることを確認

これで完璧にRailwayのPostgreSQLに接続できるようになります！??
