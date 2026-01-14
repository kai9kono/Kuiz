using System;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;
using Kuiz.Models;
using Kuiz.Services;

namespace Kuiz
{
    /// <summary>
    /// ホスト関連のUI処理
    /// </summary>
    public partial class MainWindow
    {
        private bool _isHost;

        private void BtnTitleHost_Click(object sender, RoutedEventArgs e)
        {
            ShowPanel(HostPanel);
            
            // Always start fresh host service (stop if running, then start new)
            if (_hostService.IsRunning)
            {
                _ = _hostService.StopAsync();
                _isHost = false;
            }
            
            // Start new host service with new lobby code
            _ = StartHostAsync();
            
            // Load questions when entering host panel
            _ = LoadQuestionsAsync();
        }

        private void BtnHostBack_Click(object sender, RoutedEventArgs e)
        {
            Logger.LogInfo("BtnHostBack_Click called - showing leave lobby confirmation");
            // Show confirmation overlay before leaving lobby
            LeaveLobbyConfirmOverlay.Visibility = Visibility.Visible;
            LeaveLobbyConfirmOverlay.IsHitTestVisible = true;
            AnimateConfirmOverlayOpen(LeaveLobbyConfirmBorder);
        }

        private async void BtnHostLoadQuestions_Click(object sender, RoutedEventArgs e)
        {
            await LoadQuestionsAsync();
        }

        private void BtnOpenAddQuestion_Click(object sender, RoutedEventArgs e)
        {
            // Navigate to Create Question panel instead of opening window
            ShowPanel(CreateQuestionPanel);
            UpdateQuestionListCount();
        }

        private void BtnOpenManageQuestions_Click(object sender, RoutedEventArgs e)
        {
            // Navigate to Question Manager panel instead of opening window
            ShowPanel(QuestionManagerPanel);
        }

        private void BtnOpenImportWindow_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new Microsoft.Win32.OpenFileDialog
            {
                Filter = "JSON files (*.json)|*.json|All files|*.*"
            };

            if (dlg.ShowDialog() != true) return;
            _ = ImportJsonFileAsync(dlg.FileName);
        }

