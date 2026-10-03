# 初期問題100問

Kuiz用に作成したオリジナル問題です。既存アプリの問題文を転載していません。
10分野各10問、C目安60問とB目安40問です。難易度は一般知識と定番クイズをもとにした編集上の目安で、みんはやの正答率による公式分類ではありません。

`starter-100.json` は問題管理画面のJSONインポート形式です。CategoryとDifficultyは編集用の情報で、現在のゲームは全問を混ぜて出題します。
回答はAnswer欄の表記を基準にします。漢字と読み仮名などの別表記をすべて許容する仕組みはありません。

部分確認に用いた参考資料:
- https://www.nhkso.or.jp/concert/202602C.html （展覧会の絵）
- https://www.city.joetsu.niigata.jp/soshiki/koubunsho/tenji23.html （芭蕉と曾良）
- https://abc-magazine.asahi.co.jp/post-19651/ （カルボナーラの語源）

登録先はRenderのQuestion APIが接続している既存のNeon PostgreSQLです。APIから100件を登録し、再取得した問題文と答えを全件照合します。更新時は既存問題文の一致を確認し、同じ問題を重複登録しないでください。
