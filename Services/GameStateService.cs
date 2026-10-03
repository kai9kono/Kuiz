using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Windows.Media;
using FuzzySharp;
using Kuiz.Models;

namespace Kuiz.Services
{
    /// <summary>
    /// ゲーム状態の管理を担当
    /// </summary>
    public class GameStateService
    {
        private readonly Random _rand = new();

        // Player state
        public List<string> LobbyPlayers { get; } = new();
        public Dictionary<string, Brush> PlayerColorBrushes { get; } = new();
        public Dictionary<string, int> Scores { get; } = new();
        public Dictionary<string, int> Mistakes { get; } = new();
        public List<string> BuzzOrder { get; } = new();
        public HashSet<string> AttemptedThisQuestion { get; } = new();

        // Configuration
        public int MaxMistakes { get; set; } = 3;
        public int PointsToWin { get; set; } = 5;
        public int SessionId { get; set; }

        // Question state
        public Queue<Question> PlayQueue { get; private set; } = new();
        public Question? CurrentQuestion { get; private set; }
        public int QueuePosition { get; set; } = -1;
        public int TotalQuestions { get; private set; } = 0;

        // Reveal state
        public int RevealIndex { get; set; }
        public string RevealedText { get; set; } = "";
        public bool PausedForBuzz { get; set; }
        public bool FastReveal { get; set; }
        public bool CorrectAnswered { get; set; }
        public string? LastCorrectPlayer { get; set; }
        public DateTime? RevealCompletedTime { get; set; }

        public void Reset()
        {
            LobbyPlayers.Clear();
            Scores.Clear();
            Mistakes.Clear();
            BuzzOrder.Clear();
            AttemptedThisQuestion.Clear();
            PlayQueue = new Queue<Question>();
            CurrentQuestion = null;
            QueuePosition = -1;
            ResetQuestionState();
        }

        public void ResetQuestionState()
        {
            RevealIndex = 0;
            RevealedText = "";
            PausedForBuzz = false;
            FastReveal = false;
            CorrectAnswered = false;
            LastCorrectPlayer = null;
            RevealCompletedTime = null;
            AttemptedThisQuestion.Clear();
            BuzzOrder.Clear();
        }

        public void InitializeScores()
        {
            Scores.Clear();
            Mistakes.Clear();
            foreach (var player in LobbyPlayers)
            {
                Scores[player] = 0;
                Mistakes[player] = 0;
            }
        }

        public void SetupPlayQueue(List<Question> questions, int count)
        {
            var shuffled = questions.OrderBy(_ => _rand.Next()).Take(Math.Min(count, questions.Count)).ToList();
            PlayQueue = new Queue<Question>(shuffled);
            TotalQuestions = PlayQueue.Count;
        }

        public Question? DequeueNextQuestion()
        {
            if (PlayQueue.Count == 0)
            {
                CurrentQuestion = null;
                return null;
            }

            CurrentQuestion = PlayQueue.Dequeue();
            ResetQuestionState();
            return CurrentQuestion;
        }

        public bool ProcessBuzz(string playerName)
        {
            if (PausedForBuzz || BuzzOrder.Count > 0 || AttemptedThisQuestion.Contains(playerName))
            {
                return false;
            }

            // Prevent disabled players from buzzing
            if (Mistakes.GetValueOrDefault(playerName, 0) >= MaxMistakes)
            {
                return false;
            }

            BuzzOrder.Add(playerName);
            AttemptedThisQuestion.Add(playerName);
            PausedForBuzz = true;
            Logger.LogInfo($"? Buzz accepted from {playerName}");
            return true;
        }

        /// <summary>
        /// 全角半角・大文字小文字を正規化
        /// </summary>
        private string NormalizeAnswer(string text)
        {
            if (string.IsNullOrEmpty(text)) return "";
            
            var normalized = text.Trim();
            
            // 全角英数字を半角に変換
            var sb = new System.Text.StringBuilder();
            foreach (char c in normalized)
            {
                // 全角英大文字 (Ａ-Ｚ) → 半角小文字 (a-z)
                if (c >= '０' && c <= '９')
                {
                    sb.Append((char)(c - '０' + '0'));
                }
                else if (c >= 'Ａ' && c <= 'Ｚ')
                {
                    sb.Append((char)(c - 'Ａ' + 'a'));
                }
                else if (c >= 'ａ' && c <= 'ｚ')
                {
                    sb.Append((char)(c - 'ａ' + 'a'));
                }
                else if (c >= 'A' && c <= 'Z')
                {
                    sb.Append((char)(c - 'A' + 'a'));
                }
                else
                {
                    sb.Append(char.ToLower(c));
                }
            }
            
            return sb.ToString();
        }

