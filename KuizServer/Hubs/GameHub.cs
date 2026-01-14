using Microsoft.AspNetCore.SignalR;
using KuizServer.Services;

namespace KuizServer.Hubs;

public class GameHub : Hub
{
    private readonly LobbyService _lobbyService;
    private readonly GameRoomService _gameRoomService;

    public GameHub(LobbyService lobbyService, GameRoomService gameRoomService)
    {
        _lobbyService = lobbyService;
        _gameRoomService = gameRoomService;
    }

    public override async Task OnConnectedAsync()
    {
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        // Handle player disconnection
        var playerName = _lobbyService.GetPlayerByConnectionId(Context.ConnectionId);
        if (!string.IsNullOrEmpty(playerName))
        {
            var lobbyCode = _lobbyService.GetLobbyByPlayer(playerName);
            if (!string.IsNullOrEmpty(lobbyCode))
            {
                await LeaveLobby(lobbyCode, playerName);
            }
        }
        
        await base.OnDisconnectedAsync(exception);
    }

    // Lobby management
    public async Task<string> CreateLobby(string hostName)
    {
        var lobbyCode = _lobbyService.CreateLobby(hostName, Context.ConnectionId);
        await Groups.AddToGroupAsync(Context.ConnectionId, lobbyCode);
        return lobbyCode;
    }

    public async Task<bool> JoinLobby(string lobbyCode, string playerName)
    {
        var success = _lobbyService.JoinLobby(lobbyCode, playerName, Context.ConnectionId);
        if (success)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, lobbyCode);
            await Clients.Group(lobbyCode).SendAsync("PlayerJoined", playerName);
        }
        return success;
    }

    public async Task LeaveLobby(string lobbyCode, string playerName)
    {
        _lobbyService.LeaveLobby(lobbyCode, playerName);
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, lobbyCode);
        await Clients.Group(lobbyCode).SendAsync("PlayerLeft", playerName);
    }

    public async Task<object> GetLobbyState(string lobbyCode)
    {
        return _lobbyService.GetLobbyState(lobbyCode);
    }

    // Game management
    public async Task StartGame(string lobbyCode, object gameSettings)
    {
        await Clients.Group(lobbyCode).SendAsync("GameStarting", gameSettings);
    }

    public async Task SendBuzz(string lobbyCode, string playerName)
    {
        await Clients.Group(lobbyCode).SendAsync("PlayerBuzzed", playerName);
    }

    public async Task SendAnswer(string lobbyCode, string playerName, string answer)
    {
        await Clients.Group(lobbyCode).SendAsync("PlayerAnswered", playerName, answer);
    }

    public async Task UpdateGameState(string lobbyCode, object gameState)
    {
        await Clients.Group(lobbyCode).SendAsync("GameStateUpdated", gameState);
    }

    public async Task EndGame(string lobbyCode, object results)
    {
        await Clients.Group(lobbyCode).SendAsync("GameEnded", results);
    }
}
