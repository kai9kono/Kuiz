using System.Windows;
using System.Threading.Tasks;
using System.Windows.Media.Animation;
using System.Windows.Media.Effects;
using Kuiz.Services;

namespace Kuiz
{
    /// <summary>
    /// ナビゲーション関連のUI処理
    /// </summary>
    public partial class MainWindow
    {
        private async void ShowPanel(UIElement panel)
        {
            Logger.LogInfo($"ShowPanel called for panel: {panel?.GetType().Name ?? "null"}");
            
            // Animate out current panel if needed
            var currentPanel = GetCurrentVisiblePanel();
            Logger.LogInfo($"Current visible panel: {currentPanel?.GetType().Name ?? "null"}");
            
            if (currentPanel != null && currentPanel != panel)
            {
                await AnimatePanelTransition(currentPanel, panel);
            }
            else
            {
                // Just show without animation
                HideAllPanels();
                panel.Visibility = Visibility.Visible;
                Logger.LogInfo($"Panel {panel?.GetType().Name} set to Visible (no animation)");
            }
        }
        
        private UIElement? GetCurrentVisiblePanel()
        {
            if (TitlePanel.Visibility == Visibility.Visible) return TitlePanel;
            if (HostPanel.Visibility == Visibility.Visible) return HostPanel;
            if (JoinPanel.Visibility == Visibility.Visible) return JoinPanel;
            if (GamePanel.Visibility == Visibility.Visible) return GamePanel;
            if (ResultPanel.Visibility == Visibility.Visible) return ResultPanel;
            if (QuestionManagerPanel.Visibility == Visibility.Visible) return QuestionManagerPanel;
            if (CreateQuestionPanel.Visibility == Visibility.Visible) return CreateQuestionPanel;
            if (QuestionHistoryPanel.Visibility == Visibility.Visible) return QuestionHistoryPanel;
            return null;
        }
        
        private void HideAllPanels()
        {
            TitlePanel.Visibility = Visibility.Collapsed;
            HostPanel.Visibility = Visibility.Collapsed;
            JoinPanel.Visibility = Visibility.Collapsed;
            GamePanel.Visibility = Visibility.Collapsed;
            ResultPanel.Visibility = Visibility.Collapsed;
            QuestionManagerPanel.Visibility = Visibility.Collapsed;
            CreateQuestionPanel.Visibility = Visibility.Collapsed;
            QuestionHistoryPanel.Visibility = Visibility.Collapsed;
        }
        
        private async Task AnimatePanelTransition(UIElement fromPanel, UIElement toPanel)
        {
            // Determine animation direction
            // Down: Title -> Host/Join/QuestionManager, QuestionManager -> CreateQuestion/QuestionHistory
            // Up: Host/Join/QuestionManager -> Title, CreateQuestion/QuestionHistory -> QuestionManager
            
            bool shouldAnimateDown = 
                (fromPanel == TitlePanel && (toPanel == HostPanel || toPanel == JoinPanel || toPanel == QuestionManagerPanel)) ||
                (fromPanel == QuestionManagerPanel && (toPanel == CreateQuestionPanel || toPanel == QuestionHistoryPanel));
            
            bool shouldAnimateUp = 
                ((fromPanel == HostPanel || fromPanel == JoinPanel || fromPanel == QuestionManagerPanel) && toPanel == TitlePanel) ||
                ((fromPanel == CreateQuestionPanel || fromPanel == QuestionHistoryPanel) && toPanel == QuestionManagerPanel) ||
                (fromPanel == ResultPanel && (toPanel == HostPanel || toPanel == TitlePanel));
            
            bool shouldAnimateToGame = (fromPanel == HostPanel || fromPanel == JoinPanel) && toPanel == GamePanel;
            
            bool shouldAnimateToResult = fromPanel == GamePanel && toPanel == ResultPanel;
            
            if (!shouldAnimateDown && !shouldAnimateUp && !shouldAnimateToGame && !shouldAnimateToResult)
            {
                HideAllPanels();
                toPanel.Visibility = Visibility.Visible;
                return;
            }
            
            // Play swipe sound
            _soundService.PlaySwipe();
            
            // Determine direction based on transition type
            bool animateDown = shouldAnimateDown || shouldAnimateToGame;
            
            // Prepare both panels
            fromPanel.Visibility = Visibility.Visible;
            toPanel.Visibility = Visibility.Visible;
            
            // Set up transforms
            var fromTransform = new System.Windows.Media.TranslateTransform(0, 0);
            var toTransform = new System.Windows.Media.TranslateTransform(0, animateDown ? 1080 : -1080);
            fromPanel.RenderTransform = fromTransform;
            toPanel.RenderTransform = toTransform;
            
            // Add motion blur effects
            var fromBlur = new BlurEffect { Radius = 0 };
            var toBlur = new BlurEffect { Radius = 0 };
            fromPanel.Effect = fromBlur;
            toPanel.Effect = toBlur;
            
            // Animation settings
            int duration = 450;
            
            // FROM panel: slides up/down and out
            var fromSlide = new DoubleAnimation
            {
                From = 0,
                To = animateDown ? -1080 : 1080,
                Duration = TimeSpan.FromMilliseconds(duration),
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseInOut }
            };
            
