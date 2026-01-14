using System;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using Kuiz.Models;

namespace Kuiz
{
    /// <summary>
    /// クライアント（参加者）関連のUI処理
    /// </summary>
    public partial class MainWindow
    {
        private int _lastClientSessionId = -1;
        private int _lastClientQuestionIndex = -1;
        private CancellationTokenSource? _pollStateCts;

        private void BtnTitleJoin_Click(object sender, RoutedEventArgs e) => ShowPanel(JoinPanel);
        private void BtnJoinBack_Click(object sender, RoutedEventArgs e) => ShowPanel(TitlePanel);

        private async void BtnJoinConnect_Click(object sender, RoutedEventArgs e)
        {
            var lobbyCode = TxtLobbyCodeInput.Text.Trim().ToUpper();
            var name = TxtJoinPlayerName.Text.Trim();

            if (string.IsNullOrEmpty(lobbyCode) || lobbyCode.Length != 6 || string.IsNullOrEmpty(name))
            {
                TxtJoinStatus.Text = "ロビーコードとプレイヤー名を入力してください";
                return;
            }

            TxtJoinStatus.Text = "接続中...";
            BtnJoinConnect.IsEnabled = false;

            // Always use Railway server
            var serverUrl = _appConfig.Config.ServerUrl;
            
            Logger.LogInfo($"==========================================");
            Logger.LogInfo($"?? JOIN LOBBY ATTEMPT");
            Logger.LogInfo($"   Player: {name}");
            Logger.LogInfo($"   Lobby Code: {lobbyCode}");
            Logger.LogInfo($"   Server: {serverUrl}");
            Logger.LogInfo($"==========================================");

            try
            {
                // Use SignalR client to connect
                var success = await _signalRClient.ConnectAsync(serverUrl, lobbyCode, name);

                if (success)
                {
                    Logger.LogInfo("? Successfully joined lobby!");
                    TxtJoinStatus.Text = "ロビーに参加しました！";
                    _profileService.Save(name);
                    
                    // Mark as client (not host)
                    _isHost = false;
                    
                    // Setup SignalR event handlers
                    SetupSignalRClientHandlers();
                    
                    // Get lobby state and show lobby panel
                    await UpdateClientLobbyState(lobbyCode);
                    
                    // Wait a moment before switching panels
                    await Task.Delay(500);
                    
                    // Show host panel in read-only mode (client view)
                    ShowPanel(HostPanel);
                    
                    // Disable host-only controls
                    SetClientLobbyMode();
                }
                else
                {
                    Logger.LogInfo("? Failed to join lobby - server returned false");
                    TxtJoinStatus.Text = "ロビーが見つかりません。コードを確認してください";
                    BtnJoinConnect.IsEnabled = true;
                }
            }
            catch (Exception ex)
            {
                Logger.LogError($"? Exception during join: {ex.Message}");
                Logger.LogError(ex);
                TxtJoinStatus.Text = $"エラー: {ex.Message}";
                BtnJoinConnect.IsEnabled = true;
            }
        }

