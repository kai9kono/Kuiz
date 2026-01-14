-- Railwayデータベースに問題を投入するSQLスクリプト
-- UTF-8エンコーディングで保存してください

-- テーブルが存在しない場合は作成（authorとplayed_atカラムを追加）
CREATE TABLE IF NOT EXISTS questions (
  id serial primary key,
  text text not null,
  answer text not null,
  author text,
  created_at timestamp not null default now(),
  played_at timestamp
);

-- 既存のテーブルにカラムが存在しない場合は追加
ALTER TABLE questions ADD COLUMN IF NOT EXISTS author text;
ALTER TABLE questions ADD COLUMN IF NOT EXISTS created_at timestamp default now();
ALTER TABLE questions ADD COLUMN IF NOT EXISTS played_at timestamp;

-- 既存のcreated_atがNULLの行を更新
UPDATE questions SET created_at = now() WHERE created_at IS NULL;

-- 既存のデータを削除（テスト用）
TRUNCATE TABLE questions RESTART IDENTITY CASCADE;

-- 100問のダミー問題を挿入
BEGIN;

-- 一般知識 (1-20)
INSERT INTO questions (text, answer, author) VALUES ('日本の首都はどこですか？', '東京', 'システム');
INSERT INTO questions (text, answer, author) VALUES ('太陽系で最も大きな惑星は？', '木星', 'システム');
INSERT INTO questions (text, answer, author) VALUES ('1+1は？', '2', 'システム');
INSERT INTO questions (text, answer, author) VALUES ('富士山の標高は約何メートル？', '3776', 'システム');
INSERT INTO questions (text, answer, author) VALUES ('日本で一番大きい湖は？', '琵琶湖', 'システム');
INSERT INTO questions (text, answer, author) VALUES ('オリンピックは何年に一度開催される？', '4年', 'システム');
INSERT INTO questions (text, answer, author) VALUES ('1週間は何日？', '7日', 'システム');
INSERT INTO questions (text, answer, author) VALUES ('虹は何色？', '7色', 'システム');
INSERT INTO questions (text, answer, author) VALUES ('地球の衛星の名前は？', '月', 'システム');
INSERT INTO questions (text, answer, author) VALUES ('日本の国鳥は？', 'キジ', 'システム');
INSERT INTO questions (text, answer, author) VALUES ('1年は何ヶ月？', '12ヶ月', 'システム');
INSERT INTO questions (text, answer, author) VALUES ('光の三原色は？', '赤青緑', 'システム');
INSERT INTO questions (text, answer, author) VALUES ('日本の国花は？', '桜', 'システム');
INSERT INTO questions (text, answer, author) VALUES ('世界最大の海洋は？', '太平洋', 'システム');
INSERT INTO questions (text, answer, author) VALUES ('地球は太陽の周りを何日で回る？', '365日', 'システム');
INSERT INTO questions (text, answer, author) VALUES ('水の化学式は？', 'H2O', 'システム');
INSERT INTO questions (text, answer, author) VALUES ('1時間は何分？', '60分', 'システム');
INSERT INTO questions (text, answer, author) VALUES ('日本の最西端の県は？', '沖縄県', 'システム');
INSERT INTO questions (text, answer, author) VALUES ('円周率πの最初の3桁は？', '3.14', 'システム');
INSERT INTO questions (text, answer, author) VALUES ('金の元素記号は？', 'Au', 'システム');

-- 歴史 (21-35)
INSERT INTO questions (text, answer, author) VALUES ('江戸幕府を開いたのは誰？', '徳川家康', 'システム');
INSERT INTO questions (text, answer, author) VALUES ('鎌倉幕府が成立した年は？', '1192年', 'システム');
INSERT INTO questions (text, answer, author) VALUES ('第二次世界大戦が終わった年は？', '1945年', 'システム');
INSERT INTO questions (text, answer, author) VALUES ('本能寺の変が起きた年は？', '1582年', 'システム');
INSERT INTO questions (text, answer, author) VALUES ('日本で最初の元号は？', '大化', 'システム');
INSERT INTO questions (text, answer, author) VALUES ('明治維新が起きた年は？', '1868年', 'システム');
INSERT INTO questions (text, answer, author) VALUES ('関ヶ原の戦いが起きた年は？', '1600年', 'システム');
INSERT INTO questions (text, answer, author) VALUES ('ペリーが来航した年は？', '1853年', 'システム');
INSERT INTO questions (text, answer, author) VALUES ('大化の改新が起きた年は？', '645年', 'システム');
INSERT INTO questions (text, answer, author) VALUES ('平安京に遷都した天皇は？', '桓武天皇', 'システム');
INSERT INTO questions (text, answer, author) VALUES ('源頼朝が征夷大将軍になった年は？', '1192年', 'システム');
INSERT INTO questions (text, answer, author) VALUES ('室町幕府を開いたのは誰？', '足利尊氏', 'システム');
INSERT INTO questions (text, answer, author) VALUES ('応仁の乱が起きた年は？', '1467年', 'システム');
INSERT INTO questions (text, answer, author) VALUES ('豊臣秀吉が天下統一した年は？', '1590年', 'システム');
INSERT INTO questions (text, answer, author) VALUES ('大政奉還が行われた年は？', '1867年', 'システム');