        public bool ProcessAnswer(string playerName, string answer)
        {
            if (CurrentQuestion == null) return false;

            // 全角半角・大文字小文字を正規化
            var correctAnswer = NormalizeAnswer(CurrentQuestion.Answer ?? "");
            var userAnswer = NormalizeAnswer(answer);
            
            // まず完全一致を確認（正規化後）
            bool correct = correctAnswer == userAnswer;
            
            // 完全一致しない場合はファジーマッチング
            if (!correct && !string.IsNullOrEmpty(correctAnswer))
            {
                int similarity = Fuzz.Ratio(correctAnswer, userAnswer);
                Logger.LogInfo($"Answer fuzzy match: '{answer}' -> '{userAnswer}' vs '{CurrentQuestion.Answer}' -> '{correctAnswer}' = {similarity}%");
                
                // 85%以上の類似度で正解と判定
                correct = similarity >= 85;
            }

            if (correct)
            {
                Logger.LogInfo($"? Correct answer from {playerName}");
                if (!Scores.ContainsKey(playerName)) Scores[playerName] = 0;
                Scores[playerName]++;
                CorrectAnswered = true;
                LastCorrectPlayer = playerName;
                FastReveal = true;
                BuzzOrder.Clear();
                PausedForBuzz = false;
            }
            else
            {
                Logger.LogInfo($"? Incorrect answer from {playerName}: '{answer}'");
                
                // Count mistake for incorrect answer (not timeout)
                if (!Mistakes.ContainsKey(playerName)) Mistakes[playerName] = 0;
                Mistakes[playerName]++;
                Logger.LogInfo($"   {playerName} now has {Mistakes[playerName]} mistake(s)");
                
                BuzzOrder.Clear();
                PausedForBuzz = false;
            }

            return correct;
        }

        public void AddPlayer(string name)
        {
            if (!LobbyPlayers.Contains(name))
            {
                LobbyPlayers.Add(name);
                EnsurePlayerColor(name);
            }
        }

        public Brush EnsurePlayerColor(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) return Brushes.Gray;
            // Unlike Random or string.GetHashCode(), SHA-256 is stable across
            // processes. Join order, machine and host/guest role cannot change it.
            var hash = SHA256.HashData(Encoding.UTF8.GetBytes(name.Trim().ToUpperInvariant()));
            var hue = ((hash[0] << 8) | hash[1]) / 65536.0 * 6;
            const double saturation = 0.62;
            const double lightness = 0.64;
            var chroma = (1 - Math.Abs(2 * lightness - 1)) * saturation;
            var secondary = chroma * (1 - Math.Abs(hue % 2 - 1));
            var offset = lightness - chroma / 2;
            var (red, green, blue) = (int)hue switch
            {
                0 => (chroma, secondary, 0.0),
                1 => (secondary, chroma, 0.0),
                2 => (0.0, chroma, secondary),
                3 => (0.0, secondary, chroma),
                4 => (secondary, 0.0, chroma),
                _ => (chroma, 0.0, secondary)
            };
            var color = Color.FromRgb((byte)Math.Round((red + offset) * 255),
                (byte)Math.Round((green + offset) * 255), (byte)Math.Round((blue + offset) * 255));
            if (PlayerColorBrushes.TryGetValue(name, out var cached) &&
                cached is SolidColorBrush existing && existing.Color == color) return cached;
            var brush = new SolidColorBrush(color);
            brush.Freeze();
            PlayerColorBrushes[name] = brush;
            return brush;
        }

        public string? GetWinner()
        {
            return Scores.OrderByDescending(kv => kv.Value).FirstOrDefault().Key;
        }

        public List<PlayerState> GetPlayerStates()
        {
            return LobbyPlayers.Select(p => new PlayerState
            {
                Name = p,
                Score = Scores.GetValueOrDefault(p, 0),
                Correct = Scores.GetValueOrDefault(p, 0),
                Wrong = Mistakes.GetValueOrDefault(p, 0),
                ColorBrush = EnsurePlayerColor(p),
                IsDisabled = Mistakes.GetValueOrDefault(p, 0) >= MaxMistakes
            }).ToList();
        }

        public bool EvaluateGameEnd(out string? winner, out bool allDisqualified)
        {
            winner = GetWinner();

            if (winner != null && Scores.GetValueOrDefault(winner, 0) >= PointsToWin)
            {
                allDisqualified = false;
                return true;
            }

            allDisqualified = LobbyPlayers.Count > 0 && LobbyPlayers.All(p => Mistakes.GetValueOrDefault(p, 0) >= MaxMistakes);
            if (allDisqualified)
            {
                return true;
            }

            winner = GetWinner();
            return false;
        }
    }
}
