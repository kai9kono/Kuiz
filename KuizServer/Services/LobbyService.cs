using System.Collections.Concurrent;

namespace KuizServer.Services;

public class LobbyService
{
    private const int MaxPlayersPerLobby = 4;
    private readonly ConcurrentDictionary<string, Lobby> _lobbies = new();
    private readonly ConcurrentDictionary<string, LobbyMembership> _memberships = new();
    private readonly object _membershipLock = new();

    public string CreateLobby(string hostName, string connectionId)
    {
        var normalizedName = NormalizePlayerName(hostName);
        lock (_membershipLock)
        {
            if (_memberships.ContainsKey(connectionId))
            {
                throw new InvalidOperationException("This connection already belongs to a lobby.");
            }

            string lobbyCode;
            do
            {
                lobbyCode = GenerateLobbyCode();
            } while (!_lobbies.TryAdd(lobbyCode, new Lobby
            {
                Code = lobbyCode,
                HostConnectionId = connectionId,
                HostName = normalizedName,
                Players = [new Player { Name = normalizedName, ConnectionId = connectionId }],
                CreatedAt = DateTime.UtcNow
            }));

            _memberships[connectionId] = new LobbyMembership(lobbyCode, normalizedName);
            return lobbyCode;
        }
    }

    public bool JoinLobby(string lobbyCode, string playerName, string connectionId)
    {
        if (string.IsNullOrWhiteSpace(lobbyCode) || lobbyCode.Length != 6 ||
            lobbyCode.Any(character => !char.IsAsciiLetterOrDigit(character)))
        {
            return false;
        }

        var normalizedCode = lobbyCode.Trim().ToUpperInvariant();
        var normalizedName = NormalizePlayerName(playerName);

        lock (_membershipLock)
        {
            if (_memberships.ContainsKey(connectionId) || !_lobbies.TryGetValue(normalizedCode, out var lobby))
            {
                return false;
            }

            lock (lobby.SyncRoot)
            {
                if (lobby.Players.Count >= MaxPlayersPerLobby ||
                    lobby.Players.Any(player => string.Equals(player.Name, normalizedName, StringComparison.OrdinalIgnoreCase)))
                {
                    return false;
                }

                lobby.Players.Add(new Player { Name = normalizedName, ConnectionId = connectionId });
                _memberships[connectionId] = new LobbyMembership(normalizedCode, normalizedName);
                return true;
            }
        }
    }

    public LobbyDeparture? LeaveLobby(string connectionId)
    {
        lock (_membershipLock)
        {
            if (!_memberships.TryRemove(connectionId, out var membership) ||
                !_lobbies.TryGetValue(membership.LobbyCode, out var lobby))
            {
                return null;
            }

            lock (lobby.SyncRoot)
            {
                var player = lobby.Players.FirstOrDefault(item => item.ConnectionId == connectionId);
                if (player is null)
                {
                    return null;
                }

                lobby.Players.Remove(player);
                var lobbyClosed = connectionId == lobby.HostConnectionId || lobby.Players.Count == 0;
                if (lobbyClosed)
                {
                    _lobbies.TryRemove(membership.LobbyCode, out _);
                    foreach (var remaining in lobby.Players)
                    {
                        _memberships.TryRemove(remaining.ConnectionId, out _);
                    }
                }

                return new LobbyDeparture(membership.LobbyCode, player.Name, lobbyClosed);
            }
        }
    }

    public object GetLobbyState(string connectionId)
    {
        if (!_memberships.TryGetValue(connectionId, out var membership) ||
            !_lobbies.TryGetValue(membership.LobbyCode, out var lobby))
        {
            return new { exists = false };
        }

        lock (lobby.SyncRoot)
        {
            return new
            {
                exists = true,
                code = lobby.Code,
                host = lobby.HostName,
                players = lobby.Players.Select(player => player.Name).ToList(),
                playerCount = lobby.Players.Count,
                maxPlayers = MaxPlayersPerLobby
            };
        }
    }

    public object GetLobbyStateByCode(string lobbyCode)
    {
        if (string.IsNullOrWhiteSpace(lobbyCode))
        {
            return new { exists = false };
        }

        var normalizedCode = lobbyCode.Trim().ToUpperInvariant();
        if (!_lobbies.TryGetValue(normalizedCode, out var lobby))
        {
            return new { exists = false };
        }

        lock (lobby.SyncRoot)
        {
            return new
            {
                exists = true,
                code = lobby.Code,
                host = lobby.HostName,
                players = lobby.Players.Select(player => player.Name).ToList(),
                playerCount = lobby.Players.Count,
                maxPlayers = MaxPlayersPerLobby
            };
        }
    }

    public string? GetLobbyByConnectionId(string connectionId) =>
        _memberships.TryGetValue(connectionId, out var membership) ? membership.LobbyCode : null;

    public string? GetPlayerByConnectionId(string connectionId) =>
        _memberships.TryGetValue(connectionId, out var membership) ? membership.PlayerName : null;

    public bool IsHost(string connectionId) =>
        _memberships.TryGetValue(connectionId, out var membership) &&
        _lobbies.TryGetValue(membership.LobbyCode, out var lobby) &&
        lobby.HostConnectionId == connectionId;

    private static string NormalizePlayerName(string playerName)
    {
        if (string.IsNullOrWhiteSpace(playerName))
        {
            throw new ArgumentException("Player name must contain 1 to 32 characters.", nameof(playerName));
        }

        var normalized = playerName.Trim();
        if (normalized.Length > 32)
        {
            throw new ArgumentException("Player name must contain 1 to 32 characters.", nameof(playerName));
        }

        return normalized;
    }

    private static string GenerateLobbyCode()
    {
        const string characters = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
        return string.Create(6, characters, static (span, source) =>
        {
            for (var index = 0; index < span.Length; index++)
            {
                span[index] = source[Random.Shared.Next(source.Length)];
            }
        });
    }
}

public sealed class Lobby
{
    public required string Code { get; init; }
    public required string HostName { get; init; }
    public required string HostConnectionId { get; init; }
    public required List<Player> Players { get; init; }
    public DateTime CreatedAt { get; init; }
    public object SyncRoot { get; } = new();
}

public sealed class Player
{
    public required string Name { get; init; }
    public required string ConnectionId { get; init; }
}

public sealed record LobbyMembership(string LobbyCode, string PlayerName);
public sealed record LobbyDeparture(string LobbyCode, string PlayerName, bool LobbyClosed);
