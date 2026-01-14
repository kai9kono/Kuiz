namespace KuizServer.Models;

public class Lobby
{
    public string LobbyCode { get; set; } = string.Empty;
    public string HostName { get; set; } = string.Empty;
    public List<string> Players { get; set; } = new();
    public int MaxPlayers { get; set; } = 4;
    public GameSettings? Settings { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public LobbyStatus Status { get; set; } = LobbyStatus.Waiting;
}

public enum LobbyStatus
{
    Waiting,
    InGame,
    Finished
}

public class GameSettings
{
    public int PointsToWin { get; set; } = 5;
    public int MaxMistakes { get; set; } = 3;
    public int NumQuestions { get; set; } = 10;
}

public class GameRoom
{
    public string LobbyCode { get; set; } = string.Empty;
    public Dictionary<string, int> Scores { get; set; } = new();
    public Dictionary<string, int> Mistakes { get; set; } = new();
    public List<string> BuzzOrder { get; set; } = new();
    public int CurrentQuestionIndex { get; set; } = 0;
    public string CurrentQuestion { get; set; } = string.Empty;
    public string CurrentAnswer { get; set; } = string.Empty;
    public bool IsWaitingForAnswer { get; set; } = false;
    public int SessionId { get; set; } = 0;
}
