namespace Kuiz.Models
{
    public class Question
    {
        public int Id { get; set; }
        public string Text { get; set; } = "";
        public string Answer { get; set; } = "";
        public string Author { get; set; } = "";
    }
}
