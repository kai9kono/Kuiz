using System;
using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Resources;

namespace Kuiz.Services
{
    /// <summary>
    /// UIå¯â âπÇÃçƒê∂ÇíSìñ
    /// </summary>
    public class SoundService
    {
        private static SoundService? _instance;
        public static SoundService Instance => _instance ??= new SoundService();

        private MediaPlayer _hoverPlayer;
        private MediaPlayer _pressPlayer;
        private MediaPlayer _swipePlayer;
        private MediaPlayer _buzzPlayer;
        private MediaPlayer _correctPlayer;
        private MediaPlayer _incorrectPlayer;
        private MediaPlayer _questionPlayer;

        // âπó ê›íË (0.0 ~ 1.0)
        private const double HoverVolume = 0.3;
        private const double PressVolume = 0.4;
        private const double SwipeVolume = 0.5;
        private const double BuzzVolume = 0.6;
        private const double CorrectVolume = 0.9;
        private const double IncorrectVolume = 0.6;
        private const double QuestionVolume = 0.7;

        private bool _isInitialized;

        private string? _hoverPath;
        private string? _pressPath;
        private string? _swipePath;
        private string? _buzzPath;
        private string? _correctPath;
        private string? _incorrectPath;
        private string? _questionPath;

        public double MasterVolume { get; private set; } = 1.0;
        public bool IsMuted { get; private set; }

        private SoundService()
        {
            _hoverPlayer = new MediaPlayer();
            _pressPlayer = new MediaPlayer();
            _swipePlayer = new MediaPlayer();
            _buzzPlayer = new MediaPlayer();
            _correctPlayer = new MediaPlayer();
            _incorrectPlayer = new MediaPlayer();
            _questionPlayer = new MediaPlayer();
        }

        public void Initialize()
        {
            if (_isInitialized) return;

            try
            {
                // Extract sound files to temp directory for MediaPlayer compatibility
                var tempDir = Path.Combine(Path.GetTempPath(), "Kuiz_Sounds");
                Directory.CreateDirectory(tempDir);

                _hoverPath = ExtractResource("Resources/sound/ui-button-hover.mp3", tempDir);
                _pressPath = ExtractResource("Resources/sound/ui-button-press.mp3", tempDir);
                _swipePath = ExtractResource("Resources/sound/swipe.mp3", tempDir);
                _buzzPath = ExtractResource("Resources/sound/play-button-press.mp3", tempDir);
                _correctPath = ExtractResource("Resources/sound/correct.mp3", tempDir);
                _incorrectPath = ExtractResource("Resources/sound/incorrect.mp3", tempDir);
                _questionPath = ExtractResource("Resources/sound/question.mp3", tempDir);

                if (_hoverPath != null)
                {
                    _hoverPlayer.Open(new Uri(_hoverPath));
                }

                if (_pressPath != null)
                {
                    _pressPlayer.Open(new Uri(_pressPath));
                }

                if (_swipePath != null)
                {
                    _swipePlayer.Open(new Uri(_swipePath));
                }

                if (_buzzPath != null)
                {
                    _buzzPlayer.Open(new Uri(_buzzPath));
                }

                if (_correctPath != null)
                {
                    _correctPlayer.Open(new Uri(_correctPath));
                }

                if (_incorrectPath != null)
                {
                    _incorrectPlayer.Open(new Uri(_incorrectPath));
                }

                if (_questionPath != null)
                {
                    _questionPlayer.Open(new Uri(_questionPath));
                }

                UpdateAllVolumes();

                _isInitialized = true;
            }
            catch (Exception ex)
            {
                Logger.LogError(ex);
            }
        }

        private string? ExtractResource(string resourcePath, string targetDir)
        {
            try
            {
                var uri = new Uri($"pack://application:,,,/Kuiz;component/{resourcePath}", UriKind.Absolute);
                StreamResourceInfo? sri = Application.GetResourceStream(uri);

                if (sri == null)
                {
                    Logger.LogError(new FileNotFoundException($"Resource not found: {resourcePath}"));
                    return null;
                }

                var fileName = Path.GetFileName(resourcePath);
                var targetPath = Path.Combine(targetDir, fileName);

                using (var stream = sri.Stream)
                using (var fileStream = new FileStream(targetPath, FileMode.Create, FileAccess.Write))
                {
                    stream.CopyTo(fileStream);
                }

                return targetPath;
            }
            catch (Exception ex)
            {
                Logger.LogError(ex);
                return null;
            }
        }

        public void SetMasterVolume(double value)
        {
            MasterVolume = Math.Clamp(value, 0.0, 1.0);
            UpdateAllVolumes();
        }

        public void ToggleMute()
        {
            IsMuted = !IsMuted;
            UpdateAllVolumes();
        }

        private void UpdateAllVolumes()
        {
            UpdatePlayerVolume(_hoverPlayer, HoverVolume);
            UpdatePlayerVolume(_pressPlayer, PressVolume);
            UpdatePlayerVolume(_swipePlayer, SwipeVolume);
            UpdatePlayerVolume(_buzzPlayer, BuzzVolume);
            UpdatePlayerVolume(_correctPlayer, CorrectVolume);
            UpdatePlayerVolume(_incorrectPlayer, IncorrectVolume);
            UpdatePlayerVolume(_questionPlayer, QuestionVolume);
        }

        private void UpdatePlayerVolume(MediaPlayer player, double baseVolume)
        {
            if (player == null) return;
            player.Volume = IsMuted ? 0 : Math.Clamp(baseVolume, 0.0, 1.0) * MasterVolume;
        }

        private void PlayPlayer(MediaPlayer player)
        {
            if (!_isInitialized || player == null) return;

            try
            {
                player.Stop();
                player.Position = TimeSpan.Zero;
                player.Play();
            }
            catch (Exception ex)
            {
                Logger.LogError(ex);
            }
        }

        public void PlayHover()
        {
            PlayPlayer(_hoverPlayer);
        }

        public void PlayPress()
        {
            PlayPlayer(_pressPlayer);
        }

        public void PlaySwipe()
        {
            PlayPlayer(_swipePlayer);
        }

        public void PlayBuzz()
        {
            PlayPlayer(_buzzPlayer);
        }

        public void PlayCorrect()
        {
            PlayPlayer(_correctPlayer);
        }

        public void PlayIncorrect()
        {
            PlayPlayer(_incorrectPlayer);
        }

        public void PlayQuestion()
        {
            PlayPlayer(_questionPlayer);
        }

        /// <summary>
        /// ñ‚ëËâπê∫Çí‚é~
        /// </summary>
        public void StopQuestion()
        {
            if (!_isInitialized || _questionPlayer == null) return;

            try
            {
                _questionPlayer.Stop();
            }
            catch (Exception ex)
            {
                Logger.LogError(ex);
            }
        }

        /// <summary>
        /// Ç∑Ç◊ÇƒÇÃâπê∫Çí‚é~
        /// </summary>
        public void StopAll()
        {
            if (!_isInitialized) return;

            try
            {
                _hoverPlayer?.Stop();
                _pressPlayer?.Stop();
                _swipePlayer?.Stop();
                _buzzPlayer?.Stop();
                _correctPlayer?.Stop();
                _incorrectPlayer?.Stop();
                _questionPlayer?.Stop();
            }
            catch (Exception ex)
            {
                Logger.LogError(ex);
            }
        }
    }
}
