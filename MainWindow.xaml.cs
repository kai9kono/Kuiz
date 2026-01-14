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

        // Public accessor for other windows
        public static string DefaultPostgresConnection => QuestionService.DefaultConnectionString;

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
            _profileService.Load();

            if (!string.IsNullOrEmpty(_profileService.PlayerName))
            {
                TxtJoinPlayerName.Text = _profileService.PlayerName;
            }
            
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
    }
}