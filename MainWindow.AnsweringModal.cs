using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

namespace Kuiz;

public partial class MainWindow
{
    private Grid? _answeringModal;
    private TextBlock? _answeringMessage;

    private void UpdateAnsweringModal()
    {
        var player = _gameState.BuzzOrder.FirstOrDefault();
        if (_gameEnded || GamePanel.Visibility != Visibility.Visible ||
            !_gameState.PausedForBuzz || string.IsNullOrEmpty(player) ||
            player == _profileService.PlayerName)
        {
            HideAnsweringModal();
            return;
        }
        if (_answeringModal == null)
        {
            _answeringMessage = new TextBlock
            {
                FontSize = 24, TextWrapping = TextWrapping.Wrap,
                TextAlignment = TextAlignment.Center, Margin = new Thickness(24)
            };
            _answeringMessage.SetBinding(TextBlock.ForegroundProperty, new Binding("ForegroundColor"));
            var card = new Border
            {
                Width = 420, CornerRadius = new CornerRadius(12),
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center, Child = _answeringMessage
            };
            card.SetBinding(Border.BackgroundProperty, new Binding("DialogBackgroundColor"));
            _answeringModal = new Grid { IsHitTestVisible = true };
            _answeringModal.SetBinding(Grid.BackgroundProperty, new Binding("OverlayBackgroundColor"));
            _answeringModal.Children.Add(card);
            Panel.SetZIndex(_answeringModal, 49);
            ((Panel)AnswerOverlay.Parent).Children.Add(_answeringModal);
        }
        _answeringMessage!.Text = $"{player}さんが回答中です\n判定をお待ちください";
        _answeringModal.Visibility = Visibility.Visible;
    }

    private void HideAnsweringModal()
    {
        if (_answeringModal != null) _answeringModal.Visibility = Visibility.Collapsed;
    }
}
