-- Create table if not exists (safe to run multiple times)
CREATE TABLE IF NOT EXISTS questions (
  id serial primary key,
  text text not null,
  answer text not null
);

-- Insert 100 dummy questions
BEGIN;

INSERT INTO questions (text, answer) VALUES ('ダミー問題 1: これはサンプルの問題文です。', '答え1');
INSERT INTO questions (text, answer) VALUES ('ダミー問題 2: これはサンプルの問題文です。', '答え2');
INSERT INTO questions (text, answer) VALUES ('ダミー問題 3: これはサンプルの問題文です。', '答え3');
INSERT INTO questions (text, answer) VALUES ('ダミー問題 4: これはサンプルの問題文です。', '答え4');
INSERT INTO questions (text, answer) VALUES ('ダミー問題 5: これはサンプルの問題文です。', '答え5');
INSERT INTO questions (text, answer) VALUES ('ダミー問題 6: これはサンプルの問題文です。', '答え6');
INSERT INTO questions (text, answer) VALUES ('ダミー問題 7: これはサンプルの問題文です。', '答え7');
INSERT INTO questions (text, answer) VALUES ('ダミー問題 8: これはサンプルの問題文です。', '答え8');
INSERT INTO questions (text, answer) VALUES ('ダミー問題 9: これはサンプルの問題文です。', '答え9');
INSERT INTO questions (text, answer) VALUES ('ダミー問題 10: これはサンプルの問題文です。', '答え10');

INSERT INTO questions (text, answer) VALUES ('ダミー問題 11: これはサンプルの問題文です。', '答え11');
INSERT INTO questions (text, answer) VALUES ('ダミー問題 12: これはサンプルの問題文です。', '答え12');
INSERT INTO questions (text, answer) VALUES ('ダミー問題 13: これはサンプルの問題文です。', '答え13');
INSERT INTO questions (text, answer) VALUES ('ダミー問題 14: これはサンプルの問題文です。', '答え14');
INSERT INTO questions (text, answer) VALUES ('ダミー問題 15: これはサンプルの問題文です。', '答え15');
INSERT INTO questions (text, answer) VALUES ('ダミー問題 16: これはサンプルの問題文です。', '答え16');
INSERT INTO questions (text, answer) VALUES ('ダミー問題 17: これはサンプルの問題文です。', '答え17');
INSERT INTO questions (text, answer) VALUES ('ダミー問題 18: これはサンプルの問題文です。', '答え18');
INSERT INTO questions (text, answer) VALUES ('ダミー問題 19: これはサンプルの問題文です。', '答え19');
INSERT INTO questions (text, answer) VALUES ('ダミー問題 20: これはサンプルの問題文です。', '答え20');

INSERT INTO questions (text, answer) VALUES ('ダミー問題 21: これはサンプルの問題文です。', '答え21');
INSERT INTO questions (text, answer) VALUES ('ダミー問題 22: これはサンプルの問題文です。', '答え22');
INSERT INTO questions (text, answer) VALUES ('ダミー問題 23: これはサンプルの問題文です。', '答え23');
INSERT INTO questions (text, answer) VALUES ('ダミー問題 24: これはサンプルの問題文です。', '答え24');
INSERT INTO questions (text, answer) VALUES ('ダミー問題 25: これはサンプルの問題文です。', '答え25');
INSERT INTO questions (text, answer) VALUES ('ダミー問題 26: これはサンプルの問題文です。', '答え26');
INSERT INTO questions (text, answer) VALUES ('ダミー問題 27: これはサンプルの問題文です。', '答え27');
INSERT INTO questions (text, answer) VALUES ('ダミー問題 28: これはサンプルの問題文です。', '答え28');
INSERT INTO questions (text, answer) VALUES ('ダミー問題 29: これはサンプルの問題文です。', '答え29');
INSERT INTO questions (text, answer) VALUES ('ダミー問題 30: これはサンプルの問題文です。', '答え30');