            // TO panel: slides up/down into view
            var toSlide = new DoubleAnimation
            {
                From = animateDown ? 1080 : -1080,
                To = 0,
                Duration = TimeSpan.FromMilliseconds(duration),
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseInOut }
            };
            
            // Blur animations for FROM panel
            var fromBlurIn = new DoubleAnimation
            {
                From = 0,
                To = 25,
                Duration = TimeSpan.FromMilliseconds(duration / 2),
                EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseIn }
            };
            
            var fromBlurOut = new DoubleAnimation
            {
                From = 25,
                To = 0,
                Duration = TimeSpan.FromMilliseconds(duration / 2),
                BeginTime = TimeSpan.FromMilliseconds(duration / 2),
                EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
            };
            
            // Blur animations for TO panel
            var toBlurIn = new DoubleAnimation
            {
                From = 0,
                To = 25,
                Duration = TimeSpan.FromMilliseconds(duration / 2),
                EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseIn }
            };
            
            var toBlurOut = new DoubleAnimation
            {
                From = 25,
                To = 0,
                Duration = TimeSpan.FromMilliseconds(duration / 2),
                BeginTime = TimeSpan.FromMilliseconds(duration / 2),
                EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
            };
            
            // Start all animations
            fromTransform.BeginAnimation(System.Windows.Media.TranslateTransform.YProperty, fromSlide);
            toTransform.BeginAnimation(System.Windows.Media.TranslateTransform.YProperty, toSlide);
            fromBlur.BeginAnimation(BlurEffect.RadiusProperty, fromBlurIn);
            fromBlur.BeginAnimation(BlurEffect.RadiusProperty, fromBlurOut);
            toBlur.BeginAnimation(BlurEffect.RadiusProperty, toBlurIn);
            toBlur.BeginAnimation(BlurEffect.RadiusProperty, toBlurOut);
            
            await Task.Delay(duration);
            
            // Clean up
            fromPanel.Visibility = Visibility.Collapsed;
            fromPanel.Effect = null;
            fromPanel.RenderTransform = null;
            toPanel.Effect = null;
            toPanel.RenderTransform = null;
        }

        private void BtnExitGame_Click(object sender, RoutedEventArgs e)
        {
            // Show confirmation overlay instead of exiting immediately
            ExitConfirmOverlay.Visibility = Visibility.Visible;
            ExitConfirmOverlay.IsHitTestVisible = true;
            AnimateConfirmOverlayOpen(ExitConfirmBorder);
        }

        private void BtnBackToTitle_Click(object sender, RoutedEventArgs e)
        {
            // Stop all sounds
            _soundService.StopAll();
            
            var myName = _profileService.PlayerName ?? TxtJoinPlayerName.Text.Trim();
            
            // Remove player from lobby
            if (!string.IsNullOrEmpty(myName))
            {
                _gameState.LobbyPlayers.Remove(myName);
                _gameState.Scores.Remove(myName);
                _gameState.Mistakes.Remove(myName);
            }
            
            // Always stop host service when leaving
            if (_isHost)
            {
                _ = _hostService.StopAsync();
                _isHost = false;
                _gameState.LobbyPlayers.Clear();
                
                // Reset lobby code display
                Dispatcher.Invoke(() =>
                {
                    TxtLobbyCode.Text = "------";
                });
            }
            
            ShowPanel(TitlePanel);
        }

        private void BtnBackToLobby_Click(object sender, RoutedEventArgs e)
        {
            // Stop all sounds
            _soundService.StopAll();
            
            // Return to appropriate lobby
            if (_isHost)
            {
                ShowPanel(HostPanel);
                UpdateLobbyUi();
            }
            else
            {
                // Client returns to title (can't return to host panel)
                ShowPanel(TitlePanel);
            }
        }

        private void BtnLeaveGame_Click(object sender, RoutedEventArgs e)
        {
            ShowLeaveConfirmOverlay();
        }

        private void ShowLeaveConfirmOverlay()
        {
            LeaveConfirmOverlay.Visibility = Visibility.Visible;
            LeaveConfirmOverlay.IsHitTestVisible = true;
            AnimateConfirmOverlayOpen(LeaveConfirmBorder);
        }
        
        private async void BtnLeaveConfirmYes_Click(object sender, RoutedEventArgs e)
        {
            LeaveConfirmOverlay.Visibility = Visibility.Collapsed;
            LeaveConfirmOverlay.IsHitTestVisible = false;
            
            // Stop all sounds immediately
            _soundService.StopAll();
            
            // Cancel ongoing reveal task
            _revealCts?.Cancel();
            _revealCts?.Dispose();
            _revealCts = null;
            
            // Stop the poll state loop for clients
            _pollStateCts?.Cancel();
            _pollStateCts?.Dispose();
            _pollStateCts = null;
            
            var myName = _profileService.PlayerName ?? TxtJoinPlayerName.Text.Trim();
            
            // Remove player from lobby
            if (!string.IsNullOrEmpty(myName))
            {
                _gameState.LobbyPlayers.Remove(myName);
                _gameState.Scores.Remove(myName);
                _gameState.Mistakes.Remove(myName);
            }
            
            // Always stop host service when leaving game
            if (_isHost)
            {
                await _hostService.StopAsync();
                _isHost = false;
                _gameState.LobbyPlayers.Clear();
                
                // Reset lobby code display
                Dispatcher.Invoke(() =>
                {
                    TxtLobbyCode.Text = "------";
                });
                
                ShowPanel(TitlePanel);
            }
            else
            {
                // Client leaves - just go to title
                ShowPanel(TitlePanel);
            }
        }

        private async Task LeaveToTitleAsync()
        {
            HideAllOverlays();

            // Stop all sounds immediately
            _soundService.StopAll();

            // Cancel ongoing reveal task
            _revealCts?.Cancel();
            _revealCts?.Dispose();
            _revealCts = null;
            
            // Stop the poll state loop for clients
            _pollStateCts?.Cancel();
            _pollStateCts?.Dispose();
            _pollStateCts = null;

            // Stop host service if running (with timeout)
            try
            {
                if (_hostService.IsRunning)
                {
                    var stopTask = _hostService.StopAsync();
                    var completed = await Task.WhenAny(stopTask, Task.Delay(2000));
                    if (completed == stopTask)
                    {
                        await stopTask; // observe exceptions
                    }
                }
            }
            catch { }

            // Disconnect HTTP client (close any pending connections)
            try { _httpClient.CancelPendingRequests(); } catch { }

            _isHost = false;
            _gameState.Reset();
            ShowPanel(TitlePanel);
        }

        private void BtnLeaveConfirmNo_Click(object sender, RoutedEventArgs e)
        {
            LeaveConfirmOverlay.Visibility = Visibility.Collapsed;
            LeaveConfirmOverlay.IsHitTestVisible = false;
        }

        private void HideAllOverlays()
        {
            LeaveConfirmOverlay.Visibility = Visibility.Collapsed;
            LeaveConfirmOverlay.IsHitTestVisible = false;
            ResultOverlay.Visibility = Visibility.Collapsed;
            ResultOverlay.IsHitTestVisible = false;
            AnswerOverlay.Visibility = Visibility.Collapsed;
            AnswerOverlay.IsHitTestVisible = false;
            StartGameConfirm.Visibility = Visibility.Collapsed;
            StartGameConfirm.IsHitTestVisible = false;
            StartGameCountdown.Visibility = Visibility.Collapsed;
            StartGameCountdown.IsHitTestVisible = false;
            ProfileOverlay.Visibility = Visibility.Collapsed;
            ProfileOverlay.IsHitTestVisible = false;
            ExitConfirmOverlay.Visibility = Visibility.Collapsed;
            ExitConfirmOverlay.IsHitTestVisible = false;
            LoadingOverlay.Visibility = Visibility.Collapsed;
            LoadingOverlay.IsHitTestVisible = false;
        }

        // Unused overlay button handlers (kept for XAML compatibility)
        private void BtnOverlayOk_Click(object sender, RoutedEventArgs e) { }
        private void BtnOverlayCancel_Click(object sender, RoutedEventArgs e) { }

        // Window closing handler to show confirmation overlay
        private void Window_Closing(object? sender, System.ComponentModel.CancelEventArgs e)
        {
            // If exit overlay already visible, allow close (user clicked Yes)
            if (ExitConfirmOverlay.Visibility == Visibility.Visible)
            {
                return;
            }

            // If other modal overlays are visible, cancel close and just hide/close overlays
            if (AnswerOverlay.Visibility == Visibility.Visible || StartGameConfirm.Visibility == Visibility.Visible || StartGameCountdown.Visibility == Visibility.Visible || LeaveConfirmOverlay.Visibility == Visibility.Visible || ProfileOverlay.Visibility == Visibility.Visible)
            {
                // prevent closing when overlays are active
                e.Cancel = true;
                return;
            }

            // Cancel closing and show confirmation overlay
            e.Cancel = true;
            ExitConfirmOverlay.Visibility = Visibility.Visible;
            ExitConfirmOverlay.IsHitTestVisible = true;
            AnimateConfirmOverlayOpen(ExitConfirmBorder);
        }

        private void BtnExitConfirmYes_Click(object sender, RoutedEventArgs e)
        {
            // Allow close and shutdown
            HideAllOverlays();
            Application.Current.Shutdown();
        }

        private void BtnExitConfirmNo_Click(object sender, RoutedEventArgs e)
        {
            ExitConfirmOverlay.Visibility = Visibility.Collapsed;
            ExitConfirmOverlay.IsHitTestVisible = false;
        }

        private async void BtnLeaveLobbyConfirmYes_Click(object sender, RoutedEventArgs e)
        {
            Logger.LogInfo("BtnLeaveLobbyConfirmYes_Click called");
            
            // Close confirmation overlay
            LeaveLobbyConfirmOverlay.Visibility = Visibility.Collapsed;
            LeaveLobbyConfirmOverlay.IsHitTestVisible = false;

            // Stop all sounds immediately
            _soundService.StopAll();
            
            // Cancel ongoing reveal task
            _revealCts?.Cancel();
            _revealCts?.Dispose();
            _revealCts = null;
            
            // Stop the poll state loop for clients
            _pollStateCts?.Cancel();
            _pollStateCts?.Dispose();
            _pollStateCts = null;

            // Show loading overlay
            TxtLoadingMessage.Text = "ロビーから退出中...";
            LoadingOverlay.Visibility = Visibility.Visible;
            LoadingOverlay.IsHitTestVisible = true;
            
            Logger.LogInfo("Loading overlay shown");

            // Wait a bit for UI to update
            await Task.Delay(100);

            var myName = _profileService.PlayerName ?? "Host";
            Logger.LogInfo($"Player leaving: {myName}, _isHost: {_isHost}");
            
            // Remove player from lobby
            if (!string.IsNullOrEmpty(myName))
            {
                _gameState.LobbyPlayers.Remove(myName);
                _gameState.Scores.Remove(myName);
                _gameState.Mistakes.Remove(myName);
                Logger.LogInfo($"Removed {myName} from lobby");
            }
            
            // Always stop host service when leaving lobby
            if (_isHost)
            {
                Logger.LogInfo("Stopping host service...");
                TxtLoadingMessage.Text = "ホストサービス停止中...";
                await Task.Delay(50); // Allow UI update
                
                try
                {
                    await _hostService.StopAsync();
                    Logger.LogInfo("Host service stopped successfully");
                }
                catch (Exception ex)
                {
                    Logger.LogError(ex);
                }
                
                _isHost = false;
                _gameState.LobbyPlayers.Clear();
                
                // Reset lobby code display on UI thread
                TxtLobbyCode.Text = "------";
                Logger.LogInfo("Reset lobby code display");
            }
            
            TxtLoadingMessage.Text = "完了";
            await Task.Delay(500); // Show completion message
            
            Logger.LogInfo("Hiding loading overlay and transitioning to TitlePanel...");
            
            // Hide loading overlay
            LoadingOverlay.Visibility = Visibility.Collapsed;
            LoadingOverlay.IsHitTestVisible = false;
            
            // Small delay before transition
            await Task.Delay(100);
            
            // Use animated transition to title panel
            ShowPanel(TitlePanel);
            
            Logger.LogInfo("TitlePanel now visible");
        }

        private void BtnLeaveLobbyConfirmNo_Click(object sender, RoutedEventArgs e)
        {
            LeaveLobbyConfirmOverlay.Visibility = Visibility.Collapsed;
            LeaveLobbyConfirmOverlay.IsHitTestVisible = false;
        }

        private void BtnInfo_Click(object sender, RoutedEventArgs e)
        {
            InfoOverlay.Visibility = Visibility.Visible;
            InfoOverlay.IsHitTestVisible = true;
            AnimateConfirmOverlayOpen(InfoBorder);
        }

        private void BtnCloseInfo_Click(object sender, RoutedEventArgs e)
        {
            InfoOverlay.Visibility = Visibility.Collapsed;
            InfoOverlay.IsHitTestVisible = false;
        }
    }
}