-- 地理 (36-50)
INSERT INTO questions (text, answer, author) VALUES ('北海道の県庁所在地は？', '札幌', 'システム');
INSERT INTO questions (text, answer, author) VALUES ('大阪府の県庁所在地は？', '大阪', 'システム');
INSERT INTO questions (text, answer, author) VALUES ('世界最長の川は？', 'ナイル川', 'システム');
INSERT INTO questions (text, answer, author) VALUES ('世界最高峰の山は？', 'エベレスト', 'システム');
INSERT INTO questions (text, answer, author) VALUES ('オーストラリアの首都は？', 'キャンベラ', 'システム');
INSERT INTO questions (text, answer, author) VALUES ('アメリカの首都は？', 'ワシントン', 'システム');
INSERT INTO questions (text, answer, author) VALUES ('フランスの首都は？', 'パリ', 'システム');
INSERT INTO questions (text, answer, author) VALUES ('イタリアの首都は？', 'ローマ', 'システム');
INSERT INTO questions (text, answer, author) VALUES ('スペインの首都は？', 'マドリード', 'システム');
INSERT INTO questions (text, answer, author) VALUES ('ドイツの首都は？', 'ベルリン', 'システム');
INSERT INTO questions (text, answer, author) VALUES ('イギリスの首都は？', 'ロンドン', 'システム');
INSERT INTO questions (text, answer, author) VALUES ('ロシアの首都は？', 'モスクワ', 'システム');
INSERT INTO questions (text, answer, author) VALUES ('中国の首都は？', '北京', 'システム');
INSERT INTO questions (text, answer, author) VALUES ('韓国の首都は？', 'ソウル', 'システム');
INSERT INTO questions (text, answer, author) VALUES ('インドの首都は？', 'ニューデリー', 'システム');

-- 科学 (51-65)
INSERT INTO questions (text, answer, author) VALUES ('人間の染色体は何本？', '46本', 'システム');
INSERT INTO questions (text, answer, author) VALUES ('光の速さは秒速約何万キロ？', '30万', 'システム');
INSERT INTO questions (text, answer, author) VALUES ('酸素の元素記号は？', 'O', 'システム');
INSERT INTO questions (text, answer, author) VALUES ('窒素の元素記号は？', 'N', 'システム');
INSERT INTO questions (text, answer, author) VALUES ('炭素の元素記号は？', 'C', 'システム');
INSERT INTO questions (text, answer, author) VALUES ('鉄の元素記号は？', 'Fe', 'システム');
INSERT INTO questions (text, answer, author) VALUES ('銅の元素記号は？', 'Cu', 'システム');
INSERT INTO questions (text, answer, author) VALUES ('銀の元素記号は？', 'Ag', 'システム');
INSERT INTO questions (text, answer, author) VALUES ('水銀の元素記号は？', 'Hg', 'システム');
INSERT INTO questions (text, answer, author) VALUES ('ナトリウムの元素記号は？', 'Na', 'システム');
INSERT INTO questions (text, answer, author) VALUES ('カリウムの元素記号は？', 'K', 'システム');
INSERT INTO questions (text, answer, author) VALUES ('カルシウムの元素記号は？', 'Ca', 'システム');
INSERT INTO questions (text, answer, author) VALUES ('塩化ナトリウムの化学式は？', 'NaCl', 'システム');
INSERT INTO questions (text, answer, author) VALUES ('二酸化炭素の化学式は？', 'CO2', 'システム');
INSERT INTO questions (text, answer, author) VALUES ('アンモニアの化学式は？', 'NH3', 'システム');

