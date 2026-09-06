using System.IO;
using System.Windows;

namespace Kuiz;

public partial class MainWindow
{
    internal async Task StartDebugSessionAsync()
    {
#if DEBUG
        if (DebugSession.Role is null) return;
        Title = $"Kuiz DEBUG — {DebugSession.Role} — {Environment.ProcessId}";
        WindowState = WindowState.Normal;
        var area = SystemParameters.WorkArea;
        Width = Math.Max(MinWidth, area.Width / 2);
        Height = Math.Min(area.Height, Math.Max(MinHeight, 850));
        Left = DebugSession.Role == "host" ? area.Left : area.Left + area.Width / 2;
        Top = area.Top;
        var lobbyFile = Path.Combine(DebugSession.Root!, "lobby.txt");
        if (DebugSession.Smoke) StartDebugObservation();
        try
        {
            if (DebugSession.Role == "host")
            {
                HideAllPanels();
                HostPanel.Visibility = Visibility.Visible;
                await StartHostAsync();
                if (!_hostService.IsRunning) throw new InvalidOperationException("Debug host failed to connect.");
                await LoadQuestionsAsync();
                TxtNumQuestions.Text = "3";
                File.WriteAllText(lobbyFile, _hostService.LobbyCode);
            }
            else
            {
                var deadline = DateTime.UtcNow.AddSeconds(90);
                string code = "";
                while (DateTime.UtcNow < deadline)
                {
                    if (File.Exists(lobbyFile)) code = (await File.ReadAllTextAsync(lobbyFile)).Trim();
                    if (code.Length == 6) break;
                    await Task.Delay(250);
                }
                if (code.Length != 6) throw new TimeoutException("Debug host did not publish a lobby code.");
                HideAllPanels();
                JoinPanel.Visibility = Visibility.Visible;
                TxtJoinPlayerName.Text = "DebugGuest";
                TxtLobbyCodeInput.Text = code;
                BtnJoinConnect_Click(this, new RoutedEventArgs());
            }
            if (DebugSession.Smoke && DebugSession.Role == "host") await RunDebugRegressionAsync();
        }
        catch (Exception ex)
        {
            Logger.LogError(ex);
            MessageBox.Show(ex.Message, "Debug session", MessageBoxButton.OK, MessageBoxImage.Error);
        }
#else
        await Task.CompletedTask;
#endif
    }
}
