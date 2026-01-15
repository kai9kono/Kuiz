using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using Kuiz.Models;
using Kuiz.Services;

namespace Kuiz
{
    /// <summary>
    /// ゲーム関連のUI処理
    /// </summary>
    public partial class MainWindow
    {
        private int _revealIntervalMs = 60;
        private int _fastRevealIntervalMs = 15;
        private Task? _revealTask;
        private CancellationTokenSource? _revealCts;
        private bool _isAnswerDialogOpen;
        private bool _isPreDisplay;
        private bool _correctOverlayShown;
        private bool _gameEnded;

        // Answer overlay support
        private TaskCompletionSource<string?>? _answerOverlayTcs;
        private CancellationTokenSource? _answerOverlayCts;
        private int _overlaySecondsRemaining;

        // Prevent concurrent advances
        private int _isAdvancing;

        private DateTime _lastSpaceTime = DateTime.MinValue;
        private readonly TimeSpan _spaceCooldown = TimeSpan.FromMilliseconds(500);

        private async void BtnGameBuzz_Click(object sender, RoutedEventArgs e) => await HandleBuzzAsync();
        private async void BtnBuzz_Click(object sender, RoutedEventArgs e) => await HandleBuzzAsync();

        private async Task HandleBuzzAsync()
        {
            try
            {
                if (_isPreDisplay)
                {
                    TxtGameStatus.Text = "Please wait...";
                    return;
                }

                Logger.LogInfo("Buzz invoked");
                TxtGameStatus.Text = "Buzz pressed";

                var name = _profileService.PlayerName ?? TxtJoinPlayerName.Text.Trim();
                if (string.IsNullOrEmpty(name))
                {
                    TxtGameStatus.Text = "Enter player name first";
                    return;
                }

                if (_isHost)
                {
                    await HandleHostBuzzAsync(name);
                }
                else
                {
                    await HandleClientBuzzAsync(name);
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex);
                TxtGameStatus.Text = "Unexpected error: " + ex.Message;
            }
        }

        private async Task HandleHostBuzzAsync(string name)
        {
            if (_gameState.PausedForBuzz || _gameState.BuzzOrder.Count > 0)
            {
                TxtGameStatus.Text = "Already answering";
                return;
            }

            if (_gameState.AttemptedThisQuestion.Contains(name))
            {
                TxtGameStatus.Text = "You already attempted this question";
                return;
            }

            _gameState.ProcessBuzz(name);
            UpdateGameUi();
            TxtGameStatus.Text = "Waiting for answer...";

            var answer = await ShowAnswerDialog(10);

            if (answer != null)
            {
                Logger.LogInfo("Answer entered: " + answer);
                await ProcessAnswerLocalAsync(name, answer);
            }
            else
            {
                // Timeout - mistake already counted in CleanupAnswerOverlay
                Logger.LogInfo($"?? No answer from {name} (timeout)");
                _gameState.PausedForBuzz = false;
                _gameState.BuzzOrder.Clear();
                UpdateGameUi();
            }
        }

        private async Task HandleClientBuzzAsync(string name)
        {
            if (_gameState.PausedForBuzz || _gameState.BuzzOrder.Count > 0)
            {
                TxtGameStatus.Text = "Already answering";
                return;
            }

            if (_gameState.AttemptedThisQuestion.Contains(name))
            {
                TxtGameStatus.Text = "You already attempted this question";
                return;
            }

            try
            {
                // Send buzz via SignalR
                await _signalRClient.SendBuzzAsync();
                Logger.LogInfo("?? Buzz sent via SignalR");
                
                // Add to local buzz order for immediate feedback
                _gameState.ProcessBuzz(name);
                UpdateGameUi();
                TxtGameStatus.Text = "Waiting for answer...";

                var answer = await ShowAnswerDialog(10);

                if (answer != null)
                {
                    // Send answer via SignalR
                    await _signalRClient.SendAnswerAsync(answer);
                    Logger.LogInfo($"?? Answer sent via SignalR: {answer}");
                    TxtGameStatus.Text = "Answer sent";
                }
                else
                {
                    Dispatcher.Invoke(() =>
                    {
                        BtnGameBuzz.IsEnabled = false;
                        UpdateBuzzButtonState();
                    });
                }
            }
            catch (Exception ex)
            {
                TxtGameStatus.Text = "Buzz error: " + ex.Message;
                Logger.LogError(ex);
            }
        }

        private async Task ProcessAnswerLocalAsync(string name, string answer)
        {
            var correct = _gameState.ProcessAnswer(name, answer);

            if (correct)
            {
                _soundService.PlayCorrect();
                // Set flag to prevent duplicate overlay in ShowCorrectAnswerSequenceAsync
                _correctOverlayShown = true;
                
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
            }
            else
            {
                _soundService.PlayIncorrect();
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
                if (await HandleGameEndAsync(ensureAnswerReveal: true)) return;
            }
        }

        private async Task<string?> ShowAnswerDialog(int secondsTimeout)
        {
            var currentAnswering = _gameState.BuzzOrder.Count > 0 ? _gameState.BuzzOrder[0] : null;
            var myName = _profileService.PlayerName ?? TxtJoinPlayerName.Text.Trim();

            if (!string.IsNullOrEmpty(currentAnswering) && currentAnswering != myName)
            {
                return await ShowWaitingOverlayAsync(currentAnswering, secondsTimeout);
            }

            return await ShowAnswerInputOverlayAsync(secondsTimeout);
        }