        private async Task ImportJsonFileAsync(string path)
        {
            try
            {
                var count = await _questionService.ImportQuestionsAsync(path);
                TxtGameStatus.Text = $"Imported {count} questions";
                await LoadQuestionsAsync();
            }
            catch (Exception ex)
            {
                Logger.LogError(ex);
                MessageBox.Show("Import error: " + ex.Message, "Import", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnHostStartGame_Click(object sender, RoutedEventArgs e)
        {
            // Validate game settings before showing confirmation
            if (!ValidateGameSettings())
            {
                return;
            }
            
            StartGameConfirm.Visibility = Visibility.Visible;
            StartGameConfirm.IsHitTestVisible = true;
            AnimateConfirmOverlayOpen(StartGameConfirmBorder);
        }
        
        private bool ValidateGameSettings()
        {
            // Validate Points to Win
            if (string.IsNullOrWhiteSpace(TxtPointsToWin.Text))
            {
                ShowToast("勝利ポイントを入力してください", isError: true);
                return false;
            }
            
            if (!int.TryParse(TxtPointsToWin.Text, out int pointsToWin) || pointsToWin <= 0)
            {
                ShowToast("勝利ポイントは1以上の数字を入力してください", isError: true);
                return false;
            }
            
            if (pointsToWin > 999)
            {
                ShowToast("勝利ポイントが大きすぎます（最大999）", isError: true);
                return false;
            }
            
            // Validate Max Mistakes
            if (string.IsNullOrWhiteSpace(TxtMaxMistakes.Text))
            {
                ShowToast("最大ミス回数を入力してください", isError: true);
                return false;
            }
            
            if (!int.TryParse(TxtMaxMistakes.Text, out int maxMistakes) || maxMistakes <= 0)
            {
                ShowToast("最大ミス回数は1以上の数字を入力してください", isError: true);
                return false;
            }
            
            if (maxMistakes > 99)
            {
                ShowToast("最大ミス回数が大きすぎます（最大99）", isError: true);
                return false;
            }
            
            // Validate Number of Questions
            if (string.IsNullOrWhiteSpace(TxtNumQuestions.Text))
            {
                ShowToast("問題数を入力してください", isError: true);
                return false;
            }
            
            if (!int.TryParse(TxtNumQuestions.Text, out int numQuestions) || numQuestions <= 0)
            {
                ShowToast("問題数は1以上の数字を入力してください", isError: true);
                return false;
            }
            
            // Check against available questions
            int availableQuestions = _questionService.Questions?.Count ?? 0;
            if (numQuestions > availableQuestions)
            {
                ShowToast($"問題数が多すぎます（利用可能: {availableQuestions}問）", isError: true);
                return false;
            }
            
            return true;
        }

        private async void BtnConfirmStartYes_Click(object sender, RoutedEventArgs e)
        {
            StartGameConfirm.Visibility = Visibility.Collapsed;
            StartGameConfirm.IsHitTestVisible = false;

            TxtGameStatus.Text = "Starting game...";

            // Start host service only if not already running
            if (!_hostService.IsRunning)
            {
                await StartHostAsync();
                if (!_hostService.IsRunning)
                {
                    TxtGameStatus.Text = "Failed to start host service";
                    return;
                }
            }

            // Ensure questions are loaded
            if (_questionService.Questions == null || _questionService.Questions.Count == 0)
            {
                await LoadQuestionsAsync();
            }
            
            // Validate again after loading questions
            if (!ValidateGameSettings())
            {
                return;
            }

            _gameState.SessionId++;
            ResetGameFlow();

            int pointsToWin = int.Parse(TxtPointsToWin.Text);
            int maxMistakes = int.Parse(TxtMaxMistakes.Text);
            _gameState.PointsToWin = pointsToWin;
            _gameState.MaxMistakes = maxMistakes;
            _gameEnded = false;

            var hostName = _profileService.PlayerName;
            if (string.IsNullOrWhiteSpace(hostName))
            {
                hostName = "Host";
                _profileService.Save(hostName);
            }
            else
            {
                _profileService.Save(hostName);
            }

            if (string.IsNullOrWhiteSpace(TxtJoinPlayerName.Text))
            {
                TxtJoinPlayerName.Text = hostName;
            }
            
            // Only reset and add host if not already in the list
            if (!_gameState.LobbyPlayers.Contains(hostName))
            {
                _gameState.AddPlayer(hostName);
            }
            
            _gameState.InitializeScores();

            int numQuestions = int.Parse(TxtNumQuestions.Text);
            _gameState.SetupPlayQueue(_questionService.Questions, numQuestions);

            // Record game start in player stats
            _playerStatsService.OnGameStarted();

            await ShowGameStartCountdownAsync();

            ShowPanel(GamePanel);
            UpdateGameUi();

            if (_gameState.PlayQueue.Count > 0)
            {
                _ = StartNextQuestionAsync();
            }

            TxtGameStatus.Text = "Game started";
        }

        private void BtnConfirmStartNo_Click(object sender, RoutedEventArgs e)
        {
            StartGameConfirm.Visibility = Visibility.Collapsed;
            StartGameConfirm.IsHitTestVisible = false;
        }

        private async Task ShowGameStartCountdownAsync()
        {
            StartGameCountdown.Visibility = Visibility.Visible;
            StartGameCountdown.IsHitTestVisible = true;

            for (int i = 3; i >= 1; i--)
            {
                Dispatcher.Invoke(() => TxtStartCountdown.Text = i.ToString());
                await Task.Delay(1000);
            }

            StartGameCountdown.Visibility = Visibility.Collapsed;
            StartGameCountdown.IsHitTestVisible = false;
        }

        private async Task StartHostAsync()
        {
            var (success, error, actualUrl, lobbyCode) = await _hostService.StartAsync();

            if (!success)
            {
                TxtGameStatus.Text = "Failed to start listener: " + error;

                if (error?.Contains("Access denied") == true)
                {
                    try { Clipboard.SetText(error); } catch { }
                    MessageBox.Show(error, "Permission required", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
                return;
            }

            _isHost = true;

            // Always update lobby code display
            Dispatcher.Invoke(() =>
            {
                if (!string.IsNullOrWhiteSpace(lobbyCode))
                {
                    TxtLobbyCode.Text = lobbyCode;
                }
                UpdatePlayerCountDisplay();
            });

            var hostName = _profileService.PlayerName;
            if (string.IsNullOrWhiteSpace(hostName))
            {
                hostName = "Host";
                _profileService.Save(hostName);
            }

            if (string.IsNullOrWhiteSpace(TxtJoinPlayerName.Text))
            {
                TxtJoinPlayerName.Text = hostName;
            }
            
            // Clear existing players and add host with current name
            _gameState.LobbyPlayers.Clear();
            _gameState.AddPlayer(hostName);
            UpdateLobbyUi();
        }

        private void UpdatePlayerCountDisplay()
        {
            var count = _gameState.LobbyPlayers.Count;
            TxtPlayerCount.Text = $"({count}/{HostService.MaxPlayers})";
        }
        
        private void TxtLobbyCode_Click(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            try
            {
                var code = TxtLobbyCode.Text;
                if (!string.IsNullOrWhiteSpace(code) && code != "------")
                {
                    Clipboard.SetText(code);
                    ShowToast("ロビーコードをコピーしました！");
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex);
                ShowToast("コピーに失敗しました", isError: true);
            }
        }
        
        private async void ShowToast(string message, bool isError = false)
        {
            Dispatcher.Invoke(() =>
            {
                ToastText.Text = message;
                ToastNotification.Background = new System.Windows.Media.SolidColorBrush(
                    isError ? System.Windows.Media.Color.FromRgb(255, 205, 210) : System.Windows.Media.Color.FromRgb(76, 175, 80)
                );
                
                // Change text color for error toast
                ToastText.Foreground = new System.Windows.Media.SolidColorBrush(
                    isError ? System.Windows.Media.Color.FromRgb(183, 28, 28) : System.Windows.Media.Colors.White
                );
                
                // Change icon for error toast
                var iconKind = isError ? MaterialDesignThemes.Wpf.PackIconKind.AlertCircle : MaterialDesignThemes.Wpf.PackIconKind.Check;
                var icon = ToastNotification.FindName("ToastIcon") as MaterialDesignThemes.Wpf.PackIcon;
                if (icon != null)
                {
                    icon.Kind = iconKind;
                    icon.Foreground = new System.Windows.Media.SolidColorBrush(
                        isError ? System.Windows.Media.Color.FromRgb(183, 28, 28) : System.Windows.Media.Colors.White
                    );
                }
                
                ToastNotification.Visibility = Visibility.Visible;
                
                // Slide in animation
                var slideIn = new System.Windows.Media.Animation.DoubleAnimation
                {
                    From = 400,
                    To = 0,
                    Duration = TimeSpan.FromMilliseconds(300),
                    EasingFunction = new System.Windows.Media.Animation.CubicEase { EasingMode = System.Windows.Media.Animation.EasingMode.EaseOut }
                };
                ToastTransform.BeginAnimation(System.Windows.Media.TranslateTransform.XProperty, slideIn);
            });
            
            await Task.Delay(2500);
            
            // Slide out animation
            Dispatcher.Invoke(() =>
            {
                var slideOut = new System.Windows.Media.Animation.DoubleAnimation
                {
                    From = 0,
                    To = 400,
                    Duration = TimeSpan.FromMilliseconds(250),
                    EasingFunction = new System.Windows.Media.Animation.CubicEase { EasingMode = System.Windows.Media.Animation.EasingMode.EaseIn }
                };
                slideOut.Completed += (s, e) =>
                {
                    ToastNotification.Visibility = Visibility.Collapsed;
                };
                ToastTransform.BeginAnimation(System.Windows.Media.TranslateTransform.XProperty, slideOut);
            });
        }

        private void SetupHostServiceEvents()
        {
            _hostService.OnPlayerRegistered = async (name) =>
            {
                if (_gameState.LobbyPlayers.Count >= HostService.MaxPlayers)
                {
                    return false;
                }
                
                _gameState.AddPlayer(name);
                Dispatcher.Invoke(() =>
                {
                    UpdateLobbyUi();
                    UpdatePlayerCountDisplay();
                });
                return true;
            };

            _hostService.OnBuzzReceived = async (name) =>
            {
                return _gameState.ProcessBuzz(name);
            };

            _hostService.OnAnswerReceived = async (name, answer) =>
            {
                var correct = _gameState.ProcessAnswer(name, answer);
                
                // Record stats for the current player
                var myName = _profileService.PlayerName;
                if (name == myName)
                {
                    if (correct)
                    {
                        _playerStatsService.OnCorrectAnswer();
                    }
                    else
                    {
                        _playerStatsService.OnMistake();
                    }
                }

                if (correct)
                {
                    _correctOverlayShown = true;
                    _gameState.PausedForBuzz = true;

                    Dispatcher.Invoke(() =>
                    {
                        TxtOverlayStatus.Text = "正解！";
                        TxtOverlayDetail.Text = string.Empty;
                        ResultOverlay.Visibility = Visibility.Visible;
                        ResultOverlay.IsHitTestVisible = true;
                        AnimateOverlayOpen(ResultOverlayBorder);
                        UpdateGameUi();
                    });

                    await Task.Delay(1000);
                    HideOverlay();
                    _gameState.PausedForBuzz = false;
                }
                else
                {
                    var mistakes = _gameState.Mistakes.GetValueOrDefault(name, 0);
                    Dispatcher.Invoke(() =>
                    {
                        TxtGameStatus.Text = $"不正解: {name} (ミス数: {mistakes})";
                        TxtOverlayStatus.Text = "不正解...";
                        //TxtOverlayDetail.Text = $"ミス数: {mistakes}";
                        ResultOverlay.Visibility = Visibility.Visible;
                        ResultOverlay.IsHitTestVisible = true;
                        AnimateOverlayOpen(ResultOverlayBorder);
                    });

                    await Task.Delay(1500);
                    HideOverlay();
                    UpdateGameUi();
                }

                await HandleGameEndAsync(ensureAnswerReveal: true);
                return correct;
            };

            _hostService.OnStateRequested = async () =>
            {
                return new
                {
                    questionIndex = _gameState.QueuePosition,
                    revealed = _gameState.RevealedText,
                    buzzOrder = _gameState.BuzzOrder.ToList(),
                    scores = _gameState.Scores.ToDictionary(kv => kv.Key, kv => kv.Value),
                    mistakes = _gameState.Mistakes.ToDictionary(kv => kv.Key, kv => kv.Value),
                    sessionId = _gameState.SessionId
                };
            };

            _hostService.OnNextQuestionRequested = async () =>
            {
                _gameState.BuzzOrder.Clear();
                await StartNextQuestionAsync();
                UpdateGameUi();
            };
        }

        private async Task LoadQuestionsAsync()
        {
            try
            {
                await _questionService.LoadQuestionsAsync(new Progress<int>(p =>
                {
                    Dispatcher.Invoke(() => TxtGameStatus.Text = $"Loading... {p}%");
                }));

                TxtGameStatus.Text = $"Loaded {_questionService.Questions.Count} questions";
            }
            catch (Exception ex)
            {
                TxtGameStatus.Text = "DB error: " + ex.Message;
                MessageBox.Show("DB error: " + ex.Message, "DB", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void UpdateLobbyUi()
        {
            Dispatcher.Invoke(() =>
            {
                ListLobbyPlayers.ItemsSource = _gameState.LobbyPlayers.Select(p => new
                {
                    Name = p,
                    ColorBrush = _gameState.EnsurePlayerColor(p)
                }).ToList();
                
                if (_isHost)
                {
                    UpdatePlayerCountDisplay();
                }
            });
        }

        private async void BtnRefreshQuestions_Click(object sender, RoutedEventArgs e) => await LoadQuestionsAsync();

        private async void BtnTestDb_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                await _questionService.TestConnectionAsync();
                MessageBox.Show("Connected to default DB successfully", "DB Test", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex);
                MessageBox.Show($"DB connection failed:\n{ex}", "DB Test - Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnMarkCorrect_Click(object sender, RoutedEventArgs e)
        {
            if (_gameState.BuzzOrder.Count == 0) return;

            var player = _gameState.BuzzOrder[0];
            if (!_gameState.Scores.ContainsKey(player)) _gameState.Scores[player] = 0;
            _gameState.Scores[player]++;
            _gameState.BuzzOrder.Clear();
            UpdateGameUi();
        }

        private void BtnMarkWrong_Click(object sender, RoutedEventArgs e)
        {
            if (_gameState.BuzzOrder.Count == 0) return;

            var player = _gameState.BuzzOrder[0];
            if (!_gameState.Mistakes.ContainsKey(player)) _gameState.Mistakes[player] = 0;
            _gameState.Mistakes[player]++;
            _gameState.BuzzOrder.RemoveAt(0);

            if (int.TryParse(TxtMaxMistakes.Text, out var max) && _gameState.Mistakes[player] >= max)
            {
                _gameState.LobbyPlayers.Remove(player);
                _gameState.Scores.Remove(player);
                _gameState.Mistakes.Remove(player);
            }

            UpdateGameUi();
        }

        public void RefreshAndLoadQuestions()
        {
            Dispatcher.Invoke(async () => await LoadQuestionsAsync());
        }

        private void BtnDecreasePointsToWin_Click(object sender, RoutedEventArgs e)
        {
            if (int.TryParse(TxtPointsToWin.Text, out int value) && value > 1)
            {
                TxtPointsToWin.Text = (value - 1).ToString();
            }
        }

        private void BtnIncreasePointsToWin_Click(object sender, RoutedEventArgs e)
        {
            if (int.TryParse(TxtPointsToWin.Text, out int value) && value < 999)
            {
                TxtPointsToWin.Text = (value + 1).ToString();
            }
        }

        private void BtnDecreaseMaxMistakes_Click(object sender, RoutedEventArgs e)
        {
            if (int.TryParse(TxtMaxMistakes.Text, out int value) && value > 1)
            {
                TxtMaxMistakes.Text = (value - 1).ToString();
            }
        }

        private void BtnIncreaseMaxMistakes_Click(object sender, RoutedEventArgs e)
        {
            if (int.TryParse(TxtMaxMistakes.Text, out int value) && value < 99)
            {
                TxtMaxMistakes.Text = (value + 1).ToString();
            }
        }

        private void BtnDecreaseNumQuestions_Click(object sender, RoutedEventArgs e)
        {
            if (int.TryParse(TxtNumQuestions.Text, out int value) && value > 1)
            {
                TxtNumQuestions.Text = (value - 1).ToString();
            }
        }

        private void BtnIncreaseNumQuestions_Click(object sender, RoutedEventArgs e)
        {
            if (int.TryParse(TxtNumQuestions.Text, out int value))
            {
                int availableQuestions = _questionService.Questions?.Count ?? 0;
                if (value < availableQuestions)
                {
                    TxtNumQuestions.Text = (value + 1).ToString();
                }
            }
        }
    }
}
