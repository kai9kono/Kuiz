using Microsoft.AspNetCore.SignalR;
using KuizServer.Services;

namespace KuizServer.Hubs;

public class GameHub : Hub
{
    private readonly LobbyService _lobbyService;

    public GameHub(LobbyService lobbyService)
    {
        _lobbyService = lobbyService;
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        await LeaveCurrentLobbyAsync();
        await base.OnDisconnectedAsync(exception);
    }

    public async Task<string> CreateLobby(string hostName)
    {
        var lobbyCode = _lobbyService.CreateLobby(hostName, Context.ConnectionId);
        await Groups.AddToGroupAsync(Context.ConnectionId, lobbyCode);
        return lobbyCode;
    }

    public async Task<bool> JoinLobby(string lobbyCode, string playerName)
    {
        if (!_lobbyService.JoinLobby(lobbyCode, playerName, Context.ConnectionId))
        {
            return false;
        }

        var assignedLobbyCode = GetLobbyCode();
        var assignedPlayerName = GetPlayerName();
        await Groups.AddToGroupAsync(Context.ConnectionId, assignedLobbyCode);
        await Clients.Group(assignedLobbyCode).SendAsync("PlayerJoined", assignedPlayerName);
        return true;
    }

    public Task LeaveLobby() => LeaveCurrentLobbyAsync();

    public object GetLobbyState() => _lobbyService.GetLobbyState(Context.ConnectionId);

    public Task StartGame(object gameSettings) => SendToLobbyAsHost("GameStarting", gameSettings);

    public Task SendBuzz() => Clients.Group(GetLobbyCode()).SendAsync("PlayerBuzzed", GetPlayerName());

    public Task SendAnswer(string answer) => Clients.Group(GetLobbyCode()).SendAsync("PlayerAnswered", GetPlayerName(), answer);

    public Task UpdateGameState(object gameState) => SendToLobbyAsHost("GameStateUpdated", gameState);

    public Task EndGame(object results) => SendToLobbyAsHost("GameEnded", results);

    public Task SendAnswerResult(string playerName, bool isCorrect) =>
        SendToLobbyAsHost("AnswerResult", playerName, isCorrect);

    public Task SendNextQuestion() => SendToLobbyAsHost("NextQuestion");

    private async Task LeaveCurrentLobbyAsync()
    {
        var departure = _lobbyService.LeaveLobby(Context.ConnectionId);
        if (departure is null)
        {
            return;
        }

        await Groups.RemoveFromGroupAsync(Context.ConnectionId, departure.LobbyCode);
        await Clients.Group(departure.LobbyCode).SendAsync("PlayerLeft", departure.PlayerName);
    }

    private string GetLobbyCode() =>
        _lobbyService.GetLobbyByConnectionId(Context.ConnectionId)
        ?? throw new HubException("Join a lobby before sending game events.");

    private string GetPlayerName() =>
        _lobbyService.GetPlayerByConnectionId(Context.ConnectionId)
        ?? throw new HubException("Join a lobby before sending game events.");

    private Task SendToLobbyAsHost(string method, params object?[] arguments)
    {
        if (!_lobbyService.IsHost(Context.ConnectionId))
        {
            throw new HubException("Only the lobby host can perform this action.");
        }

        return Clients.Group(GetLobbyCode()).SendAsync(method, arguments);
    }
}