        private async Task<string?> ShowWaitingOverlayAsync(string answeringPlayer, int seconds)
        {
            Dispatcher.Invoke(() =>
            {
                TxtOverlayTitle.Text = "Waiting";
                TxtOverlayInfo.Text = $"{answeringPlayer} が回答中...";
                TxtOverlayInfo.Visibility = Visibility.Visible;
                TxtOverlayAnswer.Visibility = Visibility.Collapsed;
                TxtOverlayTimer.Text = string.Empty;
                AnswerOverlay.Visibility = Visibility.Visible;
                AnswerOverlay.IsHitTestVisible = false;
                AnimateOverlayOpen(AnswerOverlayBorder);
            });

            await Task.Delay(seconds * 1000);

            Dispatcher.Invoke(() =>
            {
                AnswerOverlay.Visibility = Visibility.Collapsed;
                TxtOverlayInfo.Visibility = Visibility.Collapsed;
                TxtOverlayAnswer.Visibility = Visibility.Visible;
            });

            return null;
        }

        private async Task<string?> ShowAnswerInputOverlayAsync(int secondsTimeout)
        {
            try
            {
                Logger.LogInfo($"ShowAnswerDialog (overlay) start ({secondsTimeout}s)");

                Dispatcher.Invoke(() =>
                {
                    _gameState.PausedForBuzz = true;
                    _isAnswerDialogOpen = true;
                    UpdateGameUi();

                    var answerer = _gameState.BuzzOrder.Count > 0 ? _gameState.BuzzOrder[0] : GetCurrentQuestionHolder();
                    TxtAnsweringBadge.Text = $"回答中: {answerer}";
                    TxtAnsweringBadge.Visibility = Visibility.Visible;

                    TxtOverlayInfo.Visibility = Visibility.Collapsed;
                    TxtOverlayAnswer.Text = string.Empty;
                    TxtOverlayAnswer.Visibility = Visibility.Visible;
                    TxtOverlayTimer.Text = $"{secondsTimeout}s";
                    AnswerOverlay.Visibility = Visibility.Visible;
                    AnswerOverlay.IsHitTestVisible = true;
                    AnimateOverlayOpen(AnswerOverlayBorder);
                    TxtOverlayAnswer.Focus();
                });

                _answerOverlayTcs = new TaskCompletionSource<string?>();
                _answerOverlayCts?.Cancel();
                _answerOverlayCts = new CancellationTokenSource();
                _overlaySecondsRemaining = secondsTimeout;

                _ = RunCountdownAsync();

                return await _answerOverlayTcs.Task;
            }
            finally
            {
                CleanupAnswerOverlay();
            }
        }

        private async Task RunCountdownAsync()
        {
            try
            {
                while (_overlaySecondsRemaining > 0 && !_answerOverlayCts!.Token.IsCancellationRequested)
                {
                    await Task.Delay(1000, _answerOverlayCts.Token);
                    _overlaySecondsRemaining--;
                    Dispatcher.Invoke(() => TxtOverlayTimer.Text = $"{_overlaySecondsRemaining}s");
                }

                if (!_answerOverlayCts!.Token.IsCancellationRequested)
                {
                    _answerOverlayTcs?.TrySetResult(null);
                }
            }
            catch (OperationCanceledException) { }
            catch (Exception ex) { Logger.LogError(ex); }
        }

        private void CleanupAnswerOverlay()
        {
            Dispatcher.Invoke(() =>
            {
                AnswerOverlay.Visibility = Visibility.Collapsed;
                AnswerOverlay.IsHitTestVisible = false;
                TxtOverlayTimer.Text = string.Empty;
                TxtOverlayAnswer.Text = string.Empty;
                TxtOverlayInfo.Visibility = Visibility.Collapsed;
                TxtAnsweringBadge.Visibility = Visibility.Collapsed;
                TxtOverlayAnswer.Visibility = Visibility.Visible;
            });

            _isAnswerDialogOpen = false;
            _gameState.PausedForBuzz = false;
            _answerOverlayCts?.Cancel();
            _answerOverlayCts = null;
            _answerOverlayTcs = null;

            // Count timeout as mistake - but only if not already counted
            var current = _gameState.BuzzOrder.Count > 0 ? _gameState.BuzzOrder[0] : null;
            if (current != null && !_gameState.AttemptedThisQuestion.Contains(current))
            {
                Logger.LogInfo($"?? Answer timeout for {current} - counting as mistake");
                _gameState.AttemptedThisQuestion.Add(current);
                
                // Initialize mistakes if not present
                if (!_gameState.Mistakes.ContainsKey(current))
                {
                    _gameState.Mistakes[current] = 0;
                }
                
                // Only increment if this exact timeout hasn't been counted yet
                _gameState.Mistakes[current]++;
                Logger.LogInfo($"   {current} now has {_gameState.Mistakes[current]} mistake(s)");
                
                UpdateGameUi();
            }
            else if (current != null)
            {
                Logger.LogInfo($"?? Answer timeout for {current} - already attempted, not counting again");
            }

            _ = HandleGameEndAsync(ensureAnswerReveal: true);
        }

