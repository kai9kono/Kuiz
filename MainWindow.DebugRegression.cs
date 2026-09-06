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
                    players = _gameState.LobbyPlayers.Count
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
        TxtPointsToWin.Text = "5";
        BtnConfirmStartYes_Click(this, new RoutedEventArgs());
        await WaitFor(s => s.GetProperty("game").GetBoolean(), "guest enters game screen");
        await Task.Delay(2200);
        _gameState.ProcessBuzz("DebugHost");
        UpdateGameUi();
        await BroadcastGameStateAsync();
        await WaitFor(s => s.GetProperty("modal").GetBoolean(), "guest sees host answering modal");
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
        _gameState.PlayQueue.Clear();
        await StartNextQuestionAsync();
        await WaitFor(s => s.GetProperty("result").GetBoolean() && s.GetProperty("hostScore").GetInt32() == 2 &&
            s.GetProperty("winner").GetString() == "DebugHost" && !s.GetProperty("modal").GetBoolean(),
            "queue exhaustion: guest shows winner and 2 points, modal closed");
        File.AppendAllText(Path.Combine(DebugSession.Root!, "regression.log"), "PASS: WPF regression complete\n");
    }
#endif
}
