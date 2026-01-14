using System;

namespace Kuiz.Models
{
    /// <summary>
    /// o‘è—š—ğƒGƒ“ƒgƒŠ
    /// </summary>
    public class QuestionHistoryEntry
    {
        public int QuestionId { get; set; }
        public string Text { get; set; } = "";
        public string Answer { get; set; } = "";
        public string Author { get; set; } = "";
        public DateTime PlayedAt { get; set; }
    }
}
