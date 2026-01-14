using System;
using System.IO;
using System.Text.Json;
using Kuiz.Models;

namespace Kuiz.Services
{
    /// <summary>
    /// プレイヤーの統計情報を管理するサービス
    /// </summary>
    public class PlayerStatsService
    {
        private const string StatsFileName = "player_stats.json";
        private static readonly string StatsFilePath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Kuiz",
            StatsFileName
        );

        private PlayerStats _stats;

        public PlayerStatsService()
        {
            _stats = new PlayerStats();
            Load();
        }

        /// <summary>
        /// 現在の統計情報を取得
        /// </summary>
        public PlayerStats GetStats()
        {
            return _stats;
        }

        /// <summary>
        /// ゲーム開始時に呼ばれる
        /// </summary>
        public void OnGameStarted()
        {
            _stats.TotalGamesPlayed++;
            Save();
            Logger.LogInfo($"Game started. Total games: {_stats.TotalGamesPlayed}");
        }

        /// <summary>
        /// 正解時に呼ばれる
        /// </summary>
        public void OnCorrectAnswer()
        {
            _stats.TotalCorrectAnswers++;
            Save();
            Logger.LogInfo($"Correct answer. Total correct: {_stats.TotalCorrectAnswers}");
        }

        /// <summary>
        /// ミス時に呼ばれる
        /// </summary>
        public void OnMistake()
        {
            _stats.TotalMistakes++;
            Save();
            Logger.LogInfo($"Mistake. Total mistakes: {_stats.TotalMistakes}");
        }

        /// <summary>
        /// 勝利時に呼ばれる
        /// </summary>
        public void OnWin()
        {
            _stats.TotalWins++;
            Save();
            Logger.LogInfo($"Win! Total wins: {_stats.TotalWins}");
        }

        /// <summary>
        /// 統計情報をファイルから読み込む
        /// </summary>
        private void Load()
        {
            try
            {
                if (!File.Exists(StatsFilePath))
                {
                    Logger.LogInfo("Stats file not found, using default stats");
                    return;
                }

                var json = File.ReadAllText(StatsFilePath);
                var loadedStats = JsonSerializer.Deserialize<PlayerStats>(json);
                
                if (loadedStats != null)
                {
                    _stats = loadedStats;
                    Logger.LogInfo($"Loaded player stats: Games={_stats.TotalGamesPlayed}, Correct={_stats.TotalCorrectAnswers}, Wins={_stats.TotalWins}");
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex);
            }
        }

        /// <summary>
        /// 統計情報をファイルに保存
        /// </summary>
        private void Save()
        {
            try
            {
                var dir = Path.GetDirectoryName(StatsFilePath);
                if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                {
                    Directory.CreateDirectory(dir);
                }

                var json = JsonSerializer.Serialize(_stats, new JsonSerializerOptions
                {
                    WriteIndented = true
                });
                
                File.WriteAllText(StatsFilePath, json);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex);
            }
        }

        /// <summary>
        /// 統計情報をリセット
        /// </summary>
        public void Reset()
        {
            _stats = new PlayerStats();
            Save();
            Logger.LogInfo("Player stats reset");
        }
    }
}
