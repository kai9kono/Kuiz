using System.Collections.Concurrent;

namespace KuizServer.Services;

public class GameRoomService
{
    private readonly ConcurrentDictionary<string, GameRoom> _gameRooms = new();

    public GameRoom? GetOrCreateGameRoom(string lobbyCode)
    {
        return _gameRooms.GetOrAdd(lobbyCode, code => new GameRoom { LobbyCode = code });
    }

    public GameRoom? GetGameRoom(string lobbyCode)
    {
        _gameRooms.TryGetValue(lobbyCode, out var room);
        return room;
    }

    public void RemoveGameRoom(string lobbyCode)
    {
        _gameRooms.TryRemove(lobbyCode, out _);
    }
}

public class GameRoom
{
    public required string LobbyCode { get; set; }
    public Dictionary<string, int> Scores { get; set; } = new();
    public Dictionary<string, int> Mistakes { get; set; } = new();
    public int CurrentQuestionIndex { get; set; } = 0;
    public List<string> BuzzOrder { get; set; } = new();
    public DateTime? GameStartedAt { get; set; }
}
