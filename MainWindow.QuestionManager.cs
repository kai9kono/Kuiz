using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;
using Kuiz.Models;
using Kuiz.Services;

namespace Kuiz
{
    /// <summary>
    /// 問題マネージャー関連のUI処理
    /// </summary>
    public partial class MainWindow
    {
        private readonly QuestionHistoryService _historyService = new();
        private readonly ObservableCollection<QuestionImportDto> _createdQuestions = new();
        private QuestionImportDto? _editingQuestion;

        // Navigation
        private void BtnTitleQuestionManager_Click(object sender, RoutedEventArgs e)
        {
            ShowPanel(QuestionManagerPanel);
        }

        private void BtnQMBack_Click(object sender, RoutedEventArgs e)
        {
            ShowPanel(TitlePanel);
        }

        private void BtnGoToCreateQuestion_Click(object sender, RoutedEventArgs e)
        {
            ShowPanel(CreateQuestionPanel);
            UpdateQuestionListCount();
        }

        private async void BtnGoToQuestionHistory_Click(object sender, RoutedEventArgs e)
        {
            ShowPanel(QuestionHistoryPanel);
            await LoadQuestionHistoryAsync();
        }

        private void BtnCQBack_Click(object sender, RoutedEventArgs e)
        {
            ShowPanel(QuestionManagerPanel);
        }

        private void BtnQHBack_Click(object sender, RoutedEventArgs e)
        {
            ShowPanel(QuestionManagerPanel);
        }

        // Create Question
        private void BtnAddToList_Click(object sender, RoutedEventArgs e)
        {
            var text = TxtNewQuestionText.Text?.Trim();
            var answer = TxtNewQuestionAnswer.Text?.Trim();

            if (string.IsNullOrEmpty(text))
            {
                ShowQuestionManagerToast("問題文を入力してください", isError: true);
                return;
            }

            if (string.IsNullOrEmpty(answer))
            {
                ShowQuestionManagerToast("答えを入力してください", isError: true);
                return;
            }

            var author = _profileService.PlayerName ?? "Unknown";

            if (_editingQuestion != null)
            {
                // Update existing question
                _editingQuestion.Text = text;
                _editingQuestion.Answer = answer;
                _editingQuestion.Author = author;
                
                ShowQuestionManagerToast("問題を更新しました");
                _editingQuestion = null;
                BtnAddToList.Content = "リストに追加";
            }
            else
            {
                // Add new question
                var dto = new QuestionImportDto
                {
                    Text = text,
                    Answer = answer,
                    Author = author
                };

                _createdQuestions.Add(dto);
                ShowQuestionManagerToast("リストに追加しました");
            }
            
            RefreshQuestionList();

            // Clear inputs
            TxtNewQuestionText.Text = string.Empty;
            TxtNewQuestionAnswer.Text = string.Empty;
            TxtNewQuestionText.Focus();
        }

        private void BtnClearInputs_Click(object sender, RoutedEventArgs e)
        {
            TxtNewQuestionText.Text = string.Empty;
            TxtNewQuestionAnswer.Text = string.Empty;
            TxtNewQuestionText.Focus();
            _editingQuestion = null;
            BtnAddToList.Content = "リストに追加";
        }

        private void BtnEditFromList_Click(object sender, RoutedEventArgs e)
        {
            if (sender is System.Windows.Controls.Button btn && btn.Tag is QuestionImportDto dto)
            {
                _editingQuestion = dto;
                TxtNewQuestionText.Text = dto.Text;
                TxtNewQuestionAnswer.Text = dto.Answer;
                TxtNewQuestionText.Focus();
                BtnAddToList.Content = "更新";
            }
        }

        private void BtnRemoveFromList_Click(object sender, RoutedEventArgs e)
        {
            if (sender is System.Windows.Controls.Button btn && btn.Tag is QuestionImportDto dto)
            {
                _createdQuestions.Remove(dto);
                RefreshQuestionList();
                
                // If we were editing this question, clear the form
                if (_editingQuestion == dto)
                {
                    TxtNewQuestionText.Text = string.Empty;
                    TxtNewQuestionAnswer.Text = string.Empty;
                    _editingQuestion = null;
                    BtnAddToList.Content = "リストに追加";
                }
            }
        }

        private void RefreshQuestionList()
        {
            ListCreatedQuestions.ItemsSource = null;
            ListCreatedQuestions.ItemsSource = _createdQuestions;
            UpdateQuestionListCount();
        }

        private async void BtnExportJson_Click(object sender, RoutedEventArgs e)
        {
            if (_createdQuestions.Count == 0)
            {
                ShowQuestionManagerToast("リストが空です", isError: true);
                return;
            }

            var dlg = new Microsoft.Win32.SaveFileDialog
            {
                Filter = "JSON files (*.json)|*.json",
                FileName = $"questions_{DateTime.Now:yyyyMMdd_HHmmss}.json",
                DefaultExt = ".json"
            };

            if (dlg.ShowDialog() != true) return;

            try
            {
                var json = JsonSerializer.Serialize(_createdQuestions, new JsonSerializerOptions { WriteIndented = true });
                await File.WriteAllTextAsync(dlg.FileName, json);
                ShowQuestionManagerToast($"{_createdQuestions.Count}件をエクスポートしました");
            }
            catch (Exception ex)
            {
                Logger.LogError(ex);
                ShowQuestionManagerToast("エクスポートに失敗しました", isError: true);
            }
        }

