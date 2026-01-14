using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Media;
using FuzzySharp;
using Kuiz.Models;

namespace Kuiz.Services
{
    /// <summary>
    /// ÉQÅ[ÉÄèÛë‘ÇÃä«óùÇíSìñ
    /// </summary>
    public class GameStateService
    {
        private readonly Random _rand = new();
        private readonly List<Color> _colorPalette = new()
        {
            (Color)ColorConverter.ConvertFromString("#FFD8BFD8"), // pastel lilac
            (Color)ColorConverter.ConvertFromString("#FFFFD1A8"), // pastel peach
            (Color)ColorConverter.ConvertFromString("#FFC8E6C9"), // pastel mint
            (Color)ColorConverter.ConvertFromString("#FFFFF9C4"), // pastel yellow
            (Color)ColorConverter.ConvertFromString("#FFBBDEFB"), // pastel blue
            (Color)ColorConverter.ConvertFromString("#FFFFCDD2"), // pastel pink
            (Color)ColorConverter.ConvertFromString("#FFE1BEE7"), // pastel mauve
            (Color)ColorConverter.ConvertFromString("#FFD7CCC8")  // pastel beige
        };

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
            return true;
        }

        /// <summary>
        /// ëSäpîºäpÅEëÂï∂éöè¨ï∂éöÇê≥ãKâª
        /// </summary>
        private string NormalizeAnswer(string text)
        {
            if (string.IsNullOrEmpty(text)) return "";
            
            var normalized = text.Trim();
            
            // ëSäpâpêîéöÇîºäpÇ…ïœä∑
            var sb = new System.Text.StringBuilder();
            foreach (char c in normalized)
            {
                // ëSäpâpëÂï∂éö (Ç`-Çy) Å® îºäpè¨ï∂éö (a-z)
                if (c >= 'ÇO' && c <= 'ÇX')
                {
                    sb.Append((char)(c - 'ÇO' + '0'));
                }
                else if (c >= 'Ç`' && c <= 'Çy')
                {
                    sb.Append((char)(c - 'Ç`' + 'a'));
                }
                else if (c >= 'ÇÅ' && c <= 'Çö')
                {
                    sb.Append((char)(c - 'ÇÅ' + 'a'));
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

            // ëSäpîºäpÅEëÂï∂éöè¨ï∂éöÇê≥ãKâª
            var correctAnswer = NormalizeAnswer(CurrentQuestion.Answer ?? "");
            var userAnswer = NormalizeAnswer(answer);
            
            // Ç‹Ç∏äÆëSàÍívÇämîFÅiê≥ãKâªå„Åj
            bool correct = correctAnswer == userAnswer;
            
            // äÆëSàÍívÇµÇ»Ç¢èÍçáÇÕÉtÉ@ÉWÅ[É}ÉbÉ`ÉìÉO
            if (!correct && !string.IsNullOrEmpty(correctAnswer))
            {
                int similarity = Fuzz.Ratio(correctAnswer, userAnswer);
                Logger.LogInfo($"Answer fuzzy match: '{answer}' -> '{userAnswer}' vs '{CurrentQuestion.Answer}' -> '{correctAnswer}' = {similarity}%");
                
                // 85%à»è„ÇÃóﬁéóìxÇ≈ê≥âÇ∆îªíË
                correct = similarity >= 85;
            }

            if (correct)
            {
                if (!Scores.ContainsKey(playerName)) Scores[playerName] = 0;
                Scores[playerName]++;
                CorrectAnswered = true;
                LastCorrectPlayer = playerName;
                FastReveal = true;
            }
            else
            {
                if (!Mistakes.ContainsKey(playerName)) Mistakes[playerName] = 0;
                Mistakes[playerName]++;
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
            if (string.IsNullOrEmpty(name)) return Brushes.Gray;
            if (PlayerColorBrushes.ContainsKey(name)) return PlayerColorBrushes[name];

            // Hidden feature: players with 'Ç ' in their name get gold color
            if (name.Contains("Ç "))
            {
                var goldBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFD700"));
                goldBrush.Freeze();
                PlayerColorBrushes[name] = goldBrush;
                return goldBrush;
            }

            var usedColors = new HashSet<Color>(
                PlayerColorBrushes.Values
                    .OfType<SolidColorBrush>()
                    .Select(b => b.Color));

            Color chosen;
            var available = _colorPalette.Where(c => !usedColors.Contains(c)).ToList();
            if (available.Count > 0)
            {
                chosen = available[_rand.Next(available.Count)];
            }
            else
            {
                chosen = _colorPalette[_rand.Next(_colorPalette.Count)];
            }

            var brush = new SolidColorBrush(chosen);
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
                ColorBrush = PlayerColorBrushes.GetValueOrDefault(p) ?? EnsurePlayerColor(p),
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