        private void SetupSignalRClientHandlers()
        {
            _signalRClient.OnGameStateUpdated += (state) =>
            {
                Dispatcher.Invoke(() =>
                {
                    // ゲーム状態の更新処理
                    Logger.LogInfo("Game state updated via SignalR");
                });
            };

            _signalRClient.OnPlayerJoined += (playerName) =>
            {
                Dispatcher.Invoke(() =>
                {
                    Logger.LogInfo($"?? Player joined: {playerName}");
                    
                    // Add player to lobby list
                    if (!_gameState.LobbyPlayers.Contains(playerName))
                    {
                        _gameState.AddPlayer(playerName);
                        UpdateLobbyUi();
                    }
                });
            };

            _signalRClient.OnPlayerLeft += (playerName) =>
            {
                Dispatcher.Invoke(() =>
                {
                    Logger.LogInfo($"?? Player left: {playerName}");
                    
                    // Remove player from lobby list
                    _gameState.LobbyPlayers.Remove(playerName);
                    UpdateLobbyUi();
                });
            };

            _signalRClient.OnPlayerBuzzed += (playerName) =>
            {
                Dispatcher.Invoke(() =>
                {
                    Logger.LogInfo($"?? Player buzzed: {playerName}");
                    
                    // Handle buzz in game (simplified - just log for now)
                    // Full buzz handling is done on host side
                });
            };

            _signalRClient.OnGameStarting += async (settings) =>
            {
                await Dispatcher.InvokeAsync(async () =>
                {
                    Logger.LogInfo("?? Game starting!");
                    
                    // Parse game settings
                    try
                    {
                        var settingsJson = System.Text.Json.JsonSerializer.Serialize(settings);
                        var gameSettings = System.Text.Json.JsonSerializer.Deserialize<GameSettings>(settingsJson);
                        
                        if (gameSettings != null)
                        {
                            // Initialize game state from settings
                            _gameState.PointsToWin = gameSettings.PointsToWin;
                            _gameState.MaxMistakes = gameSettings.MaxMistakes;
                            
                            // Initialize players
                            _gameState.LobbyPlayers.Clear();
                            foreach (var player in gameSettings.Players)
                            {
                                _gameState.AddPlayer(player);
                            }
                            
                            // Initialize scores
                            _gameState.Scores.Clear();
                            foreach (var kvp in gameSettings.Scores)
                            {
                                _gameState.Scores[kvp.Key] = kvp.Value;
                            }
                            
                            // Initialize mistakes
                            _gameState.Mistakes.Clear();
                            foreach (var kvp in gameSettings.Mistakes)
                            {
                                _gameState.Mistakes[kvp.Key] = kvp.Value;
                            }
                            
                            _gameState.InitializeScores();
                            
                            Logger.LogInfo($"?? Game initialized with {gameSettings.Players.Count} players");
                        }
                    }
                    catch (Exception ex)
                    {
                        Logger.LogError($"Failed to parse game settings: {ex.Message}");
                    }
                    
                    // Show countdown
                    await ShowGameStartCountdownAsync();
                    
                    // Switch to game panel
                    ShowPanel(GamePanel);
                    UpdateGameUi();
                });
            };

            _signalRClient.OnGameEnded += (results) =>
            {
                Dispatcher.Invoke(() =>
                {
                    Logger.LogInfo("?? Game ended");
                    // Handle game end
                });
            };
        }

        private async Task UpdateClientLobbyState(string lobbyCode)
        {
            try
            {
                var state = await _signalRClient.GetLobbyStateAsync();
                
                // Parse lobby state
                var stateJson = System.Text.Json.JsonSerializer.Serialize(state);
                var lobbyState = System.Text.Json.JsonSerializer.Deserialize<LobbyState>(stateJson);
                
                if (lobbyState != null && lobbyState.Exists)
                {
                    Logger.LogInfo($"?? Lobby state: {lobbyState.PlayerCount} players");
                    
                    // Update lobby code display
                    TxtLobbyCode.Text = lobbyState.Code;
                    
                    // Update player list
                    _gameState.LobbyPlayers.Clear();
                    foreach (var player in lobbyState.Players)
                    {
                        _gameState.AddPlayer(player);
                    }
                    
                    UpdateLobbyUi();
                }
            }
            catch (Exception ex)
            {
                Logger.LogError($"Failed to get lobby state: {ex.Message}");
                Logger.LogError(ex);
            }
        }

        private void SetClientLobbyMode()
        {
            // Disable host-only controls
            BtnHostStartGame.IsEnabled = false;
            BtnHostStartGame.Opacity = 0.5;
            BtnHostStartGame.ToolTip = "ホストのみが開始できます";
            
            // Disable game settings
            TxtPointsToWin.IsReadOnly = true;
            TxtMaxMistakes.IsReadOnly = true;
            TxtNumQuestions.IsReadOnly = true;
            
            // Hide increment/decrement buttons
            // Find buttons in the visual tree
            var pointsGrid = TxtPointsToWin.Parent as System.Windows.Controls.Grid;
            if (pointsGrid != null)
            {
                foreach (var child in pointsGrid.Children)
                {
                    if (child is System.Windows.Controls.Button btn)
                    {
                        btn.IsEnabled = false;
                        btn.Opacity = 0.3;
                    }
                }
            }
            
            var mistakesGrid = TxtMaxMistakes.Parent as System.Windows.Controls.Grid;
            if (mistakesGrid != null)
            {
                foreach (var child in mistakesGrid.Children)
                {
                    if (child is System.Windows.Controls.Button btn)
                    {
                        btn.IsEnabled = false;
                        btn.Opacity = 0.3;
                    }
                }
            }
            
            var questionsGrid = TxtNumQuestions.Parent as System.Windows.Controls.Grid;
            if (questionsGrid != null)
            {
                foreach (var child in questionsGrid.Children)
                {
                    if (child is System.Windows.Controls.Button btn)
                    {
                        btn.IsEnabled = false;
                        btn.Opacity = 0.3;
                    }
                }
            }
            
            Logger.LogInfo("?? Client lobby mode enabled (read-only)");
        }

