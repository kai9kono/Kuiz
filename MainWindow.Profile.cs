using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using Kuiz.Services;
using MaterialDesignThemes.Wpf;

namespace Kuiz
{
    /// <summary>
    /// 設定関連のUI処理
    /// </summary>
    public partial class MainWindow
    {
        private void BtnEditProfile_Click(object sender, RoutedEventArgs e)
        {
            TxtProfileName.Text = _profileService.PlayerName ?? TxtJoinPlayerName.Text.Trim();
            UpdateDarkModeButton();
            
            ProfileOverlay.Visibility = Visibility.Visible;
            ProfileOverlay.IsHitTestVisible = true;
            
            // Animate dialog appearance
            AnimateProfileDialogOpen();
            
            TxtProfileName.Focus();
        }

        private void BtnSaveProfile_Click(object sender, RoutedEventArgs e)
        {
            var oldName = _profileService.PlayerName;
            var name = TxtProfileName.Text?.Trim();
            
            if (!string.IsNullOrEmpty(name))
            {
                _profileService.Save(name, _themeService.IsDarkMode);
                TxtJoinPlayerName.Text = name;

                // If the name changed, update game state entries so UI reflects immediately
                if (!string.IsNullOrEmpty(oldName) && oldName != name)
                {
                    // Update lobby list
                    var idx = _gameState.LobbyPlayers.IndexOf(oldName);
                    if (idx >= 0)
                    {
                        if (!_gameState.LobbyPlayers.Contains(name))
                        {
                            _gameState.LobbyPlayers[idx] = name;
                        }
                        else
                        {
                            // new name already present -> remove old
                            _gameState.LobbyPlayers.RemoveAt(idx);
                        }
                    }

                    // Transfer/merge scores
                    if (_gameState.Scores.TryGetValue(oldName, out var oldScore))
                    {
                        if (_gameState.Scores.ContainsKey(name)) _gameState.Scores[name] += oldScore;
                        else _gameState.Scores[name] = oldScore;
                        _gameState.Scores.Remove(oldName);
                    }

                    // Transfer/merge mistakes
                    if (_gameState.Mistakes.TryGetValue(oldName, out var oldMistakes))
                    {
                        if (_gameState.Mistakes.ContainsKey(name)) _gameState.Mistakes[name] += oldMistakes;
                        else _gameState.Mistakes[name] = oldMistakes;
                        _gameState.Mistakes.Remove(oldName);
                    }

                    // Transfer color brush
                    if (_gameState.PlayerColorBrushes.TryGetValue(oldName, out var brush))
                    {
                        if (!_gameState.PlayerColorBrushes.ContainsKey(name))
                        {
                            _gameState.PlayerColorBrushes[name] = brush;
                        }
                        _gameState.PlayerColorBrushes.Remove(oldName);
                    }

                    // Replace in buzz order
                    for (int i = 0; i < _gameState.BuzzOrder.Count; i++)
                    {
                        if (_gameState.BuzzOrder[i] == oldName) _gameState.BuzzOrder[i] = name;
                    }

                    // Replace in attempted set
                    if (_gameState.AttemptedThisQuestion.Contains(oldName))
                    {
                        _gameState.AttemptedThisQuestion.Remove(oldName);
                        _gameState.AttemptedThisQuestion.Add(name);
                    }

                    // Refresh UI
                    Dispatcher.Invoke(() =>
                    {
                        UpdateLobbyUi();
                        UpdateGameUi();
                });
            }
        }

        AnimateProfileDialogClose();
    }

    private void BtnCancelProfile_Click(object sender, RoutedEventArgs e)
    {
        // Revert theme to saved state if user cancels
        _themeService.SetTheme(_profileService.IsDarkMode);
        UpdateDarkModeButton();
        
        AnimateProfileDialogClose();
    }
        
        private void BtnToggleDarkMode_Click(object sender, RoutedEventArgs e)
        {
            // Toggle theme with animation
            var newDarkMode = !_themeService.IsDarkMode;
            
            // Animate button rotation
            AnimateThemeButtonRotation();
            
            // Fade out, change theme, fade in
            AnimateThemeTransition(newDarkMode);
        }
        
        private void AnimateThemeButtonRotation()
        {
            // Create rotation animation for the icon
            var rotateTransform = new RotateTransform();
            IconDarkMode.RenderTransform = rotateTransform;
            IconDarkMode.RenderTransformOrigin = new Point(0.5, 0.5);
            
            var rotateAnimation = new DoubleAnimation
            {
                From = 0,
                To = 360,
                Duration = TimeSpan.FromMilliseconds(400),
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseInOut }
            };
            
            rotateTransform.BeginAnimation(RotateTransform.AngleProperty, rotateAnimation);
        }
        
        private async void AnimateThemeTransition(bool newDarkMode)
        {
            // Create fade out animation
            var fadeOut = new DoubleAnimation
            {
                From = 1.0,
                To = 0.0,
                Duration = TimeSpan.FromMilliseconds(200),
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseIn }
            };
            
            // Animate main grid opacity
            var mainGrid = this.Content as Grid;
            if (mainGrid != null)
            {
                mainGrid.BeginAnimation(OpacityProperty, fadeOut);
            }
            
            // Wait for fade out to complete
            await Task.Delay(200);
            
            // Change theme
            _themeService.SetTheme(newDarkMode);
            _profileService.SaveDarkMode(newDarkMode);
            
            // Update button appearance
            UpdateDarkModeButton();
            
            // Create fade in animation
            var fadeIn = new DoubleAnimation
            {
                From = 0.0,
                To = 1.0,
                Duration = TimeSpan.FromMilliseconds(200),
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
            };
            
