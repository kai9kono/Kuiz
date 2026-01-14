namespace Kuiz.Models
{
    public class GameSettings
    {
        public int PointsToWin { get; set; } = 5;
        public int MaxMistakes { get; set; } = 3;
        public int NumQuestions { get; set; } = 10;
        public int RevealIntervalMs { get; set; } = 60;
        public int FastRevealIntervalMs { get; set; } = 15;
        public int AnswerTimeoutSeconds { get; set; } = 10;
    }
}