INSERT INTO questions (text, answer) VALUES ('ダミー問題 31: これはサンプルの問題文です。', '答え31');
INSERT INTO questions (text, answer) VALUES ('ダミー問題 32: これはサンプルの問題文です。', '答え32');
INSERT INTO questions (text, answer) VALUES ('ダミー問題 33: これはサンプルの問題文です。', '答え33');
INSERT INTO questions (text, answer) VALUES ('ダミー問題 34: これはサンプルの問題文です。', '答え34');
INSERT INTO questions (text, answer) VALUES ('ダミー問題 35: これはサンプルの問題文です。', '答え35');
INSERT INTO questions (text, answer) VALUES ('ダミー問題 36: これはサンプルの問題文です。', '答え36');
INSERT INTO questions (text, answer) VALUES ('ダミー問題 37: これはサンプルの問題文です。', '答え37');
INSERT INTO questions (text, answer) VALUES ('ダミー問題 38: これはサンプルの問題文です。', '答え38');
INSERT INTO questions (text, answer) VALUES ('ダミー問題 39: これはサンプルの問題文です。', '答え39');
INSERT INTO questions (text, answer) VALUES ('ダミー問題 40: これはサンプルの問題文です。', '答え40');

INSERT INTO questions (text, answer) VALUES ('ダミー問題 41: これはサンプルの問題文です。', '答え41');
INSERT INTO questions (text, answer) VALUES ('ダミー問題 42: これはサンプルの問題文です。', '答え42');
INSERT INTO questions (text, answer) VALUES ('ダミー問題 43: これはサンプルの問題文です。', '答え43');
INSERT INTO questions (text, answer) VALUES ('ダミー問題 44: これはサンプルの問題文です。', '答え44');
INSERT INTO questions (text, answer) VALUES ('ダミー問題 45: これはサンプルの問題文です。', '答え45');
INSERT INTO questions (text, answer) VALUES ('ダミー問題 46: これはサンプルの問題文です。', '答え46');
INSERT INTO questions (text, answer) VALUES ('ダミー問題 47: これはサンプルの問題文です。', '答え47');
INSERT INTO questions (text, answer) VALUES ('ダミー問題 48: これはサンプルの問題文です。', '答え48');
INSERT INTO questions (text, answer) VALUES ('ダミー問題 49: これはサンプルの問題文です。', '答え49');
INSERT INTO questions (text, answer) VALUES ('ダミー問題 50: これはサンプルの問題文です。', '答え50');

INSERT INTO questions (text, answer) VALUES ('ダミー問題 51: これはサンプルの問題文です。', '答え51');
INSERT INTO questions (text, answer) VALUES ('ダミー問題 52: これはサンプルの問題文です。', '答え52');
INSERT INTO questions (text, answer) VALUES ('ダミー問題 53: これはサンプルの問題文です。', '答え53');
INSERT INTO questions (text, answer) VALUES ('ダミー問題 54: これはサンプルの問題文です。', '答え54');
INSERT INTO questions (text, answer) VALUES ('ダミー問題 55: これはサンプルの問題文です。', '答え55');
INSERT INTO questions (text, answer) VALUES ('ダミー問題 56: これはサンプルの問題文です。', '答え56');
INSERT INTO questions (text, answer) VALUES ('ダミー問題 57: これはサンプルの問題文です。', '答え57');
INSERT INTO questions (text, answer) VALUES ('ダミー問題 58: これはサンプルの問題文です。', '答え58');
INSERT INTO questions (text, answer) VALUES ('ダミー問題 59: これはサンプルの問題文です。', '答え59');
INSERT INTO questions (text, answer) VALUES ('ダミー問題 60: これはサンプルの問題文です。', '答え60');

INSERT INTO questions (text, answer) VALUES ('ダミー問題 61: これはサンプルの問題文です。', '答え61');
INSERT INTO questions (text, answer) VALUES ('ダミー問題 62: これはサンプルの問題文です。', '答え62');
INSERT INTO questions (text, answer) VALUES ('ダミー問題 63: これはサンプルの問題文です。', '答え63');
INSERT INTO questions (text, answer) VALUES ('ダミー問題 64: これはサンプルの問題文です。', '答え64');
INSERT INTO questions (text, answer) VALUES ('ダミー問題 65: これはサンプルの問題文です。', '答え65');
INSERT INTO questions (text, answer) VALUES ('ダミー問題 66: これはサンプルの問題文です。', '答え66');
INSERT INTO questions (text, answer) VALUES ('ダミー問題 67: これはサンプルの問題文です。', '答え67');
INSERT INTO questions (text, answer) VALUES ('ダミー問題 68: これはサンプルの問題文です。', '答え68');
INSERT INTO questions (text, answer) VALUES ('ダミー問題 69: これはサンプルの問題文です。', '答え69');
INSERT INTO questions (text, answer) VALUES ('ダミー問題 70: これはサンプルの問題文です。', '答え70');