            // Animate main grid opacity
            if (mainGrid != null)
            {
                mainGrid.BeginAnimation(OpacityProperty, fadeIn);
            }
        }
        
        private void UpdateDarkModeButton()
        {
            if (_themeService.IsDarkMode)
            {
                IconDarkMode.Kind = PackIconKind.WhiteBalanceSunny;
                TxtDarkModeStatus.Text = "ダークモード";
            }
            else
            {
                IconDarkMode.Kind = PackIconKind.WeatherNight;
                TxtDarkModeStatus.Text = "ライトモード";
            }
        }
        
        private void ToggleDarkMode_Checked(object sender, RoutedEventArgs e)
        {
            // No longer used
        }
        
    private void ToggleDarkMode_Unchecked(object sender, RoutedEventArgs e)
    {
        // No longer used
    }
    
    private void AnimateProfileDialogOpen()
        {
            var storyboard = new Storyboard();
            
            // Set initial opacity to 0
            ProfileOverlay.Opacity = 0;
            
            // Fade in overlay
            var fadeIn = new DoubleAnimation
            {
                From = 0,
                To = 1,
                Duration = TimeSpan.FromMilliseconds(250),
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
            };
            Storyboard.SetTarget(fadeIn, ProfileOverlay);
            Storyboard.SetTargetProperty(fadeIn, new PropertyPath("Opacity"));
            storyboard.Children.Add(fadeIn);
            
            // Scale up dialog from center (0.0 to 1.0 for more dramatic effect)
            // RenderTransformOrigin is already set to 0.5,0.5 in XAML, so we don't need to set CenterX/CenterY
            
            var scaleXAnim = new DoubleAnimation
            {
                From = 0.0,
                To = 1.0,
                Duration = TimeSpan.FromMilliseconds(350),
                EasingFunction = new BackEase { EasingMode = EasingMode.EaseOut, Amplitude = 0.4 }
            };
            
            var scaleYAnim = new DoubleAnimation
            {
                From = 0.0,
                To = 1.0,
                Duration = TimeSpan.FromMilliseconds(350),
                EasingFunction = new BackEase { EasingMode = EasingMode.EaseOut, Amplitude = 0.4 }
            };
            
            Storyboard.SetTarget(scaleXAnim, ProfileDialogBorder);
            Storyboard.SetTargetProperty(scaleXAnim, new PropertyPath("(UIElement.RenderTransform).(ScaleTransform.ScaleX)"));
            storyboard.Children.Add(scaleXAnim);
            
            Storyboard.SetTarget(scaleYAnim, ProfileDialogBorder);
            Storyboard.SetTargetProperty(scaleYAnim, new PropertyPath("(UIElement.RenderTransform).(ScaleTransform.ScaleY)"));
            storyboard.Children.Add(scaleYAnim);
            
            storyboard.Begin();
        }
        
        private void AnimateProfileDialogClose()
        {
            var storyboard = new Storyboard();
            
            // Fade out overlay
            var fadeOut = new DoubleAnimation
            {
                From = 1,
                To = 0,
                Duration = TimeSpan.FromMilliseconds(200),
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseIn }
            };
            Storyboard.SetTarget(fadeOut, ProfileOverlay);
            Storyboard.SetTargetProperty(fadeOut, new PropertyPath("Opacity"));
            storyboard.Children.Add(fadeOut);
            
            // Scale down dialog to center (1.0 to 0.0)
            var scaleXAnim = new DoubleAnimation
            {
                From = 1.0,
                To = 0.0,
                Duration = TimeSpan.FromMilliseconds(200),
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseIn }
            };
            
            var scaleYAnim = new DoubleAnimation
            {
                From = 1.0,
                To = 0.0,
                Duration = TimeSpan.FromMilliseconds(200),
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseIn }
            };
            
            Storyboard.SetTarget(scaleXAnim, ProfileDialogBorder);
            Storyboard.SetTargetProperty(scaleXAnim, new PropertyPath("(UIElement.RenderTransform).(ScaleTransform.ScaleX)"));
            storyboard.Children.Add(scaleXAnim);
            
            Storyboard.SetTarget(scaleYAnim, ProfileDialogBorder);
            Storyboard.SetTargetProperty(scaleYAnim, new PropertyPath("(UIElement.RenderTransform).(ScaleTransform.ScaleY)"));
            storyboard.Children.Add(scaleYAnim);
            
            storyboard.Completed += (s, e) =>
            {
                ProfileOverlay.Visibility = Visibility.Collapsed;
                ProfileOverlay.IsHitTestVisible = false;
            };
            
            storyboard.Begin();
        }
        
        private void ApplyThemeToUI()
        {
            // Theme is automatically applied through XAML bindings
            // Just trigger a refresh if needed
            this.UpdateLayout();
        }
        
        private void UpdateTextBlockColors(DependencyObject obj)
        {
            for (int i = 0; i < System.Windows.Media.VisualTreeHelper.GetChildrenCount(obj); i++)
            {
                var child = System.Windows.Media.VisualTreeHelper.GetChild(obj, i);
                
                if (child is System.Windows.Controls.TextBlock textBlock)
                {
                    // Skip if it has a specific foreground set (like colored text)
                    if (textBlock.Foreground == Brushes.Black || 
                        textBlock.ReadLocalValue(System.Windows.Controls.TextBlock.ForegroundProperty) == DependencyProperty.UnsetValue)
                    {
                        textBlock.Foreground = _themeService.ForegroundColor;
                    }
                }
                
                UpdateTextBlockColors(child);
            }
        }
    }
}
