# Kuiz の配布と更新

Windows x64版 v1.0.3。ユーザーの指示により Yu Gothic UI / Segoe UI を使う。モリサワフォントは同梱しない。

## 配布先

- 公開サイト: https://kuiz-download.kai9kono.chatgpt.site
- v1.0.3: https://github.com/kai9kono/Kuiz/releases/tag/v1.0.3
- サイトID: `appgprj_6ac0863b31208191baa569bfb86ecbd2`
- サイト作業場所: このリポジトリの `DownloadSite`（Sites管理の独立したGitリポジトリ）
- インストーラー: 公開GitHubリポジトリ `kai9kono/Kuiz` の Releases
- 本番API: https://kuiz-server.onrender.com/api/question
- 問題DB: Renderが接続する既存Neon PostgreSQL。接続情報はRender側で保持する。

## v1.0.3での確認

- 本番APIから100問を再取得し、問題文と答えを全件照合。
- 本番SignalRでロビー作成・参加、状態更新、早押し、回答、判定、次問、結果、ホスト権限、退出を確認。
- 2つのWPFクライアントによる回帰確認: 画面遷移、表示・回答時の早押し制御、ミス上限、答え表示、次問リセット、回答待ち、最終結果、プレイヤー色を確認。
- 初回の回帰確認では参加側の接続が終了し失敗した。再実行で全項目成功。状態ログに早押しを無効にする各フラグを追加して調査可能にした。
- Releaseビルド成功。既存のnullableなどの警告は残る。MaterialDesignColorsの互換性警告は2.1.4に修正。
- 本体と.NET Desktop Runtimeを一緒に収録するself-contained形式を使用。
- 同じファイルを収録した検証用インストーラー（AppIdと出力名のみ分離）で、インストール、417ファイルのハッシュ照合、タイトル画面の起動、上書き更新、削除を確認。既存1.0.1のインストール登録を保持した。
- インストーラーはコード署名なし。別のWindows PCや.NET未導入のクリーンVMでの確認は未実施。
- 公開済みEXEをダウンロードし、ローカルEXEと公開SHA-256が一致することを確認。SHA-256: `4da6d8b544758696eaeca6d9cd7fc07c931d89f81a86dc62a617cb7440371fdd`。
- 元々の回帰確認はUI状態を設定するテストであり、実際のキー入力・全問題の正誤判定を網羅するものではない。

## 次回の更新

ユーザーが公開を指示した時だけ実行する。自動更新・定期公開は行わない。

1. AppVersion.csとKuiz.csprojのVersionを同じ新しい値にする。
2. `Start-TwoClients.ps1 -Smoke` を実行し、sessionのregression.log末尾の `PASS: WPF regression complete` を確認する。検証中の2つの画面を閉じると失敗する。確認後は表示された検証プロセスだけを終了する。
3. `dotnet run --project Tools/MultiplayerDebug/MultiplayerDebug.csproj -c Release -- --server-url https://kuiz-server.onrender.com` で本番通信確認を行う。
4. `Installer/Build-InnoSetup.ps1` を実行する。毎回Releaseのself-contained x64を作り直し、InstallerにEXEとSHA-256を生成する。
5. 必要なインストール・起動確認を行い、対象ソースをリリース用ブランチにコミット・pushする。既存ユーザーの保存データを壊さない。
6. そのコミットを対象にGitHub Releaseを作成し、EXEとSHA-256をアップロードする。以前のリリースは保持する。
7. `Installer/Prepare-DownloadSite.ps1 -Version <version>` で公開アセットを確認し、同じサイトのrelease.jsonを更新する。
8. sites-building/sites-hostingスキルで同じSiteを開き、DownloadSiteの最新sourceを同期・公開する。新しいSiteを作らない。公開成功のURLを確認する。
9. 公開アセットをダウンロードしてSHA-256を照合する。

WindowsでSitesの配布アーカイブを作る時は、実行プロセスのPATHに `C:\Program Files\Git\bin` を追加し、`TAR_OPTIONS=--force-local` を設定する。bundled site-workflowを使い、認証情報は標準入力だけで渡す。

## 初期問題

Questions/starter-100.jsonは10分野各10問、C目安60問・B目安40問。既存アプリから転載していない。難易度は編集上の目安。
問題の別表記は限定的なので、漢字・読み仮名すべてが正解になるわけではない。

秘密情報・DB接続文字列は、ゲーム、インストーラー、サイト、リリース、ソース管理に含めない。