        private void TxtOverlayAnswer_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                e.Handled = true;
                var text = TxtOverlayAnswer.Text?.Trim();
                _answerOverlayTcs?.TrySetResult(string.IsNullOrWhiteSpace(text) ? null : text);
                _answerOverlayCts?.Cancel();
            }
            else if (e.Key == Key.Escape)
            {
                e.Handled = true;
                
                // Show incorrect feedback when ESC is pressed
                var current = _gameState.BuzzOrder.Count > 0 ? _gameState.BuzzOrder[0] : null;
                if (current != null)
                {
                    _soundService.PlayIncorrect();
                    
                    Dispatcher.Invoke(() =>
                    {
                        // Close answer overlay first
                        AnswerOverlay.Visibility = Visibility.Collapsed;
                        AnswerOverlay.IsHitTestVisible = false;
                        TxtAnsweringBadge.Visibility = Visibility.Collapsed;
                        
                        // Show incorrect overlay
                        TxtOverlayStatus.Text = "不正解...";
                        TxtOverlayDetail.Text = string.Empty;
                        ResultOverlay.Visibility = Visibility.Visible;
                        ResultOverlay.IsHitTestVisible = true;
                        AnimateOverlayOpen(ResultOverlayBorder);
                    });
                    
                    // Hide overlay after delay
                    _ = Task.Run(async () =>
                    {
                        await Task.Delay(1500);
                        Dispatcher.Invoke(() =>
                        {
                            ResultOverlay.Visibility = Visibility.Collapsed;
                            ResultOverlay.IsHitTestVisible = false;
                        });
                    });
                }
                
                _answerOverlayTcs?.TrySetResult(null);
                _answerOverlayCts?.Cancel();
            }
        }

        private async Task StartNextQuestionAsync()
        {
            // Cancel any active answer dialog before starting new question
            _answerOverlayCts?.Cancel();
            _answerOverlayTcs?.TrySetCanceled();
            
            // Force cleanup of answer overlay
            Dispatcher.Invoke(() =>
            {
                AnswerOverlay.Visibility = Visibility.Collapsed;
                AnswerOverlay.IsHitTestVisible = false;
                TxtAnsweringBadge.Visibility = Visibility.Collapsed;
                _isAnswerDialogOpen = false;
            });

            if (_gameEnded || await HandleGameEndAsync())
            {
                return;
            }

            if (Interlocked.Exchange(ref _isAdvancing, 1) == 1)
            {
                Logger.LogInfo("StartNextQuestionAsync skipped due to concurrent advance");
                return;
            }

            try
            {
                _gameState.BuzzOrder.Clear();
                _gameState.QueuePosition++;
                _correctOverlayShown = false; // Reset flag for new question

                var question = _gameState.DequeueNextQuestion();
                if (question == null)
                {
                    var winner = _gameState.GetWinner();
                    Logger.LogInfo($"Play queue exhausted, winner='{winner}'");
                    _revealCts?.Cancel();
                    ShowResult(winner ?? "No winner");
                    return;
                }

                // Record question to history
                _ = RecordQuestionPlayedAsync(question);

                await ShowPreDisplayBannerAsync();
                StartRevealLoop();
            }
            finally
            {
                Interlocked.Exchange(ref _isAdvancing, 0);
            }
        }

        private async Task ShowPreDisplayBannerAsync()
        {
            var prevAlign = System.Windows.TextAlignment.Left;
            var prevWeight = FontWeights.Normal;

            Dispatcher.Invoke(() =>
            {
                _isPreDisplay = true;
                prevAlign = TxtGameQuestion.TextAlignment;
                prevWeight = TxtGameQuestion.FontWeight;
                // Show current question number (incremental)
                var currentQuestionNum = _gameState.QueuePosition + 1;
                TxtGameQuestion.Text = $"第{currentQuestionNum}問";
                TxtGameQuestion.TextAlignment = System.Windows.TextAlignment.Center;
                TxtGameQuestion.FontWeight = FontWeights.Bold;
                if (BtnGameBuzz != null) BtnGameBuzz.IsEnabled = false;
                _soundService.PlayQuestion();
            });

            await Task.Delay(2000);

            Dispatcher.Invoke(() =>
            {
                _isPreDisplay = false;
                TxtGameQuestion.Text = string.Empty;
                TxtGameQuestion.TextAlignment = prevAlign;
                TxtGameQuestion.FontWeight = prevWeight;
                
                if (BtnGameBuzz != null) BtnGameBuzz.IsEnabled = true;
                UpdateBuzzButtonState();
            });
        }

        private void StartRevealLoop()
        {
            _revealCts?.Cancel();
            _revealCts = new CancellationTokenSource();
            _revealTask = Task.Run(() => RevealLoopAsync(_revealCts.Token));
        }

        private async Task RevealLoopAsync(CancellationToken ct)
        {
            try
            {
                var current = _gameState.CurrentQuestion;
                if (current == null) return;

                var fullText = current.Text;
                int lastBroadcastIndex = -1;

                while (!ct.IsCancellationRequested && _gameState.RevealIndex < fullText.Length)
                {
                    if (_gameState.PausedForBuzz)
                    {
                        await Task.Delay(100, ct);
                        continue;
                    }

                    _gameState.RevealIndex++;
                    _gameState.RevealedText = fullText.Substring(0, _gameState.RevealIndex);
                    UpdateGameUi();
                    
                    // If host, broadcast game state to clients via SignalR every 3 characters or on last character
                    if (_isHost && _hostService.IsRunning && 
                        (_gameState.RevealIndex - lastBroadcastIndex >= 3 || _gameState.RevealIndex >= fullText.Length))
                    {
                        try
                        {
                            var gameState = new
                            {
                                revealedText = _gameState.RevealedText,
                                revealIndex = _gameState.RevealIndex,
                                totalLength = fullText.Length,
                                scores = _gameState.Scores.ToDictionary(kv => kv.Key, kv => kv.Value),
                                mistakes = _gameState.Mistakes.ToDictionary(kv => kv.Key, kv => kv.Value)
                            };
                            await _hostService.NotifyGameStateAsync(gameState);
                            lastBroadcastIndex = _gameState.RevealIndex;
                        }
                        catch (Exception ex)
                        {
                            Logger.LogError($"Failed to broadcast game state: {ex.Message}");
                        }
                    }

                    var delayMs = _gameState.FastReveal ? _fastRevealIntervalMs : _revealIntervalMs;
                    await Task.Delay(delayMs, ct);
                }

                if (!ct.IsCancellationRequested)
                {
                    // Record the time when question reveal completed
                    _gameState.RevealCompletedTime = DateTime.UtcNow;
                    Logger.LogInfo($"?? Question reveal completed at {_gameState.RevealCompletedTime:HH:mm:ss.fff}");
                    
                    // Final broadcast with complete text
                    if (_isHost && _hostService.IsRunning)
                    {
                        try
                        {
                            var gameState = new
                            {
                                revealedText = _gameState.RevealedText,
                                revealIndex = _gameState.RevealIndex,
                                totalLength = fullText.Length,
                                revealCompleted = true,
                                scores = _gameState.Scores.ToDictionary(kv => kv.Key, kv => kv.Value),
                                mistakes = _gameState.Mistakes.ToDictionary(kv => kv.Key, kv => kv.Value)
                            };
                            await _hostService.NotifyGameStateAsync(gameState);
                        }
                        catch (Exception ex)
                        {
                            Logger.LogError($"Failed to broadcast final game state: {ex.Message}");
                        }
                    }
                    
                    await HandleRevealCompleteAsync(ct);
                }
            }
            catch (OperationCanceledException) { }
            catch (Exception ex) { Logger.LogError(ex); }
        }

        private async Task HandleRevealCompleteAsync(CancellationToken ct)
        {
            var question = _gameState.CurrentQuestion;
            if (question == null) return;

            if (_gameState.CorrectAnswered)
            {
                await ShowCorrectAnswerSequenceAsync(question, ct);
            }
            else
            {
                // Wait for any ongoing answer dialog to complete before showing answer
                await WaitForAnsweringPlayerAsync(ct);
                
                // Wait for the "next question" timer (5 seconds)
                await ShowNextTimerAsync(5, ct);
                
                // タイマー終了時の最終チェック: 誰かが回答中か確認
                Logger.LogInfo("?? Timer ended - checking if anyone is answering...");
                
                // タイマー終了直前にバズされた可能性があるので、少し待つ
                if (_isAnswerDialogOpen || _gameState.PausedForBuzz)
                {
                    Logger.LogInfo("?? Someone buzzed near timer end - waiting for their answer...");
                    
                    // 回答ダイアログが完了するまで待機 (最大15秒)
                    var waitStartTime = DateTime.UtcNow;
                    var maxWaitTime = TimeSpan.FromSeconds(15);
                    
                    while (!ct.IsCancellationRequested && 
                           (_isAnswerDialogOpen || _gameState.PausedForBuzz) &&
                           DateTime.UtcNow - waitStartTime < maxWaitTime)
                    {
                        await Task.Delay(100, ct);
                    }
                    
                    if (_isAnswerDialogOpen || _gameState.PausedForBuzz)
                    {
                        Logger.LogInfo("?? Wait timeout - proceeding to show answer");
                    }
                    else
                    {
                        Logger.LogInfo("? Answer completed - now showing correct answer");
                    }
                }
                
                // 誰かが正解していないか最終確認
                if (!_gameState.CorrectAnswered)
                {
                    await ShowAnswerRevealAsync(question, ct);
                }
                else
                {
                    Logger.LogInfo("? Someone answered correctly during wait - skipping answer reveal");
                }
            }

            _gameState.BuzzOrder.Clear();
            if (_gameEnded || await HandleGameEndAsync())
            {
                return;
            }
            await StartNextQuestionAsync();
            UpdateGameUi();
        }

        private async Task ShowCorrectAnswerSequenceAsync(Question question, CancellationToken ct)
        {
            if (!_correctOverlayShown)
            {
                Dispatcher.Invoke(() =>
                {
                    TxtOverlayStatus.Text = "正解！";
                    TxtOverlayDetail.Text = string.Empty;
                    ResultOverlay.Visibility = Visibility.Visible;
                    ResultOverlay.IsHitTestVisible = true;
                    AnimateOverlayOpen(ResultOverlayBorder);
                    UpdateGameUi();
                });

                await Task.Delay(1000, ct);
                HideOverlay();
            }

            _correctOverlayShown = false;

            Dispatcher.Invoke(() =>
            {
                TxtAnswerReveal.Text = $"答え：{question.Answer}";
                TxtAnswerReveal.Visibility = Visibility.Visible;
                UpdateGameUi();
            });

            await Task.Delay(3000, ct);
            Dispatcher.Invoke(() => TxtAnswerReveal.Visibility = Visibility.Collapsed);
        }

        private async Task WaitForAnsweringPlayerAsync(CancellationToken ct)
        {
            var timeout = TimeSpan.FromSeconds(15); // Maximum wait time for answer
            var startTime = DateTime.UtcNow;
            
            while (!ct.IsCancellationRequested && _gameState.PausedForBuzz)
            {
                // Check if we've exceeded timeout
                if (DateTime.UtcNow - startTime > timeout)
                {
                    Logger.LogInfo("?? WaitForAnsweringPlayer timeout - continuing to next question");
                    break;
                }
                
                try { await Task.Delay(100, ct); }
                catch (OperationCanceledException) { break; }
            }
        }

        private async Task ShowAnswerRevealAsync(Question question, CancellationToken ct)
        {
            Dispatcher.Invoke(() =>
            {
                TxtAnswerReveal.Text = $"答え：{question.Answer}";
                TxtAnswerReveal.Visibility = Visibility.Visible;
                UpdateGameUi();
            });

            await Task.Delay(3000, ct);
            Dispatcher.Invoke(() => TxtAnswerReveal.Visibility = Visibility.Collapsed);
        }

        private async Task ShowNextTimerAsync(int seconds, CancellationToken ct)
        {
            if (seconds <= 0) return;

            // タイマー中もバズは有効 (タイマー終了時にチェック)
            Logger.LogInfo("?? Answer reveal timer started (buzzing still allowed)");

            const int intervalMs = 30;
            var totalMs = seconds * 1000;
            var remainingMs = totalMs;

            Dispatcher.Invoke(() =>
            {
                PbNextTimer.Minimum = 0;
                PbNextTimer.Maximum = 1;
                PbNextTimer.Value = 1;
                PbNextTimer.Visibility = Visibility.Visible;
            });

            try
            {
                while (remainingMs > 0 && !ct.IsCancellationRequested)
                {
                    if (_gameState.PausedForBuzz)
                    {
                        await Task.Delay(100, ct);
                        continue;
                    }

                    // Check if someone answered correctly during timer
                    if (_gameState.CorrectAnswered)
                    {
                        // Skip to end immediately
                        remainingMs = 0;
                        Dispatcher.Invoke(() => PbNextTimer.Value = 0);
                        break;
                    }

                    await Task.Delay(intervalMs, ct);
                    remainingMs -= intervalMs;
                    if (remainingMs < 0) remainingMs = 0;

                    var val = remainingMs / (double)totalMs;
                    Dispatcher.Invoke(() => PbNextTimer.Value = val);
                }
            }
            finally
            {
                Dispatcher.Invoke(() =>
                {
                    PbNextTimer.Visibility = Visibility.Collapsed;
                    PbNextTimer.Value = 0;
                });
            }
        }

        private void UpdateGameUi()
        {
            Dispatcher.Invoke(() =>
            {
                // Ensure settings from UI (max mistakes) are applied before building player states
                ApplySettingsFromUi();

                ListPlayersInGame.ItemsSource = _gameState.GetPlayerStates();

                if (!_isAnswerDialogOpen && !_isPreDisplay)
                {
                    TxtGameQuestion.Text = _gameState.RevealedText;
                }

                UpdateBuzzButtonState();
            });
        }

        private void ApplySettingsFromUi()
        {
            if (int.TryParse(TxtMaxMistakes?.Text, out var max) && max > 0)
            {
                _gameState.MaxMistakes = max;
            }
            else
            {
                _gameState.MaxMistakes = 3;
            }
        }

        private void SetBuzzImage(string state)
        {
            if (ImgBuzz == null) return;

            string filename = state == "pressed" ? "button-pressed.png" : state == "unavailable" ? "button-unavailable.png" : "button-unpressed.png";
            string[] tryUris = new[]
            {
                $"pack://application:,,,/Kuiz;component/Resources/img/{filename}",
                $"/Kuiz;component/Resources/img/{filename}",
                $"Resources/img/{filename}"
            };

            System.Windows.Media.ImageSource? loaded = null;

            foreach (var u in tryUris)
            {
                try
                {
                    var bmi = new System.Windows.Media.Imaging.BitmapImage();
                    bmi.BeginInit();
                    bmi.UriSource = new Uri(u, UriKind.RelativeOrAbsolute);
                    bmi.CacheOption = System.Windows.Media.Imaging.BitmapCacheOption.OnLoad;
                    bmi.CreateOptions = System.Windows.Media.Imaging.BitmapCreateOptions.IgnoreImageCache;
                    bmi.EndInit();
                    bmi.Freeze();
                    loaded = bmi;
                    break;
                }
                catch { /* try next */ }
            }

            // Try loading via ResourceStream as a last resort
            if (loaded == null)
            {
                try
                {
                    var resUri = new Uri($"/Kuiz;component/Resources/img/{filename}", UriKind.Relative);
                    var sri = Application.GetResourceStream(resUri);
                    if (sri != null)
                    {
                        var bmi2 = new System.Windows.Media.Imaging.BitmapImage();
                        bmi2.BeginInit();
                        bmi2.StreamSource = sri.Stream;
                        bmi2.CacheOption = System.Windows.Media.Imaging.BitmapCacheOption.OnLoad;
                        bmi2.EndInit();
                        bmi2.Freeze();
                        loaded = bmi2;
                    }
                }
                catch { }
            }

            // Fallback to DrawingImage resource
            if (loaded == null)
            {
                try
                {
                    string imgKey = state == "pressed" ? "BuzzPressedImage" : state == "unavailable" ? "BuzzUnavailableImage" : "BuzzUnpressedImage";
                    if (Application.Current.Resources.Contains(imgKey))
                    {
                        loaded = (System.Windows.Media.ImageSource)Application.Current.Resources[imgKey];
                    }
                }
                catch { }
            }

            if (loaded != null)
            {
                ImgBuzz.Source = loaded;
                ImgBuzz.Visibility = Visibility.Visible;
            }
        }

        private void BtnGameBuzz_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            SetBuzzImage("pressed");
            _soundService.PlayBuzz();
        }

        private void BtnGameBuzz_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            // restore based on state
            UpdateBuzzButtonState();
        }

        private void BtnGameBuzz_LostMouseCapture(object sender, MouseEventArgs e)
        {
            UpdateBuzzButtonState();
        }

        private void UpdateBuzzButtonState()
        {
            var myName = _profileService.PlayerName ?? TxtJoinPlayerName?.Text?.Trim() ?? string.Empty;
            bool canBuzz = !string.IsNullOrEmpty(myName) && !_isPreDisplay;

            // If player is disabled (too many mistakes) they cannot buzz
            if (_gameState.Mistakes.GetValueOrDefault(myName, 0) >= _gameState.MaxMistakes)
            {
                canBuzz = false;
            }

            if (_gameState.BuzzOrder.Count > 0 && _gameState.BuzzOrder[0] != myName)
            {
                canBuzz = false;
            }

            if (_gameState.AttemptedThisQuestion.Contains(myName))
            {
                canBuzz = false;
            }

            if (BtnGameBuzz != null)
            {
                BtnGameBuzz.IsEnabled = canBuzz;
                BtnGameBuzz.Opacity = canBuzz ? 1.0 : 0.6;

                try
                {
                    string imgKey;
                    var first = _gameState.BuzzOrder.Count > 0 ? _gameState.BuzzOrder[0] : null;
                    if (!string.IsNullOrEmpty(first) && first != myName)
                    {
                        imgKey = "unavailable";
                    }
                    else if (!string.IsNullOrEmpty(first) && first == myName)
                    {
                        imgKey = "pressed";
                    }
                    else if (!canBuzz)
                    {
                        imgKey = "unavailable";
                    }
                    else
                    {
                        imgKey = "unpressed";
                    }

                    SetBuzzImage(imgKey);
                }
                catch (Exception ex)
                {
                    Logger.LogError(ex);
                }
            }
        }

        private void ShowOverlay(string title, string detail, bool blockUI)
        {
            Dispatcher.Invoke(() =>
            {
                TxtOverlayStatus.Text = title;
                TxtOverlayDetail.Text = detail;
                ResultOverlay.Visibility = Visibility.Visible;
                ResultOverlay.IsHitTestVisible = blockUI;
                AnimateOverlayOpen(ResultOverlayBorder);
            });
        }

        private void HideOverlay()
        {
            Dispatcher.Invoke(() =>
            {
                ResultOverlay.Visibility = Visibility.Collapsed;
                ResultOverlay.IsHitTestVisible = false;
            });
        }

        private void AnimateOverlayOpen(System.Windows.Controls.Border border)
        {
            var storyboard = new System.Windows.Media.Animation.Storyboard();
            
            // Fade in overlay parent
            var fadeIn = new System.Windows.Media.Animation.DoubleAnimation
            {
                From = 0,
                To = 1,
                Duration = TimeSpan.FromMilliseconds(200),
                EasingFunction = new System.Windows.Media.Animation.CubicEase { EasingMode = System.Windows.Media.Animation.EasingMode.EaseOut }
            };
            System.Windows.Media.Animation.Storyboard.SetTarget(fadeIn, border.Parent as System.Windows.UIElement);
            System.Windows.Media.Animation.Storyboard.SetTargetProperty(fadeIn, new PropertyPath("Opacity"));
            storyboard.Children.Add(fadeIn);
            
            // Scale up from center
            var scaleXAnim = new System.Windows.Media.Animation.DoubleAnimation
            {
                From = 0.0,
                To = 1.0,
                Duration = TimeSpan.FromMilliseconds(300),
                EasingFunction = new System.Windows.Media.Animation.BackEase { EasingMode = System.Windows.Media.Animation.EasingMode.EaseOut, Amplitude = 0.4 }
            };
            
            var scaleYAnim = new System.Windows.Media.Animation.DoubleAnimation
            {
                From = 0.0,
                To = 1.0,
                Duration = TimeSpan.FromMilliseconds(300),
                EasingFunction = new System.Windows.Media.Animation.BackEase { EasingMode = System.Windows.Media.Animation.EasingMode.EaseOut, Amplitude = 0.4 }
            };
            
            System.Windows.Media.Animation.Storyboard.SetTarget(scaleXAnim, border);
            System.Windows.Media.Animation.Storyboard.SetTargetProperty(scaleXAnim, new PropertyPath("(UIElement.RenderTransform).(ScaleTransform.ScaleX)"));
            storyboard.Children.Add(scaleXAnim);
            
            System.Windows.Media.Animation.Storyboard.SetTarget(scaleYAnim, border);
            System.Windows.Media.Animation.Storyboard.SetTargetProperty(scaleYAnim, new PropertyPath("(UIElement.RenderTransform).(ScaleTransform.ScaleY)"));
            storyboard.Children.Add(scaleYAnim);
            
            storyboard.Begin();
        }

        private async Task<bool> HandleGameEndAsync(bool ensureAnswerReveal = false)
        {
            if (_gameEnded)
            {
                return true;
            }

            if (!_gameState.EvaluateGameEnd(out var winner, out var allDisqualified))
            {
                return false;
            }

            _revealCts?.Cancel();

            if (ensureAnswerReveal && allDisqualified && _gameState.CurrentQuestion != null)
            {
                await ShowAnswerRevealAsync(_gameState.CurrentQuestion, CancellationToken.None);
            }

            ShowResult(winner ?? "No winner");
            return true;
        }

        private void ResetGameFlow()
        {
            _gameEnded = false;
            _isPreDisplay = true;
            _isAnswerDialogOpen = false;
            _correctOverlayShown = false;
            _gameState.ResetQuestionState();
            _gameState.BuzzOrder.Clear();
            _gameState.AttemptedThisQuestion.Clear();
            _gameState.PausedForBuzz = false;
            _gameState.FastReveal = false;
            _revealCts?.Cancel();
            _revealTask = null;
            _gameState.QueuePosition = -1;

            Dispatcher.Invoke(() =>
            {
                ResultOverlay.Visibility = Visibility.Collapsed;
                ResultOverlay.IsHitTestVisible = false;
                AnswerOverlay.Visibility = Visibility.Collapsed;
                AnswerOverlay.IsHitTestVisible = false;
                TxtAnswerReveal.Visibility = Visibility.Collapsed;
                TxtOverlayStatus.Text = string.Empty;
                TxtOverlayDetail.Text = string.Empty;
                TxtGameStatus.Text = string.Empty;
                if (PbNextTimer != null)
                {
                    PbNextTimer.Visibility = Visibility.Collapsed;
                    PbNextTimer.Value = 0;
                }

                if (BtnGameBuzz != null)
                {
                    BtnGameBuzz.IsEnabled = false;
                    BtnGameBuzz.Opacity = 0.6;
                    SetBuzzImage("unavailable");
                }
            });

            _lastClientQuestionIndex = -1;
            UpdateBuzzButtonState();
        }

        private void ShowResult(string winner)
        {
            if (_gameEnded) return;
            _gameEnded = true;

            // Record win for the current player if they won
            var myName = _profileService.PlayerName;
            if (!string.IsNullOrEmpty(myName) && winner == myName)
            {
                _playerStatsService.OnWin();
            }

            Dispatcher.Invoke(() =>
            {
                // Prepare result data for result panel
                var rankedPlayers = _gameState.Scores
                    .OrderByDescending(kv => kv.Value)
                    .Select((kv, index) => new
                    {
                        Rank = index + 1,
                        Name = kv.Key,
                        Score = kv.Value,
                        ColorBrush = _gameState.EnsurePlayerColor(kv.Key),
                        Mistakes = _gameState.Mistakes.GetValueOrDefault(kv.Key, 0)
                    })
                    .ToList();

                // Set result panel data
                if (rankedPlayers.Count > 0)
                {
                    var first = rankedPlayers[0];
                    TxtWinnerName.Text = first.Name;
                    TxtWinnerScore.Text = $"{first.Score}ポイント";
                }

                ListResultPlayers.ItemsSource = rankedPlayers;
                
                // Show result panel with animation
                ShowPanel(ResultPanel);
            });

            // Don't stop host service - keep it running for next game
        }

        private string GetCurrentQuestionHolder()
        {
            return _gameState.LobbyPlayers.Count > 0 ? _gameState.LobbyPlayers[0] : "Player";
        }

        private void ShowGameStatus(string text)
        {
            Dispatcher.Invoke(() =>
            {
                if (string.IsNullOrWhiteSpace(text))
                {
                    TxtGameStatus.Visibility = Visibility.Collapsed;
                    TxtGameStatus.Text = string.Empty;
                }
                else
                {
                    TxtGameStatus.Visibility = Visibility.Visible;
                    TxtGameStatus.Text = text;
                }
            });
        }

        private void ShowTemporaryGameStatus(string text, int milliseconds = 3000)
        {
            Dispatcher.Invoke(() =>
            {
                TxtGameStatus.Visibility = Visibility.Visible;
                TxtGameStatus.Text = text;
            });

            var captured = text;
            _ = Task.Run(async () =>
            {
                await Task.Delay(milliseconds);
                Dispatcher.Invoke(() =>
                {
                    if (TxtGameStatus.Text == captured)
                    {
                        TxtGameStatus.Text = string.Empty;
                        TxtGameStatus.Visibility = Visibility.Collapsed;
                    }
                });
            });
        }

        private void Window_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            // Check for both half-width and full-width space
            bool isSpacePressed = e.Key == Key.Space;
            
            // Also check for full-width space via text input (IME)
            if (!isSpacePressed && e.Key == Key.ImeProcessed)
            {
                // Will be handled by PreviewTextInput for full-width space
                return;
            }
            
            if (isSpacePressed && GamePanel.Visibility == Visibility.Visible)
            {
                if (_isPreDisplay)
                {
                    e.Handled = true;
                    return;
                }

                var now = DateTime.UtcNow;
                if (now - _lastSpaceTime < _spaceCooldown)
                {
                    e.Handled = true;
                    return;
                }

                // Prevent disabled players from using space to buzz
                var myName = _profileService.PlayerName ?? TxtJoinPlayerName?.Text?.Trim() ?? string.Empty;
                bool canBuzz = !string.IsNullOrEmpty(myName) && !_isPreDisplay;

                if (_gameState.Mistakes.GetValueOrDefault(myName, 0) >= _gameState.MaxMistakes)
                {
                    canBuzz = false;
                }

                if (_gameState.BuzzOrder.Count > 0 && _gameState.BuzzOrder[0] != myName)
                {
                    canBuzz = false;
                }

                if (_gameState.AttemptedThisQuestion.Contains(myName))
                {
                    canBuzz = false;
                }

                if (!canBuzz)
                {
                    e.Handled = true;
                    // Update UI to reflect that buzz isn't allowed
                    Dispatcher.Invoke(() =>
                    {
                        TxtGameStatus.Text = "Cannot buzz";
                        UpdateBuzzButtonState();
                    });
                    return;
                }

                _lastSpaceTime = now;
                e.Handled = true;
                _soundService.PlayBuzz();
                _ = HandleBuzzAsync();
            }

            // If ESC pressed and no answer overlay open, show appropriate confirm overlay
            if (e.Key == Key.Escape)
            {
                if (_isAnswerDialogOpen) return; // let overlay handler manage it

                e.Handled = true;

                if (GamePanel.Visibility == Visibility.Visible)
                {
                    // During game, show leave confirmation (not app exit)
                    if (LeaveConfirmOverlay.Visibility == Visibility.Visible)
                    {
                        LeaveConfirmOverlay.Visibility = Visibility.Collapsed;
                        LeaveConfirmOverlay.IsHitTestVisible = false;
                    }
                    else
                    {
                        LeaveConfirmOverlay.Visibility = Visibility.Visible;
                        LeaveConfirmOverlay.IsHitTestVisible = true;
                    }
                }
                else
                {
                    // Outside game, show app exit confirm
                    if (ExitConfirmOverlay.Visibility == Visibility.Visible)
                    {
                        ExitConfirmOverlay.Visibility = Visibility.Collapsed;
                        ExitConfirmOverlay.IsHitTestVisible = false;
                    }
                    else
                    {
                        ExitConfirmOverlay.Visibility = Visibility.Visible;
                        ExitConfirmOverlay.IsHitTestVisible = true;
                    }
                }
            }
        }



        private void BtnEndGame_Click(object sender, RoutedEventArgs e)
        {
            if (_gameState.Scores.Count == 0)
            {
                ShowPanel(ResultPanel);
                TxtWinnerName.Text = "No players";
                TxtWinnerScore.Text = "";
                return;
            }

            var winner = _gameState.GetWinner() ?? "No winner";
            ShowResult(winner);
        }

        private void StopGameFlow()
        {
            Logger.LogInfo("?? Stopping game flow (canceling tasks, stopping sounds)");
            
            // Cancel reveal loop
            _revealCts?.Cancel();
            _revealTask = null;
            
            // Cancel answer overlay
            _answerOverlayCts?.Cancel();
            _answerOverlayTcs?.TrySetCanceled();
            
            // Stop all sounds
            _soundService.StopAll();
            
            // Reset game state flags
            _gameEnded = true;
            _isAnswerDialogOpen = false;
            _isPreDisplay = false;
            _correctOverlayShown = false;
            Interlocked.Exchange(ref _isAdvancing, 0);
            
            // Hide overlays
            Dispatcher.Invoke(() =>
            {
                AnswerOverlay.Visibility = Visibility.Collapsed;
                AnswerOverlay.IsHitTestVisible = false;
                ResultOverlay.Visibility = Visibility.Collapsed;
                ResultOverlay.IsHitTestVisible = false;
                TxtAnsweringBadge.Visibility = Visibility.Collapsed;
                TxtAnswerReveal.Visibility = Visibility.Collapsed;
                PbNextTimer.Visibility = Visibility.Collapsed;
                TxtGameStatus.Visibility = Visibility.Collapsed;
            });
            
            Logger.LogInfo("? Game flow stopped");
        }
    }
}