        // Helper class for lobby state deserialization
        private class LobbyState
        {
            public bool Exists { get; set; }
            public string Code { get; set; } = string.Empty;
            public string Host { get; set; } = string.Empty;
            public List<string> Players { get; set; } = new();
            public int PlayerCount { get; set; }
            public int MaxPlayers { get; set; }
        }

        // Helper class for game settings deserialization
        private class GameSettings
        {
            public int PointsToWin { get; set; }
            public int MaxMistakes { get; set; }
            public int NumQuestions { get; set; }
            public List<string> Players { get; set; } = new();
            public Dictionary<string, int> Scores { get; set; } = new();
            public Dictionary<string, int> Mistakes { get; set; } = new();
        }

        // Legacy HTTP polling - replaced by SignalR
        /*
        private async Task PollStateLoopAsync(string hostUrl, CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    await Task.Delay(500, cancellationToken);
                    var res = await _httpClient.GetAsync(new Uri(new Uri(hostUrl), "/state"), cancellationToken);

                    if (res.IsSuccessStatusCode)
                    {
                        var json = await res.Content.ReadAsStringAsync();
                        var state = JsonSerializer.Deserialize<StateDto>(json);
                        Dispatcher.Invoke(() => ApplyState(state));
                    }
                }
                catch (OperationCanceledException)
                {
                    // Loop was cancelled - exit gracefully
                    Logger.LogInfo("PollStateLoopAsync cancelled");
                    break;
                }
                catch (Exception ex)
                {
                    Logger.LogError(ex);
                }
            }
        }

        private void ApplyState(StateDto? state)
        {
            if (state == null) return;

            _gameState.QueuePosition = state.questionIndex;
            _gameState.RevealedText = state.revealed ?? "";

            if (state.sessionId != _lastClientSessionId)
            {
                _gameState.AttemptedThisQuestion.Clear();
                _gameState.BuzzOrder.Clear();
                _gameState.PausedForBuzz = false;
                _isAnswerDialogOpen = false;
                _isPreDisplay = false;
                _lastClientQuestionIndex = -1;
                _lastClientSessionId = state.sessionId;
                
                Dispatcher.Invoke(() =>
                {
                    AnswerOverlay.Visibility = Visibility.Collapsed;
                    AnswerOverlay.IsHitTestVisible = false;
                    TxtAnsweringBadge.Visibility = Visibility.Collapsed;
                });
            }

            if (state.questionIndex != _lastClientQuestionIndex)
            {
                _gameState.AttemptedThisQuestion.Clear();
                _gameState.BuzzOrder.Clear();
                _gameState.PausedForBuzz = false;
                _lastClientQuestionIndex = state.questionIndex;
            }

            _gameState.BuzzOrder.Clear();
            if (state.buzzOrder != null)
            {
                foreach (var b in state.buzzOrder)
                {
                    _gameState.BuzzOrder.Add(b);
                }
            }

            _gameState.Scores.Clear();
            if (state.scores != null)
            {
                foreach (var kv in state.scores)
                {
                    _gameState.Scores[kv.Key] = kv.Value;
                }
            }

            _gameState.Mistakes.Clear();
            if (state.mistakes != null)
            {
                foreach (var kv in state.mistakes)
                {
                    _gameState.Mistakes[kv.Key] = kv.Value;
                }
            }

            if (!_isAnswerDialogOpen && !_isPreDisplay)
            {
                TxtGameQuestion.Text = _gameState.RevealedText;
            }

            ListPlayersInGame.ItemsSource = _gameState.GetPlayerStates();

            UpdateBuzzButtonState();
        }
        */
    }
}