INSERT INTO questions (text, answer) VALUES ('ダミー問題 71: これはサンプルの問題文です。', '答え71');
INSERT INTO questions (text, answer) VALUES ('ダミー問題 72: これはサンプルの問題文です。', '答え72');
INSERT INTO questions (text, answer) VALUES ('ダミー問題 73: これはサンプルの問題文です。', '答え73');
INSERT INTO questions (text, answer) VALUES ('ダミー問題 74: これはサンプルの問題文です。', '答え74');
INSERT INTO questions (text, answer) VALUES ('ダミー問題 75: これはサンプルの問題文です。', '答え75');
INSERT INTO questions (text, answer) VALUES ('ダミー問題 76: これはサンプルの問題文です。', '答え76');
INSERT INTO questions (text, answer) VALUES ('ダミー問題 77: これはサンプルの問題文です。', '答え77');
INSERT INTO questions (text, answer) VALUES ('ダミー問題 78: これはサンプルの問題文です。', '答え78');
INSERT INTO questions (text, answer) VALUES ('ダミー問題 79: これはサンプルの問題文です。', '答え79');
INSERT INTO questions (text, answer) VALUES ('ダミー問題 80: これはサンプルの問題文です。', '答え80');

INSERT INTO questions (text, answer) VALUES ('ダミー問題 81: これはサンプルの問題文です。', '答え81');
INSERT INTO questions (text, answer) VALUES ('ダミー問題 82: これはサンプルの問題文です。', '答え82');
INSERT INTO questions (text, answer) VALUES ('ダミー問題 83: これはサンプルの問題文です。', '答え83');
INSERT INTO questions (text, answer) VALUES ('ダミー問題 84: これはサンプルの問題文です。', '答え84');
INSERT INTO questions (text, answer) VALUES ('ダミー問題 85: これはサンプルの問題文です。', '答え85');
INSERT INTO questions (text, answer) VALUES ('ダミー問題 86: これはサンプルの問題文です。', '答え86');
INSERT INTO questions (text, answer) VALUES ('ダミー問題 87: これはサンプルの問題文です。', '答え87');
INSERT INTO questions (text, answer) VALUES ('ダミー問題 88: これはサンプルの問題文です。', '答え88');
INSERT INTO questions (text, answer) VALUES ('ダミー問題 89: これはサンプルの問題文です。', '答え89');
INSERT INTO questions (text, answer) VALUES ('ダミー問題 90: これはサンプルの問題文です。', '答え90');

INSERT INTO questions (text, answer) VALUES ('ダミー問題 91: これはサンプルの問題文です。', '答え91');
INSERT INTO questions (text, answer) VALUES ('ダミー問題 92: これはサンプルの問題文です。', '答え92');
INSERT INTO questions (text, answer) VALUES ('ダミー問題 93: これはサンプルの問題文です。', '答え93');
INSERT INTO questions (text, answer) VALUES ('ダミー問題 94: これはサンプルの問題文です。', '答え94');
INSERT INTO questions (text, answer) VALUES ('ダミー問題 95: これはサンプルの問題文です。', '答え95');
INSERT INTO questions (text, answer) VALUES ('ダミー問題 96: これはサンプルの問題文です。', '答え96');
INSERT INTO questions (text, answer) VALUES ('ダミー問題 97: これはサンプルの問題文です。', '答え97');
INSERT INTO questions (text, answer) VALUES ('ダミー問題 98: これはサンプルの問題文です。', '答え98');
INSERT INTO questions (text, answer) VALUES ('ダミー問題 99: これはサンプルの問題文です。', '答え99');
INSERT INTO questions (text, answer) VALUES ('ダミー問題 100: これはサンプルの問題文です。', '答え100');

COMMIT;
