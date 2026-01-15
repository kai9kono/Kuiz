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
        
        // Client-side question management for smooth display
        private readonly List<ClientQuestion> _clientQuestions = new();
        private int _clientQuestionIndex = -1;
        private CancellationTokenSource? _clientRevealCts;

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
                    Logger.LogInfo("✅ Successfully joined lobby!");
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
                    Logger.LogInfo("❌ Failed to join lobby - server returned false");
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
                    try
                    {
                        // Parse game state from host
                        var stateJson = System.Text.Json.JsonSerializer.Serialize(state);
                        var gameState = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(stateJson);
                        
                        if (gameState != null)
                        {
                            // NOTE: Don't update revealed text - client displays locally for smooth animation
                            
                            // Update scores
                            if (gameState.ContainsKey("scores"))
                            {
                                var scoresElement = gameState["scores"];
                                _gameState.Scores.Clear();
                                foreach (var prop in scoresElement.EnumerateObject())
                                {
                                    var playerName = prop.Name;
                                    var score = prop.Value.GetInt32();
                                    _gameState.Scores[playerName] = score;
                                }
                            }
                            
                            // Update mistakes
                            if (gameState.ContainsKey("mistakes"))
                            {
                                var mistakesElement = gameState["mistakes"];
                                _gameState.Mistakes.Clear();
                                foreach (var prop in mistakesElement.EnumerateObject())
                                {
                                    var playerName = prop.Name;
                                    var mistakes = prop.Value.GetInt32();
                                    _gameState.Mistakes[playerName] = mistakes;
                                }
                            }
                            
                            // Update buzz order
                            if (gameState.ContainsKey("buzzOrder"))
                            {
                                var buzzOrderElement = gameState["buzzOrder"];
                                _gameState.BuzzOrder.Clear();
                                foreach (var item in buzzOrderElement.EnumerateArray())
                                {
                                    var playerName = item.GetString();
                                    if (!string.IsNullOrEmpty(playerName))
                                    {
                                        _gameState.BuzzOrder.Add(playerName);
                                    }
                                }
                                
                                // Show answering badge if someone is answering
                                if (_gameState.BuzzOrder.Count > 0)
                                {
                                    var answerer = _gameState.BuzzOrder[0];
                                    TxtAnsweringBadge.Text = $"回答中: {answerer}";
                                    TxtAnsweringBadge.Visibility = Visibility.Visible;
                                    TxtGameStatus.Text = $"{answerer} が回答中...";
                                }
                                else
                                {
                                    TxtAnsweringBadge.Visibility = Visibility.Collapsed;
                                }
                            }
                            
                            // Update paused state
                            if (gameState.ContainsKey("pausedForBuzz"))
                            {
                                _gameState.PausedForBuzz = gameState["pausedForBuzz"].GetBoolean();
                            }
                            
                            // Update attempted list
                            if (gameState.ContainsKey("attemptedThisQuestion"))
                            {
                                var attemptedElement = gameState["attemptedThisQuestion"];
                                _gameState.AttemptedThisQuestion.Clear();
                                foreach (var item in attemptedElement.EnumerateArray())
                                {
                                    var playerName = item.GetString();
                                    if (!string.IsNullOrEmpty(playerName))
                                    {
                                        _gameState.AttemptedThisQuestion.Add(playerName);
                                    }
                                }
                            }
                            
                            // Update players list
                            if (gameState.ContainsKey("players"))
                            {
                                var playersElement = gameState["players"];
                                _gameState.LobbyPlayers.Clear();
                                foreach (var item in playersElement.EnumerateArray())
                                {
                                    var playerName = item.GetString();
                                    if (!string.IsNullOrEmpty(playerName))
                                    {
                                        _gameState.AddPlayer(playerName);
                                    }
                                }
                            }
                            
                            // Update game UI to reflect changes
                            UpdateGameUi();
                        }
                    }
                    catch (Exception ex)
                    {
                        Logger.LogError($"Failed to update game state: {ex.Message}");
                    }
                });
            };

            _signalRClient.OnPlayerJoined += (playerName) =>
            {
                Dispatcher.Invoke(() =>
                {
                    Logger.LogInfo($"📥 Player joined: {playerName}");
                    
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
                    Logger.LogInfo($"📤 Player left: {playerName}");
                    
                    // Remove player from lobby list
                    _gameState.LobbyPlayers.Remove(playerName);
                    UpdateLobbyUi();
                });
            };

            _signalRClient.OnPlayerBuzzed += (playerName) =>
            {
                Dispatcher.Invoke(async () =>
                {
                    Logger.LogInfo($"🔔 Player buzzed: {playerName}");
                    
                    // Update buzz state
                    _gameState.PausedForBuzz = true;
                    if (!_gameState.BuzzOrder.Contains(playerName))
                    {
                        _gameState.BuzzOrder.Clear();
                        _gameState.BuzzOrder.Add(playerName);
                    }
                    
                    var myName = _profileService.PlayerName ?? TxtJoinPlayerName.Text.Trim();
                    
                    // Show answering badge for everyone
                    TxtAnsweringBadge.Text = $"回答中: {playerName}";
                    TxtAnsweringBadge.Visibility = Visibility.Visible;
                    
                    // If it's me who buzzed, show answer input
                    if (playerName == myName)
                    {
                        TxtGameStatus.Text = "回答入力中...";
                        UpdateGameUi();
                        
                        // Cancel reveal while answering
                        _clientRevealCts?.Cancel();
                        
                        var answer = await ShowClientAnswerInputAsync(10);
                        
                        // Send answer (empty string for timeout)
                        var answerToSend = answer ?? "";
                        await _signalRClient.SendAnswerAsync(answerToSend);
                        Logger.LogInfo($"📤 Answer sent via SignalR: '{answerToSend}'");
                        
                        if (!string.IsNullOrEmpty(answer))
                        {
                            TxtGameStatus.Text = "回答送信済み - 判定待ち...";
                        }
                        else
                        {
                            TxtGameStatus.Text = "時間切れ";
                        }
                    }
                    else
                    {
                        // Show that someone else is answering
                        TxtGameStatus.Text = $"{playerName} が回答中...";
                        
                        // Disable buzz button while someone is answering
                        if (BtnGameBuzz != null)
                        {
                            BtnGameBuzz.IsEnabled = false;
                        }
                        UpdateGameUi();
                    }
                });
            };

            _signalRClient.OnGameStarting += async (settings) =>
            {
                await Dispatcher.InvokeAsync(async () =>
                {
                    Logger.LogInfo("🎮 Game starting!");
                    
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
                            
                            // Initialize questions for local display
                            _clientQuestions.Clear();
                            if (gameSettings.Questions != null)
                            {
                                foreach (var q in gameSettings.Questions)
                                {
                                    _clientQuestions.Add(new ClientQuestion { Text = q.Text, Answer = q.Answer });
                                }
                                Logger.LogInfo($"📋 Received {_clientQuestions.Count} questions for local display");
                            }
                            _clientQuestionIndex = -1;
                            
                            _gameState.InitializeScores();
                            
                            Logger.LogInfo($"📋 Game initialized with {gameSettings.Players.Count} players");
                        }
                    }
                    catch (Exception ex)
                    {
                        Logger.LogError($"Failed to parse game settings: {ex.Message}");
                    }
                    
                    // Reset game state
                    _gameEnded = false;
                    
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
                    Logger.LogInfo("🏁 Game ended via SignalR - processing results");
                    
                    try
                    {
                        // Parse results
                        var resultsJson = System.Text.Json.JsonSerializer.Serialize(results);
                        Logger.LogInfo($"📋 Game results JSON: {resultsJson}");
                        
                        var gameResults = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(resultsJson);
                        
                        var winner = "No winner";
                        
                        if (gameResults != null)
                        {
                            // Try both "Scores" and "scores" (case sensitivity)
                            if (gameResults.ContainsKey("Scores"))
                            {
                                var scoresElement = gameResults["Scores"];
                                _gameState.Scores.Clear();
                                foreach (var prop in scoresElement.EnumerateObject())
                                {
                                    _gameState.Scores[prop.Name] = prop.Value.GetInt32();
                                }
                            }
                            else if (gameResults.ContainsKey("scores"))
                            {
                                var scoresElement = gameResults["scores"];
                                _gameState.Scores.Clear();
                                foreach (var prop in scoresElement.EnumerateObject())
                                {
                                    _gameState.Scores[prop.Name] = prop.Value.GetInt32();
                                }
                            }
                            
                            // Get winner (try both cases)
                            if (gameResults.ContainsKey("Winner"))
                            {
                                winner = gameResults["Winner"].GetString() ?? "No winner";
                            }
                            else if (gameResults.ContainsKey("winner"))
                            {
                                winner = gameResults["winner"].GetString() ?? "No winner";
                            }
                        }
                        
                        Logger.LogInfo($"🏆 Winner: {winner}");
                        
                        // Cancel any ongoing reveal
                        _clientRevealCts?.Cancel();
                        
                        // Show result screen
                        _gameEnded = true;
                        ShowResult(winner);
                    }
                    catch (Exception ex)
                    {
                        Logger.LogError($"Failed to parse game results: {ex.Message}");
                        Logger.LogError(ex);
                        
                        // Show result screen anyway
                        _gameEnded = true;
                        ShowResult("Game Over");
                    }
                });
            };

            // 回答結果を受信
            _signalRClient.OnAnswerResult += (playerName, isCorrect) =>
            {
                Dispatcher.Invoke(async () =>
                {
                    Logger.LogInfo($"📋 Answer result: {playerName} - {(isCorrect ? "正解" : "不正解")}");
                    
                    // Hide answering badge
                    TxtAnsweringBadge.Visibility = Visibility.Collapsed;
                    _gameState.BuzzOrder.Clear();
                    _gameState.PausedForBuzz = false;
                    
                    if (isCorrect)
                    {
                        _soundService.PlayCorrect();
                        TxtOverlayStatus.Text = "正解！";
                        TxtOverlayDetail.Text = $"{playerName}";
                    }
                    else
                    {
                        _soundService.PlayIncorrect();
                        TxtOverlayStatus.Text = "不正解...";
                        TxtOverlayDetail.Text = $"{playerName}";
                    }
                    
                    ResultOverlay.Visibility = Visibility.Visible;
                    ResultOverlay.IsHitTestVisible = true;
                    AnimateOverlayOpen(ResultOverlayBorder);
                    
                    await Task.Delay(isCorrect ? 1000 : 1500);
                    
                    ResultOverlay.Visibility = Visibility.Collapsed;
                    ResultOverlay.IsHitTestVisible = false;
                    
                    // Resume local reveal if not correct (game continues)
                    if (!isCorrect && _clientQuestionIndex >= 0 && _clientQuestionIndex < _clientQuestions.Count)
                    {
                        // Resume from current position
                        var currentQuestion = _clientQuestions[_clientQuestionIndex].Text;
                        var currentPos = _gameState.RevealedText.Length;
                        if (currentPos < currentQuestion.Length)
                        {
                            StartClientRevealLoopFromPosition(currentQuestion, currentPos);
                        }
                    }
                    
                    UpdateGameUi();
                });
            };

            // 次の問題へ遷移
            _signalRClient.OnNextQuestion += () =>
            {
                Dispatcher.Invoke(async () =>
                {
                    Logger.LogInfo("📋 Next question notification received");
                    
                    // Reset question state for client
                    _gameState.AttemptedThisQuestion.Clear();
                    _gameState.BuzzOrder.Clear();
                    _gameState.PausedForBuzz = false;
                    TxtAnsweringBadge.Visibility = Visibility.Collapsed;
                    TxtGameStatus.Text = string.Empty;
                    
                    // Move to next question locally
                    _clientQuestionIndex++;
                    
                    if (_clientQuestionIndex < _clientQuestions.Count)
                    {
                        // Show question number banner
                        await ShowClientPreDisplayBannerAsync(_clientQuestionIndex + 1);
                        
                        // Start local reveal
                        StartClientRevealLoop(_clientQuestions[_clientQuestionIndex].Text);
                    }
                    
                    UpdateGameUi();
                });
            };
        }

        private async Task UpdateClientLobbyState(string lobbyCode)
        {
            try
            {
                Logger.LogInfo($"📋 Requesting lobby state for code: {lobbyCode}");
                var state = await _signalRClient.GetLobbyStateAsync();
                
                // Parse lobby state
                var stateJson = System.Text.Json.JsonSerializer.Serialize(state);
                Logger.LogInfo($"📋 Received lobby state JSON: {stateJson}");
                
                var lobbyState = System.Text.Json.JsonSerializer.Deserialize<LobbyState>(stateJson);
                
                if (lobbyState != null && lobbyState.Exists)
                {
                    Logger.LogInfo($"📋 Lobby state: {lobbyState.PlayerCount} players - {string.Join(", ", lobbyState.Players)}");
                    
                    // Update lobby code display
                    TxtLobbyCode.Text = lobbyState.Code;
                    
                    // Update player list
                    _gameState.LobbyPlayers.Clear();
                    foreach (var player in lobbyState.Players)
                    {
                        _gameState.AddPlayer(player);
                        Logger.LogInfo($"  - Added player: {player}");
                    }
                    
                    UpdateLobbyUi();
                }
                else
                {
                    Logger.LogError($"❌ Lobby state is null or does not exist");
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
            
            Logger.LogInfo("🔒 Client lobby mode enabled (read-only)");
        }

        // Helper class for lobby state deserialization
        private class LobbyState
        {
            [System.Text.Json.Serialization.JsonPropertyName("exists")]
            public bool Exists { get; set; }
            [System.Text.Json.Serialization.JsonPropertyName("code")]
            public string Code { get; set; } = string.Empty;
            [System.Text.Json.Serialization.JsonPropertyName("host")]
            public string Host { get; set; } = string.Empty;
            [System.Text.Json.Serialization.JsonPropertyName("players")]
            public List<string> Players { get; set; } = new();
            [System.Text.Json.Serialization.JsonPropertyName("playerCount")]
            public int PlayerCount { get; set; }
            [System.Text.Json.Serialization.JsonPropertyName("maxPlayers")]
            public int MaxPlayers { get; set; }
        }

        // Helper class for game settings deserialization
        private class GameSettings
        {
            [System.Text.Json.Serialization.JsonPropertyName("PointsToWin")]
            public int PointsToWin { get; set; }
            [System.Text.Json.Serialization.JsonPropertyName("MaxMistakes")]
            public int MaxMistakes { get; set; }
            [System.Text.Json.Serialization.JsonPropertyName("NumQuestions")]
            public int NumQuestions { get; set; }
            [System.Text.Json.Serialization.JsonPropertyName("Players")]
            public List<string> Players { get; set; } = new();
            [System.Text.Json.Serialization.JsonPropertyName("Scores")]
            public Dictionary<string, int> Scores { get; set; } = new();
            [System.Text.Json.Serialization.JsonPropertyName("Mistakes")]
            public Dictionary<string, int> Mistakes { get; set; } = new();
            [System.Text.Json.Serialization.JsonPropertyName("Questions")]
            public List<QuestionData> Questions { get; set; } = new();
        }

        private class QuestionData
        {
            [System.Text.Json.Serialization.JsonPropertyName("Text")]
            public string Text { get; set; } = string.Empty;
            [System.Text.Json.Serialization.JsonPropertyName("Answer")]
            public string Answer { get; set; } = string.Empty;
        }

        private class ClientQuestion
        {
            public string Text { get; set; } = string.Empty;
            public string Answer { get; set; } = string.Empty;
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

        /// <summary>
        /// クライアント用のシンプルな回答入力（AnswerOverlayを使用）
        /// </summary>
        private async Task<string?> ShowClientAnswerInputAsync(int secondsTimeout)
        {
            var tcs = new TaskCompletionSource<string?>();
            CancellationTokenSource? cts = null;

            try
            {
                cts = new CancellationTokenSource();
                var remainingSeconds = secondsTimeout;

                Dispatcher.Invoke(() =>
                {
                    TxtOverlayTitle.Text = "回答入力";
                    TxtOverlayInfo.Visibility = Visibility.Collapsed;
                    TxtOverlayAnswer.Text = string.Empty;
                    TxtOverlayAnswer.Visibility = Visibility.Visible;
                    TxtOverlayTimer.Text = $"{remainingSeconds}s";
                    AnswerOverlay.Visibility = Visibility.Visible;
                    AnswerOverlay.IsHitTestVisible = true;
                    AnimateOverlayOpen(AnswerOverlayBorder);
                    TxtOverlayAnswer.Focus();
                });

                // Enterキーで回答を送信
                void OnKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
                {
                    if (e.Key == System.Windows.Input.Key.Enter)
                    {
                        e.Handled = true;
                        var text = TxtOverlayAnswer.Text?.Trim();
                        tcs.TrySetResult(string.IsNullOrWhiteSpace(text) ? null : text);
                        cts?.Cancel();
                    }
                    else if (e.Key == System.Windows.Input.Key.Escape)
                    {
                        e.Handled = true;
                        tcs.TrySetResult(null);
                        cts?.Cancel();
                    }
                }

                Dispatcher.Invoke(() => TxtOverlayAnswer.KeyDown += OnKeyDown);

                // カウントダウン
                _ = Task.Run(async () =>
                {
                    try
                    {
                        while (remainingSeconds > 0 && !cts.Token.IsCancellationRequested)
                        {
                            await Task.Delay(1000, cts.Token);
                            remainingSeconds--;
                            Dispatcher.Invoke(() => TxtOverlayTimer.Text = $"{remainingSeconds}s");
                        }

                        if (!cts.Token.IsCancellationRequested)
                        {
                            tcs.TrySetResult(null);
                        }
                    }
                    catch (OperationCanceledException) { }
                });

                var result = await tcs.Task;

                Dispatcher.Invoke(() => TxtOverlayAnswer.KeyDown -= OnKeyDown);

                return result;
            }
            finally
            {
                cts?.Cancel();
                cts?.Dispose();

                Dispatcher.Invoke(() =>
                {
                    AnswerOverlay.Visibility = Visibility.Collapsed;
                    AnswerOverlay.IsHitTestVisible = false;
                    TxtOverlayTimer.Text = string.Empty;
                    TxtOverlayAnswer.Text = string.Empty;
                });
            }
        }

        /// <summary>
        /// クライアント用の問題番号表示
        /// </summary>
        private async Task ShowClientPreDisplayBannerAsync(int questionNumber)
        {
            Dispatcher.Invoke(() =>
            {
                TxtGameQuestion.Text = $"第{questionNumber}問";
                TxtGameQuestion.TextAlignment = System.Windows.TextAlignment.Center;
                TxtGameQuestion.FontWeight = FontWeights.Bold;
                if (BtnGameBuzz != null) BtnGameBuzz.IsEnabled = false;
                _soundService.PlayQuestion();
            });

            await Task.Delay(2000);

            Dispatcher.Invoke(() =>
            {
                TxtGameQuestion.Text = string.Empty;
                TxtGameQuestion.TextAlignment = System.Windows.TextAlignment.Left;
                TxtGameQuestion.FontWeight = FontWeights.Normal;
                if (BtnGameBuzz != null) BtnGameBuzz.IsEnabled = true;
                UpdateBuzzButtonState();
            });
        }

        /// <summary>
        /// クライアント用の問題テキスト表示（スムーズに1文字ずつ）
        /// </summary>
        private void StartClientRevealLoop(string questionText)
        {
            StartClientRevealLoopFromPosition(questionText, 0);
        }

        /// <summary>
        /// クライアント用の問題テキスト表示（指定位置から再開）
        /// </summary>
        private void StartClientRevealLoopFromPosition(string questionText, int startIndex)
        {
            _clientRevealCts?.Cancel();
            _clientRevealCts = new CancellationTokenSource();
            var ct = _clientRevealCts.Token;

            _ = Task.Run(async () =>
            {
                try
                {
                    var revealIndex = startIndex;
                    var revealIntervalMs = 60;

                    while (!ct.IsCancellationRequested && revealIndex < questionText.Length)
                    {
                        // Pause while someone is answering
                        if (_gameState.PausedForBuzz)
                        {
                            await Task.Delay(100, ct);
                            continue;
                        }

                        revealIndex++;
                        var revealedText = questionText.Substring(0, revealIndex);
                        _gameState.RevealedText = revealedText;

                        Dispatcher.Invoke(() =>
                        {
                            if (!_isAnswerDialogOpen)
                            {
                                TxtGameQuestion.Text = revealedText;
                            }
                        });

                        await Task.Delay(revealIntervalMs, ct);
                    }
                }
                catch (OperationCanceledException) { }
                catch (Exception ex) { Logger.LogError(ex); }
            });
        }
    }
}