-- スポーツ (66-80)
INSERT INTO questions (text, answer, author) VALUES ('サッカーは1チーム何人？', '11人', 'システム');
INSERT INTO questions (text, answer, author) VALUES ('バスケットボールは1チーム何人？', '5人', 'システム');
INSERT INTO questions (text, answer, author) VALUES ('バレーボールは1チーム何人？', '6人', 'システム');
INSERT INTO questions (text, answer, author) VALUES ('野球は1チーム何人？', '9人', 'システム');
INSERT INTO questions (text, answer, author) VALUES ('マラソンは何キロメートル？', '42.195', 'システム');
INSERT INTO questions (text, answer, author) VALUES ('オリンピックの五輪マークは何色？', '5色', 'システム');
INSERT INTO questions (text, answer, author) VALUES ('テニスのグランドスラムは何大会？', '4大会', 'システム');
INSERT INTO questions (text, answer, author) VALUES ('大相撲の最高位は？', '横綱', 'システム');
INSERT INTO questions (text, answer, author) VALUES ('柔道は何級から始まる？', '5級', 'システム');
INSERT INTO questions (text, answer, author) VALUES ('ゴルフのパーとは何？', '基準打数', 'システム');
INSERT INTO questions (text, answer, author) VALUES ('ボウリングのピンは何本？', '10本', 'システム');
INSERT INTO questions (text, answer, author) VALUES ('卓球の球は何色が公式？', '白', 'システム');
INSERT INTO questions (text, answer, author) VALUES ('競泳の4泳法とは？', '自由形背泳平泳バタフライ', 'システム');
INSERT INTO questions (text, answer, author) VALUES ('陸上100mの世界記録保持者は？', 'ボルト', 'システム');
INSERT INTO questions (text, answer, author) VALUES ('サッカーW杯の優勝トロフィー名は？', 'FIFAワールドカップ', 'システム');

-- 文化・芸術 (81-100)
INSERT INTO questions (text, answer, author) VALUES ('モナリザを描いたのは誰？', 'レオナルドダヴィンチ', 'システム');
INSERT INTO questions (text, answer, author) VALUES ('ベートーヴェンの交響曲第9番の通称は？', '第九', 'システム');
INSERT INTO questions (text, answer, author) VALUES ('源氏物語の作者は？', '紫式部', 'システム');
INSERT INTO questions (text, answer, author) VALUES ('枕草子の作者は？', '清少納言', 'システム');
INSERT INTO questions (text, answer, author) VALUES ('竹取物語の主人公は？', 'かぐや姫', 'システム');
INSERT INTO questions (text, answer, author) VALUES ('夏目漱石の代表作は？', '吾輩は猫である', 'システム');
INSERT INTO questions (text, answer, author) VALUES ('芥川龍之介の代表作は？', '羅生門', 'システム');
INSERT INTO questions (text, answer, author) VALUES ('太宰治の代表作は？', '人間失格', 'システム');
INSERT INTO questions (text, answer, author) VALUES ('川端康成の代表作は？', '雪国', 'システム');
INSERT INTO questions (text, answer, author) VALUES ('村上春樹の代表作は？', 'ノルウェイの森', 'システム');
INSERT INTO questions (text, answer, author) VALUES ('百人一首を編纂したのは誰？', '藤原定家', 'システム');
INSERT INTO questions (text, answer, author) VALUES ('古今和歌集を編纂したのは誰？', '紀貫之', 'システム');
INSERT INTO questions (text, answer, author) VALUES ('万葉集の時代は？', '奈良時代', 'システム');
INSERT INTO questions (text, answer, author) VALUES ('能を大成したのは誰？', '世阿弥', 'システム');
INSERT INTO questions (text, answer, author) VALUES ('歌舞伎を創始したのは誰？', '出雲の阿国', 'システム');
INSERT INTO questions (text, answer, author) VALUES ('浮世絵の代表的絵師は？', '葛飾北斎', 'システム');
INSERT INTO questions (text, answer, author) VALUES ('富嶽三十六景を描いたのは誰？', '葛飾北斎', 'システム');
INSERT INTO questions (text, answer, author) VALUES ('俳句の季語は何を表す？', '季節', 'システム');
INSERT INTO questions (text, answer, author) VALUES ('俳句は何音？', '17音', 'システム');
INSERT INTO questions (text, answer, author) VALUES ('短歌は何音？', '31音', 'システム');

COMMIT;

-- 問題数を確認
SELECT COUNT(*) as total_questions FROM questions;
SELECT '? 問題データを正常に挿入しました！' as status;
