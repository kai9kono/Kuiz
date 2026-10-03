using System.IO;
using System.Text.Json;
using System.Windows;
using System.Windows.Threading;

namespace Kuiz;

public partial class MainWindow
{
#if DEBUG
    private void StartDebugObservation()
    {
        var timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(100) };
        timer.Tick += (_, _) =>
        {
            File.WriteAllText(Path.Combine(DebugSession.Root!, DebugSession.Role + "-state.json"),
                JsonSerializer.Serialize(new
                {
                    game = GamePanel.Visibility == Visibility.Visible,
                    result = ResultPanel.Visibility == Visibility.Visible,
                    modal = _answeringModal?.Visibility == Visibility.Visible,
                    winner = TxtWinnerName.Text,
                    hostScore = _gameState.Scores.GetValueOrDefault("DebugHost"),
                    players = _gameState.LobbyPlayers.Count,
                    buzzEnabled = BtnGameBuzz.IsEnabled,
                    preDisplay = _isPreDisplay,
                    hostPreDisplay = _hostPreDisplay,
                    revealingAnswer = _isRevealingAnswer,
                    clientAnswering = _isClientAnswering,
                    answerDialogOpen = _isAnswerDialogOpen,
                    correctAnswered = _gameState.CorrectAnswered,
                    pausedForBuzz = _gameState.PausedForBuzz,
                    mistakes = _gameState.Mistakes.GetValueOrDefault("DebugGuest"),
                    maxMistakes = _gameState.MaxMistakes,
                    attempted = _gameState.AttemptedThisQuestion.ToArray(),
                    answerVisible = TxtAnswerReveal.Visibility == Visibility.Visible,
                    answer = TxtAnswerReveal.Text,
                    question = TxtGameQuestion.Text,
                    transitions = _panelTransitionCount,
                    playerColors = _gameState.LobbyPlayers.ToDictionary(name => name,
                        name => ((System.Windows.Media.SolidColorBrush)_gameState.EnsurePlayerColor(name)).Color.ToString())
                }));
        };
        timer.Start();
        Closed += (_, _) => timer.Stop();
    }

    private async Task RunDebugRegressionAsync()
    {
        async Task WaitFor(Func<JsonElement, bool> predicate, string label)
        {
            var deadline = DateTime.UtcNow.AddSeconds(20);
            while (DateTime.UtcNow < deadline)
            {
                try
                {
                    using var doc = JsonDocument.Parse(File.ReadAllText(Path.Combine(DebugSession.Root!, "guest-state.json")));
                    if (predicate(doc.RootElement))
                    {
                        File.AppendAllText(Path.Combine(DebugSession.Root!, "regression.log"), "PASS: " + label + "\n");
                        return;
                    }
                }
                catch (IOException) { }
                catch (JsonException) { }
                await Task.Delay(100);
            }
            throw new TimeoutException("Regression failed: " + label);
        }

        await WaitFor(s => s.GetProperty("players").GetInt32() == 2, "two WPF clients joined");
        var expectedFont = ((System.Windows.Media.FontFamily)FindResource("AppFont")).Source;
        if (FontFamily.Source != expectedFont || TxtGameQuestion.FontFamily.Source != expectedFont ||
            TxtWinnerName.FontFamily.Source != expectedFont || BtnToggleDarkMode.FontFamily.Source != expectedFont)
            throw new Exception("The clean UI font is not applied consistently.");
        var playerColors = _gameState.LobbyPlayers.ToDictionary(name => name,
            name => ((System.Windows.Media.SolidColorBrush)_gameState.EnsurePlayerColor(name)).Color.ToString());
        if (playerColors.Values.Distinct().Count() != playerColors.Count)
            throw new Exception("Debug players must have different colors.");
        await WaitFor(s => playerColors.All(pair =>
            s.GetProperty("playerColors").TryGetProperty(pair.Key, out var color) && color.GetString() == pair.Value),
            "each player's color agrees between host and guest in the lobby");
        File.AppendAllText(Path.Combine(DebugSession.Root!, "regression.log"), "PASS: clean UI font\n");
        TxtPointsToWin.Text = "5";
        BtnConfirmStartYes_Click(this, new RoutedEventArgs());
        await WaitFor(s => s.GetProperty("game").GetBoolean(), "guest enters game screen");
        await WaitFor(s => s.GetProperty("preDisplay").GetBoolean() && !s.GetProperty("buzzEnabled").GetBoolean(), "guest cannot buzz during question banner");
        await WaitFor(s => s.GetProperty("buzzEnabled").GetBoolean() && s.GetProperty("transitions").GetInt32() >= 2, "guest lobby and game animated transitions complete and buzz is enabled");
        if (!BtnGameBuzz.IsEnabled) throw new Exception("Host and guest buzz eligibility differs.");
        await WaitFor(s => playerColors.All(pair =>
            s.GetProperty("playerColors").TryGetProperty(pair.Key, out var color) && color.GetString() == pair.Value),
            "player colors remain consistent during the game");
        _gameState.Mistakes["DebugGuest"] = _gameState.MaxMistakes;
        await BroadcastGameStateAsync();
        await WaitFor(s => !s.GetProperty("buzzEnabled").GetBoolean(), "guest uses host mistake limit");
        _gameState.Mistakes["DebugGuest"] = 0;
        _gameState.AttemptedThisQuestion.Add("DebugGuest");
        await BroadcastGameStateAsync();
        await WaitFor(s => !s.GetProperty("buzzEnabled").GetBoolean(), "guest cannot answer twice in a question");
        _gameState.AttemptedThisQuestion.Clear();
        await BroadcastGameStateAsync();
        await WaitFor(s => s.GetProperty("buzzEnabled").GetBoolean(), "guest eligibility recovers with host state");
        _revealCts?.Cancel();
        var reveal = ShowAnswerRevealAsync(_gameState.CurrentQuestion!, CancellationToken.None);
        await WaitFor(s => s.GetProperty("answerVisible").GetBoolean() && !s.GetProperty("buzzEnabled").GetBoolean() &&
            s.GetProperty("question").GetString() == _gameState.CurrentQuestion!.Text &&
            s.GetProperty("answer").GetString() == "答え：" + _gameState.CurrentQuestion!.Answer,
            "normal question keeps question and answer separate");
        await reveal;
        await StartNextQuestionAsync();
        await WaitFor(s => !s.GetProperty("answerVisible").GetBoolean() && s.GetProperty("buzzEnabled").GetBoolean(),
            "next question clears answer and restores guest buzz eligibility");
        _gameState.ProcessBuzz("DebugHost");
        UpdateGameUi();
        await BroadcastGameStateAsync();
        await WaitFor(s => s.GetProperty("modal").GetBoolean(), "guest sees host answering modal");
        await WaitFor(s => !s.GetProperty("buzzEnabled").GetBoolean(), "guest cannot buzz while host answers");
        if (_answeringModal?.Visibility == Visibility.Visible) throw new Exception("Host saw own answering modal.");
        _gameState.PausedForBuzz = false;
        _gameState.BuzzOrder.Clear();
        await BroadcastGameStateAsync();
        await WaitFor(s => !s.GetProperty("modal").GetBoolean(), "modal closes after answering");
        _gameState.ProcessBuzz("DebugGuest");
        UpdateGameUi();
        if (_answeringModal?.Visibility != Visibility.Visible) throw new Exception("Host did not see guest answering modal.");
        await BroadcastGameStateAsync();
        await WaitFor(s => !s.GetProperty("modal").GetBoolean(), "guest does not see own answering modal");
        _gameState.Scores["DebugHost"] = 2;
        _gameState.PointsToWin = 2;
        _gameState.PlayQueue.Clear();
        var finishing = StartNextQuestionAsync();
        await WaitFor(s => s.GetProperty("answerVisible").GetBoolean() && !s.GetProperty("buzzEnabled").GetBoolean() &&
            s.GetProperty("answer").GetString() == "答え：" + _gameState.CurrentQuestion!.Answer &&
            s.GetProperty("question").GetString() == _gameState.CurrentQuestion!.Text &&
            !s.GetProperty("result").GetBoolean(), "final answer has a dedicated display before results");
        await Task.Delay(1800);
        await WaitFor(s => s.GetProperty("answerVisible").GetBoolean() && !s.GetProperty("result").GetBoolean(), "final answer remains visible rather than immediately transitioning");
        await finishing;
        await WaitFor(s => playerColors.All(pair =>
            s.GetProperty("playerColors").TryGetProperty(pair.Key, out var color) && color.GetString() == pair.Value),
            "player colors remain consistent in results");
        await WaitFor(s => s.GetProperty("result").GetBoolean() && s.GetProperty("hostScore").GetInt32() == 2 &&
            s.GetProperty("winner").GetString() == "DebugHost" && !s.GetProperty("modal").GetBoolean(),
            "winning score: guest shows winner and 2 points, modal closed");
        File.AppendAllText(Path.Combine(DebugSession.Root!, "regression.log"), "PASS: WPF regression complete\n");
    }
#endif
}
