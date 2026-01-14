namespace KuizServer.Models;

public class Question
{
    public int Id { get; set; }
    public required string Text { get; set; }
    public required string Answer { get; set; }
    public string? Author { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? PlayedAt { get; set; }
}

public class QuestionDto
{
    public int Id { get; set; }
    public required string Text { get; set; }
    public required string Answer { get; set; }
    public string? Author { get; set; }
}

public class CreateQuestionDto
{
    public required string Text { get; set; }
    public required string Answer { get; set; }
    public string? Author { get; set; }
}
