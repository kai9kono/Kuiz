using System.Collections.Concurrent;
using KuizServer.Models;

namespace KuizServer.Services;

public class GameRoomService
{
    private readonly ConcurrentDictionary<string, GameRoom> _rooms = new();

    public GameRoom CreateRoom(string lobbyCode, List<string> players)
    {
        var room = new GameRoom
        {
            LobbyCode = lobbyCode,
            Scores = players.ToDictionary(p => p, p => 0),
            Mistakes = players.ToDictionary(p => p, p => 0),
            SessionId = new Random().Next(1000, 9999)
        };

        _rooms[lobbyCode] = room;
        return room;
    }

    public GameRoom? GetRoom(string lobbyCode)
    {
        _rooms.TryGetValue(lobbyCode, out var room);
        return room;
    }

    public bool ProcessBuzz(string lobbyCode, string playerName)
    {
        if (!_rooms.TryGetValue(lobbyCode, out var room))
            return false;

        if (room.BuzzOrder.Contains(playerName))
            return false;

        room.BuzzOrder.Add(playerName);
        room.IsWaitingForAnswer = true;
        return true;
    }

    public (bool correct, bool gameOver, string? winner) ProcessAnswer(string lobbyCode, string playerName, string answer, int maxMistakes, int pointsToWin)
    {
        if (!_rooms.TryGetValue(lobbyCode, out var room))
            return (false, false, null);

        var correct = string.Equals(answer, room.CurrentAnswer, StringComparison.OrdinalIgnoreCase);

        if (correct)
        {
            room.Scores[playerName]++;
            room.BuzzOrder.Clear();
            room.IsWaitingForAnswer = false;

            // Check win condition
            if (room.Scores[playerName] >= pointsToWin)
            {
                return (true, true, playerName);
            }
        }
        else
        {
            room.Mistakes[playerName]++;
            
            // Remove from buzz order
            room.BuzzOrder.Remove(playerName);

            // Check elimination
            if (room.Mistakes[playerName] >= maxMistakes)
            {
                room.Scores.Remove(playerName);
                room.Mistakes.Remove(playerName);
            }

            // Check if only one player left
            if (room.Scores.Count == 1)
            {
                var winner = room.Scores.Keys.First();
                return (false, true, winner);
            }
        }

        return (correct, false, null);
    }

    public void SetCurrentQuestion(string lobbyCode, string question, string answer)
    {
        if (_rooms.TryGetValue(lobbyCode, out var room))
        {
            room.CurrentQuestion = question;
            room.CurrentAnswer = answer;
            room.CurrentQuestionIndex++;
            room.BuzzOrder.Clear();
            room.IsWaitingForAnswer = false;
        }
    }

    public void RemoveRoom(string lobbyCode)
    {
        _rooms.TryRemove(lobbyCode, out _);
    }
}