        private async void BtnImportToDb_Click(object sender, RoutedEventArgs e)
        {
            if (_createdQuestions.Count == 0)
            {
                ShowQuestionManagerToast("リストが空です", isError: true);
                return;
            }

            // Show confirmation dialog
            Dispatcher.Invoke(() =>
            {
                TxtImportConfirmMessage.Text = $"{_createdQuestions.Count}件の問題をDBにインポートしますか？";
                ImportToDbConfirmOverlay.Visibility = Visibility.Visible;
                ImportToDbConfirmOverlay.IsHitTestVisible = true;
                AnimateConfirmOverlayOpen(ImportToDbConfirmBorder);
            });
        }

        private async void BtnImportConfirmYes_Click(object sender, RoutedEventArgs e)
        {
            // Hide confirmation dialog
            ImportToDbConfirmOverlay.Visibility = Visibility.Collapsed;
            ImportToDbConfirmOverlay.IsHitTestVisible = false;

            try
            {
                int count = 0;
                foreach (var dto in _createdQuestions)
                {
                    await _questionService.AddQuestionAsync(
                        dto.Text ?? "",
                        dto.Answer ?? "",
                        dto.Author ?? ""
                    );
                    count++;
                }

                ShowQuestionManagerToast($"{count}件をDBに登録しました");
                _createdQuestions.Clear();
                RefreshQuestionList();
            }
            catch (Exception ex)
            {
                Logger.LogError(ex);
                ShowQuestionManagerToast("DB登録に失敗しました", isError: true);
            }
        }

        private void BtnImportConfirmNo_Click(object sender, RoutedEventArgs e)
        {
            ImportToDbConfirmOverlay.Visibility = Visibility.Collapsed;
            ImportToDbConfirmOverlay.IsHitTestVisible = false;
        }

        private void UpdateQuestionListCount()
        {
            TxtQuestionListCount.Text = $"({_createdQuestions.Count}件)";
        }

        // Question History
        private async Task LoadQuestionHistoryAsync()
        {
            await _historyService.LoadAsync();
            
            Dispatcher.Invoke(() =>
            {
                ListQuestionHistory.ItemsSource = _historyService.History;
                TxtHistoryCount.Text = $"({_historyService.History.Count}件)";
                
                if (_historyService.History.Count == 0)
                {
                    HistoryEmptyState.Visibility = Visibility.Visible;
                    ListQuestionHistory.Visibility = Visibility.Collapsed;
                }
                else
                {
                    HistoryEmptyState.Visibility = Visibility.Collapsed;
                    ListQuestionHistory.Visibility = Visibility.Visible;
                }
            });
        }

        private void BtnClearHistory_Click(object sender, RoutedEventArgs e)
        {
            if (_historyService.History.Count == 0)
            {
                ShowQuestionManagerToast("履歴は既に空です", isError: true);
                return;
            }

            ClearHistoryConfirmOverlay.Visibility = Visibility.Visible;
            ClearHistoryConfirmOverlay.IsHitTestVisible = true;
            AnimateConfirmOverlayOpen(ClearHistoryConfirmBorder);
        }

        private async void BtnClearHistoryConfirmYes_Click(object sender, RoutedEventArgs e)
        {
            ClearHistoryConfirmOverlay.Visibility = Visibility.Collapsed;
            ClearHistoryConfirmOverlay.IsHitTestVisible = false;

            await _historyService.ClearHistoryAsync();
            await LoadQuestionHistoryAsync();
            ShowQuestionManagerToast("履歴をクリアしました");
        }

        private void BtnClearHistoryConfirmNo_Click(object sender, RoutedEventArgs e)
        {
            ClearHistoryConfirmOverlay.Visibility = Visibility.Collapsed;
            ClearHistoryConfirmOverlay.IsHitTestVisible = false;
        }

        // Record question to history when played
        public async Task RecordQuestionPlayedAsync(Question question)
        {
            await _historyService.AddEntryAsync(question);
        }

        // Toast for Question Manager screens
        private async void ShowQuestionManagerToast(string message, bool isError = false)
        {
            Dispatcher.Invoke(() =>
            {
                ToastText.Text = message;
                ToastNotification.Background = new System.Windows.Media.SolidColorBrush(
                    isError ? System.Windows.Media.Color.FromRgb(255, 205, 210) : System.Windows.Media.Color.FromRgb(76, 175, 80)
                );
                
                ToastText.Foreground = new System.Windows.Media.SolidColorBrush(
                    isError ? System.Windows.Media.Color.FromRgb(183, 28, 28) : System.Windows.Media.Colors.White
                );
                
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
    }
}
