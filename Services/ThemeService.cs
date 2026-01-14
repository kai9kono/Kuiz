using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Media;

namespace Kuiz.Services
{
    /// <summary>
    /// テーマカラーを管理するサービス
    /// </summary>
    public class ThemeService : INotifyPropertyChanged
    {
        public static ThemeService Instance { get; } = new ThemeService();

        private SolidColorBrush _backgroundColor;
        private SolidColorBrush _foregroundColor;
        private SolidColorBrush _cardBackgroundColor;
        private SolidColorBrush _secondaryTextColor;
        private SolidColorBrush _borderColor;
        private SolidColorBrush _accentColor;
        private SolidColorBrush _scoreboardBackgroundColor;
        private SolidColorBrush _overlayBackgroundColor;
        private SolidColorBrush _dialogBackgroundColor;
        private SolidColorBrush _buttonHoverColor;
        private SolidColorBrush _buttonPressedColor;
        private SolidColorBrush _progressBarBackgroundColor;
        private SolidColorBrush _progressBarForegroundColor;
        private SolidColorBrush _confirmButtonBackgroundColor;
        private SolidColorBrush _confirmButtonForegroundColor;

        // Theme colors - dynamically updated with property change notification
        public SolidColorBrush BackgroundColor
        {
            get => _backgroundColor;
            private set { _backgroundColor = value; OnPropertyChanged(); }
        }

        public SolidColorBrush ForegroundColor
        {
            get => _foregroundColor;
            private set { _foregroundColor = value; OnPropertyChanged(); }
        }

        public SolidColorBrush CardBackgroundColor
        {
            get => _cardBackgroundColor;
            private set { _cardBackgroundColor = value; OnPropertyChanged(); }
        }

        public SolidColorBrush SecondaryTextColor
        {
            get => _secondaryTextColor;
            private set { _secondaryTextColor = value; OnPropertyChanged(); }
        }

        public SolidColorBrush BorderColor
        {
            get => _borderColor;
            private set { _borderColor = value; OnPropertyChanged(); }
        }

        public SolidColorBrush AccentColor
        {
            get => _accentColor;
            private set { _accentColor = value; OnPropertyChanged(); }
        }

        public SolidColorBrush ScoreboardBackgroundColor
        {
            get => _scoreboardBackgroundColor;
            private set { _scoreboardBackgroundColor = value; OnPropertyChanged(); }
        }

        public SolidColorBrush OverlayBackgroundColor
        {
            get => _overlayBackgroundColor;
            private set { _overlayBackgroundColor = value; OnPropertyChanged(); }
        }

        public SolidColorBrush DialogBackgroundColor
        {
            get => _dialogBackgroundColor;
            private set { _dialogBackgroundColor = value; OnPropertyChanged(); }
        }

        public SolidColorBrush ButtonHoverColor
        {
            get => _buttonHoverColor;
            private set { _buttonHoverColor = value; OnPropertyChanged(); }
        }

        public SolidColorBrush ButtonPressedColor
        {
            get => _buttonPressedColor;
            private set { _buttonPressedColor = value; OnPropertyChanged(); }
        }

        public SolidColorBrush ProgressBarBackgroundColor
        {
            get => _progressBarBackgroundColor;
            private set { _progressBarBackgroundColor = value; OnPropertyChanged(); }
        }

        public SolidColorBrush ProgressBarForegroundColor
        {
            get => _progressBarForegroundColor;
            private set { _progressBarForegroundColor = value; OnPropertyChanged(); }
        }

        public SolidColorBrush ConfirmButtonBackgroundColor
        {
            get => _confirmButtonBackgroundColor;
            private set { _confirmButtonBackgroundColor = value; OnPropertyChanged(); }
        }

        public SolidColorBrush ConfirmButtonForegroundColor
        {
            get => _confirmButtonForegroundColor;
            private set { _confirmButtonForegroundColor = value; OnPropertyChanged(); }
        }

        public bool IsDarkMode { get; private set; }

        public event PropertyChangedEventHandler? PropertyChanged;
        public event EventHandler? ThemeChanged;

        private ThemeService()
        {
            ApplyLightTheme();
        }

        public void SetTheme(bool isDarkMode)
        {
            IsDarkMode = isDarkMode;
            
            if (isDarkMode)
            {
                ApplyDarkTheme();
            }
            else
            {
                ApplyLightTheme();
            }

            ThemeChanged?.Invoke(this, EventArgs.Empty);
        }

        private void ApplyLightTheme()
        {
            BackgroundColor = new SolidColorBrush(Colors.White);
            ForegroundColor = new SolidColorBrush(Colors.Black);
            CardBackgroundColor = new SolidColorBrush(Color.FromRgb(245, 245, 245));
            SecondaryTextColor = new SolidColorBrush(Color.FromRgb(102, 102, 102));
            BorderColor = new SolidColorBrush(Color.FromRgb(221, 221, 221));
            AccentColor = new SolidColorBrush(Color.FromRgb(33, 150, 243));
            ScoreboardBackgroundColor = new SolidColorBrush(Color.FromRgb(245, 245, 245));
            OverlayBackgroundColor = new SolidColorBrush(Color.FromArgb(128, 0, 0, 0));
            DialogBackgroundColor = new SolidColorBrush(Colors.White);
            ButtonHoverColor = new SolidColorBrush(Color.FromRgb(138, 138, 138));
            ButtonPressedColor = new SolidColorBrush(Color.FromRgb(224, 224, 224));
            ProgressBarBackgroundColor = new SolidColorBrush(Color.FromRgb(240, 240, 240));
            ProgressBarForegroundColor = new SolidColorBrush(Color.FromRgb(190, 190, 190));
            ConfirmButtonBackgroundColor = new SolidColorBrush(Color.FromRgb(66, 66, 66));
            ConfirmButtonForegroundColor = new SolidColorBrush(Colors.White);
        }

        private void ApplyDarkTheme()
        {
            BackgroundColor = new SolidColorBrush(Color.FromRgb(18, 18, 18));
            ForegroundColor = new SolidColorBrush(Color.FromRgb(230, 230, 230));
            CardBackgroundColor = new SolidColorBrush(Color.FromRgb(33, 33, 33));
            SecondaryTextColor = new SolidColorBrush(Color.FromRgb(158, 158, 158));
            BorderColor = new SolidColorBrush(Color.FromRgb(120, 120, 120));
            AccentColor = new SolidColorBrush(Color.FromRgb(100, 181, 246));
            ScoreboardBackgroundColor = new SolidColorBrush(Color.FromRgb(33, 33, 33));
            OverlayBackgroundColor = new SolidColorBrush(Color.FromArgb(180, 0, 0, 0));
            DialogBackgroundColor = new SolidColorBrush(Color.FromRgb(40, 40, 40));
            ButtonHoverColor = new SolidColorBrush(Color.FromRgb(80, 80, 80));
            ButtonPressedColor = new SolidColorBrush(Color.FromRgb(60, 60, 60));
            ProgressBarBackgroundColor = new SolidColorBrush(Color.FromRgb(40, 40, 40));
            ProgressBarForegroundColor = new SolidColorBrush(Color.FromRgb(100, 181, 246));
            ConfirmButtonBackgroundColor = new SolidColorBrush(Color.FromRgb(70, 70, 70));
            ConfirmButtonForegroundColor = new SolidColorBrush(Color.FromRgb(230, 230, 230));
        }

        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
