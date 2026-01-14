using System.Collections.Concurrent;
using KuizServer.Models;

namespace KuizServer.Services;

public class LobbyService
{
    private readonly ConcurrentDictionary<string, Lobby> _lobbies = new();
    private readonly Random _random = new();

    public string CreateLobby(string hostName)
    {
        var code = GenerateLobbyCode();
        var lobby = new Lobby
        {
            LobbyCode = code,
            HostName = hostName,
            Players = new List<string> { hostName },
            Settings = new GameSettings()
        };

        _lobbies[code] = lobby;
        return code;
    }

    public bool JoinLobby(string lobbyCode, string playerName)
    {
        if (!_lobbies.TryGetValue(lobbyCode, out var lobby))
            return false;

        if (lobby.Players.Count >= lobby.MaxPlayers)
            return false;

        if (lobby.Players.Contains(playerName))
            return false;

        lobby.Players.Add(playerName);
        return true;
    }

    public bool LeaveLobby(string lobbyCode, string playerName)
    {
        if (!_lobbies.TryGetValue(lobbyCode, out var lobby))
            return false;

        lobby.Players.Remove(playerName);

        // Remove lobby if empty
        if (lobby.Players.Count == 0)
        {
            _lobbies.TryRemove(lobbyCode, out _);
        }

        return true;
    }

    public Lobby? GetLobby(string lobbyCode)
    {
        _lobbies.TryGetValue(lobbyCode, out var lobby);
        return lobby;
    }

    public bool UpdateSettings(string lobbyCode, GameSettings settings)
    {
        if (!_lobbies.TryGetValue(lobbyCode, out var lobby))
            return false;

        lobby.Settings = settings;
        return true;
    }

    public bool StartGame(string lobbyCode)
    {
        if (!_lobbies.TryGetValue(lobbyCode, out var lobby))
            return false;

        lobby.Status = LobbyStatus.InGame;
        return true;
    }

    private string GenerateLobbyCode()
    {
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
        var code = new char[6];
        for (int i = 0; i < 6; i++)
        {
            code[i] = chars[_random.Next(chars.Length)];
        }
        return new string(code);
    }

    public void CleanupOldLobbies(TimeSpan maxAge)
    {
        var cutoff = DateTime.UtcNow - maxAge;
        var oldLobbies = _lobbies.Where(kvp => kvp.Value.CreatedAt < cutoff).Select(kvp => kvp.Key).ToList();
        
        foreach (var code in oldLobbies)
        {
            _lobbies.TryRemove(code, out _);
        }
    }
}
