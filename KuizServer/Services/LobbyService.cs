using System.Collections.Concurrent;

namespace KuizServer.Services;

public class LobbyService
{
    private readonly ConcurrentDictionary<string, Lobby> _lobbies = new();
    private readonly ConcurrentDictionary<string, string> _playerToLobby = new();
    private readonly ConcurrentDictionary<string, string> _connectionToPlayer = new();
    private const int MaxPlayersPerLobby = 4;

    public string CreateLobby(string hostName, string connectionId)
    {
        var lobbyCode = GenerateLobbyCode();
        var lobby = new Lobby
        {
            Code = lobbyCode,
            HostName = hostName,
            Players = new List<Player> { new Player { Name = hostName, ConnectionId = connectionId } },
            CreatedAt = DateTime.UtcNow
        };

        _lobbies[lobbyCode] = lobby;
        _playerToLobby[hostName] = lobbyCode;
        _connectionToPlayer[connectionId] = hostName;

        return lobbyCode;
    }

    public bool JoinLobby(string lobbyCode, string playerName, string connectionId)
    {
        if (!_lobbies.TryGetValue(lobbyCode, out var lobby))
            return false;

        if (lobby.Players.Count >= MaxPlayersPerLobby)
            return false;

        if (lobby.Players.Any(p => p.Name == playerName))
            return false;

        lobby.Players.Add(new Player { Name = playerName, ConnectionId = connectionId });
        _playerToLobby[playerName] = lobbyCode;
        _connectionToPlayer[connectionId] = playerName;

        return true;
    }

    public void LeaveLobby(string lobbyCode, string playerName)
    {
        if (!_lobbies.TryGetValue(lobbyCode, out var lobby))
            return;

        var player = lobby.Players.FirstOrDefault(p => p.Name == playerName);
        if (player != null)
        {
            lobby.Players.Remove(player);
            _playerToLobby.TryRemove(playerName, out _);
            _connectionToPlayer.TryRemove(player.ConnectionId, out _);

            // Remove lobby if empty or host left
            if (lobby.Players.Count == 0 || playerName == lobby.HostName)
            {
                _lobbies.TryRemove(lobbyCode, out _);
                foreach (var p in lobby.Players)
                {
                    _playerToLobby.TryRemove(p.Name, out _);
                    _connectionToPlayer.TryRemove(p.ConnectionId, out _);
                }
            }
        }
    }

    public object GetLobbyState(string lobbyCode)
    {
        if (!_lobbies.TryGetValue(lobbyCode, out var lobby))
            return new { exists = false };

        return new
        {
            exists = true,
            code = lobby.Code,
            host = lobby.HostName,
            players = lobby.Players.Select(p => p.Name).ToList(),
            playerCount = lobby.Players.Count,
            maxPlayers = MaxPlayersPerLobby
        };
    }

    public string? GetPlayerByConnectionId(string connectionId)
    {
        _connectionToPlayer.TryGetValue(connectionId, out var playerName);
        return playerName;
    }

    public string? GetLobbyByPlayer(string playerName)
    {
        _playerToLobby.TryGetValue(playerName, out var lobbyCode);
        return lobbyCode;
    }

    private string GenerateLobbyCode()
    {
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
        var random = new Random();
        string code;
        
        do
        {
            code = new string(Enumerable.Range(0, 6)
                .Select(_ => chars[random.Next(chars.Length)])
                .ToArray());
        } while (_lobbies.ContainsKey(code));

        return code;
    }
}

public class Lobby
{
    public required string Code { get; set; }
    public required string HostName { get; set; }
    public required List<Player> Players { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class Player
{
    public required string Name { get; set; }
    public required string ConnectionId { get; set; }
}
