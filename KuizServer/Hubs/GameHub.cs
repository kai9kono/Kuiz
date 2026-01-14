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

    public async Task JoinLobby(string lobbyCode, string playerName)
    {
        var lobby = _lobbyService.GetLobby(lobbyCode);
        if (lobby == null)
        {
            await Clients.Caller.SendAsync("Error", "Lobby not found");
            return;
        }

        await Groups.AddToGroupAsync(Context.ConnectionId, lobbyCode);
        await Clients.Group(lobbyCode).SendAsync("PlayerJoined", playerName, lobby.Players);
    }

    public async Task LeaveLobby(string lobbyCode, string playerName)
    {
        _lobbyService.LeaveLobby(lobbyCode, playerName);
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, lobbyCode);
        
        var lobby = _lobbyService.GetLobby(lobbyCode);
        if (lobby != null)
        {
            await Clients.Group(lobbyCode).SendAsync("PlayerLeft", playerName, lobby.Players);
        }
    }

    public async Task StartGame(string lobbyCode)
    {
        var lobby = _lobbyService.GetLobby(lobbyCode);
        if (lobby == null)
        {
            await Clients.Caller.SendAsync("Error", "Lobby not found");
            return;
        }

        _lobbyService.StartGame(lobbyCode);
        var room = _gameRoomService.CreateRoom(lobbyCode, lobby.Players);
        
        await Clients.Group(lobbyCode).SendAsync("GameStarted", room.SessionId);
    }

    public async Task Buzz(string lobbyCode, string playerName)
    {
        var success = _gameRoomService.ProcessBuzz(lobbyCode, playerName);
        if (success)
        {
            var room = _gameRoomService.GetRoom(lobbyCode);
            await Clients.Group(lobbyCode).SendAsync("BuzzReceived", playerName, room?.BuzzOrder);
        }
    }

    public async Task SubmitAnswer(string lobbyCode, string playerName, string answer)
    {
        var lobby = _lobbyService.GetLobby(lobbyCode);
        if (lobby?.Settings == null)
        {
            await Clients.Caller.SendAsync("Error", "Invalid lobby");
            return;
        }

        var (correct, gameOver, winner) = _gameRoomService.ProcessAnswer(
            lobbyCode, 
            playerName, 
            answer, 
            lobby.Settings.MaxMistakes, 
            lobby.Settings.PointsToWin
        );

        var room = _gameRoomService.GetRoom(lobbyCode);
        
        await Clients.Group(lobbyCode).SendAsync("AnswerResult", playerName, correct, room?.Scores, room?.Mistakes);

        if (gameOver && winner != null)
        {
            await Clients.Group(lobbyCode).SendAsync("GameOver", winner, room?.Scores, room?.Mistakes);
            _gameRoomService.RemoveRoom(lobbyCode);
        }
    }

    public async Task NextQuestion(string lobbyCode, string question, string answer)
    {
        _gameRoomService.SetCurrentQuestion(lobbyCode, question, answer);
        await Clients.Group(lobbyCode).SendAsync("QuestionRevealed", question);
    }

    public async Task UpdateGameState(string lobbyCode, object state)
    {
        await Clients.Group(lobbyCode).SendAsync("StateUpdated", state);
    }
}
