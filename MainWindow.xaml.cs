using System.ComponentModel;
using System.Net.Http;
using System.Windows;
using System.Windows.Input;
using System.Text.RegularExpressions;
using Kuiz.Services;
using MaterialDesignThemes.Wpf;

namespace Kuiz
{
    /// <summary>
    /// MainWindow - メインウィンドウ（partialクラス）
    /// 
    /// 関連ファイル:
    /// - MainWindow.Game.cs      : ゲーム関連UI処理
    /// - MainWindow.Host.cs      : ホスト関連UI処理
    /// - MainWindow.Client.cs    : クライアント関連UI処理
    /// - MainWindow.Navigation.cs: ナビゲーション処理
    /// - MainWindow.Profile.cs   : プロフィール処理
    /// </summary>
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        // Services
        private readonly ProfileService _profileService = new();
        private readonly QuestionService _questionService = new();
        private readonly GameStateService _gameState = new();
        private readonly HostService _hostService = new();
        private readonly HttpClient _httpClient = new();
        private readonly ThemeService _themeService = ThemeService.Instance;
        private readonly SoundService _soundService = SoundService.Instance;
        private readonly PlayerStatsService _playerStatsService = new();

        public event PropertyChangedEventHandler? PropertyChanged;

        public MainWindow()
        {
            InitializeComponent();

            
            // Set ThemeService as DataContext for bindings
            this.DataContext = _themeService;
            
            InitializeServices();
        }

        private void InitializeServices()
        {
            // ログファイルの場所を記録
            Logger.LogInfo("===========================================");
            Logger.LogInfo("🚀 Kuiz Application Starting");
            Logger.LogInfo($"📁 Log file: {Logger.GetLogFilePath()}");
            Logger.LogInfo($"📁 Log directory: {Logger.GetLogDirectory()}");
            Logger.LogInfo("===========================================");
            
            _profileService.Load();

            // プレイヤー名をテキストボックスに設定（デフォルト：ちびすけ明太子）
            TxtJoinPlayerName.Text = _profileService.PlayerName ?? "ちびすけ明太子";
            
            // Apply saved theme using ThemeService

            _themeService.SetTheme(_profileService.IsDarkMode);
            ApplyThemeToUI();

            SetupHostServiceEvents();
            
            // Initialize sound service
            _soundService.Initialize();

            if (SldVolume != null)
            {
                SldVolume.Value = _soundService.MasterVolume;
            }
            UpdateVolumeToggleIcon();

            try
            {
                BtnEditProfile.Visibility = Visibility.Visible;
            }
            catch { }

            // Ensure buzz button image is initialized and background transparent
            try
            {
                BtnGameBuzz.Background = System.Windows.Media.Brushes.Transparent;
                SetBuzzImage("unpressed");
            }
            catch { }
            
            // API接続テストを実行
            _ = TestApiConnectionOnStartup();
        }
        
        // Sound event handlers for buttons
        private void Button_MouseEnter(object sender, MouseEventArgs e)
        {
            _soundService.PlayHover();
        }
        
