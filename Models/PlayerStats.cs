namespace Kuiz.Models
{
    /// <summary>
    /// プレイヤーの統計情報
    /// </summary>
    public class PlayerStats
    {
        /// <summary>
        /// 総プレイ数
        /// </summary>
        public int TotalGamesPlayed { get; set; }

        /// <summary>
        /// 総正解数
        /// </summary>
        public int TotalCorrectAnswers { get; set; }

        /// <summary>
        /// 総勝利数
        /// </summary>
        public int TotalWins { get; set; }

        /// <summary>
        /// 総ミス数
        /// </summary>
        public int TotalMistakes { get; set; }

        /// <summary>
        /// 正解率（パーセンテージ）
        /// </summary>
        public double CorrectRate
        {
            get
            {
                var totalAnswers = TotalCorrectAnswers + TotalMistakes;
                return totalAnswers > 0 ? (double)TotalCorrectAnswers / totalAnswers * 100.0 : 0.0;
            }
        }

        /// <summary>
        /// 勝率（パーセンテージ）
        /// </summary>
        public double WinRate
        {
            get
            {
                return TotalGamesPlayed > 0 ? (double)TotalWins / TotalGamesPlayed * 100.0 : 0.0;
            }
        }
    }
}