        private void Button_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            _soundService.PlayPress();
        }
        
        private void AnimateConfirmOverlayOpen(System.Windows.Controls.Border border)
        {
            var storyboard = new System.Windows.Media.Animation.Storyboard();
            
            // Fade in overlay parent
            var fadeIn = new System.Windows.Media.Animation.DoubleAnimation
            {
                From = 0,
                To = 1,
                Duration = System.TimeSpan.FromMilliseconds(200),
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
                Duration = System.TimeSpan.FromMilliseconds(300),
                EasingFunction = new System.Windows.Media.Animation.BackEase { EasingMode = System.Windows.Media.Animation.EasingMode.EaseOut, Amplitude = 0.4 }
            };
            
            var scaleYAnim = new System.Windows.Media.Animation.DoubleAnimation
            {
                From = 0.0,
                To = 1.0,
                Duration = System.TimeSpan.FromMilliseconds(300),
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
        
        private void SldVolume_Loaded(object sender, RoutedEventArgs e)
        {
            SldVolume.Value = _soundService.MasterVolume;
            UpdateVolumeToggleIcon();
        }

        private void SldVolume_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            _soundService.SetMasterVolume(e.NewValue);
        }

        private void BtnToggleMute_Click(object sender, RoutedEventArgs e)
        {
            _soundService.ToggleMute();
            UpdateVolumeToggleIcon();
        }

        private void UpdateVolumeToggleIcon()
        {
            if (IconVolumeToggle == null) return;
            IconVolumeToggle.Kind = _soundService.IsMuted ? PackIconKind.VolumeOff : PackIconKind.VolumeHigh;
        }

        // Numeric input validation for textboxes
        private void NumericTextBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            e.Handled = !IsTextNumeric(e.Text);
        }
        
        private void NumericTextBox_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            // Allow control keys
            if (e.Key == Key.Back || e.Key == Key.Delete || e.Key == Key.Left || e.Key == Key.Right || 
                e.Key == Key.Tab || e.Key == Key.Enter)
            {
                return;
            }
            
            // Block paste
            if ((e.Key == Key.V && (Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control))
            {
                e.Handled = true;
            }
        }
        
        
        private static bool IsTextNumeric(string text)
        {
            return Regex.IsMatch(text, "^[0-9]+$");
        }

        private void ShowServerErrorPopup(string message)
        {
            // 既存のエラーオーバーレイがあれば使用、なければStartGameConfirmを流用
            var errorOverlay = new System.Windows.Controls.Grid
            {
                Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromArgb(200, 0, 0, 0)),
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Stretch,
                Visibility = Visibility.Visible
            };

            var errorBorder = new System.Windows.Controls.Border
            {
                Width = 520,
                Padding = new Thickness(24),
                CornerRadius = new CornerRadius(8),
                Background = _themeService.DialogBackgroundColor,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };

            var stackPanel = new System.Windows.Controls.StackPanel();

            var titleText = new System.Windows.Controls.TextBlock
            {
                Text = "⚠️ 接続エラー",
                FontSize = 22,
                FontWeight = FontWeights.Bold,
                TextAlignment = TextAlignment.Center,
                Foreground = _themeService.ForegroundColor,
                Margin = new Thickness(0, 0, 0, 16)
            };

            var messageText = new System.Windows.Controls.TextBlock
            {
                Text = message,
                FontSize = 16,
                TextAlignment = TextAlignment.Center,
                TextWrapping = TextWrapping.Wrap,
                Foreground = _themeService.SecondaryTextColor,
                Margin = new Thickness(0, 0, 0, 24)
            };

            var okButton = new System.Windows.Controls.Button
            {
                Content = "OK",
                Width = 140,
                Height = 50,
                FontSize = 16,
                HorizontalAlignment = HorizontalAlignment.Center
            };
            okButton.Click += (s, e) =>
            {
                ((System.Windows.Controls.Grid)this.Content).Children.Remove(errorOverlay);
            };

            stackPanel.Children.Add(titleText);
            stackPanel.Children.Add(messageText);
            stackPanel.Children.Add(okButton);

            errorBorder.Child = stackPanel;
            errorOverlay.Children.Add(errorBorder);

            ((System.Windows.Controls.Grid)this.Content).Children.Add(errorOverlay);
            System.Windows.Controls.Panel.SetZIndex(errorOverlay, 100);
        }
        
        private async Task TestApiConnectionOnStartup()
        {
            try
            {
                Logger.LogInfo("🔍 Testing API connection on startup...");
                await _questionService.TestConnectionAsync();
                Logger.LogInfo("✅ API connection test successful");
            }
            catch (Exception ex)
            {
                Logger.LogError(ex);
                Logger.LogError(new Exception($"⚠️ API connection test failed on startup: {ex.Message}"));
                
                // ユーザーにエラーを表示（メインスレッドで）
                Dispatcher.Invoke(() =>
                {
                    MessageBox.Show(
                        $"Railway APIに接続できませんでした。\n\n" +
                        $"エラー: {ex.Message}\n\n" +
                        $"インターネット接続を確認してください。\n" +
                        $"ログファイル: {Logger.GetLogFilePath()}",
                        "接続エラー",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning
                    );
                });
            }
        }
    }
}
