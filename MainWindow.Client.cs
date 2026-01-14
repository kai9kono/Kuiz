using System;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using Kuiz.Models;

namespace Kuiz
{
    /// <summary>
    /// クライアント（参加者）関連のUI処理
    /// </summary>
    public partial class MainWindow
    {
        private int _lastClientSessionId = -1;
        private int _lastClientQuestionIndex = -1;
        private CancellationTokenSource? _pollStateCts;

        private void BtnTitleJoin_Click(object sender, RoutedEventArgs e) => ShowPanel(JoinPanel);
        private void BtnJoinBack_Click(object sender, RoutedEventArgs e) => ShowPanel(TitlePanel);

        private async void BtnJoinConnect_Click(object sender, RoutedEventArgs e)
        {
            var lobbyCode = TxtLobbyCodeInput.Text.Trim().ToUpper();
            var name = TxtJoinPlayerName.Text.Trim();

            if (string.IsNullOrEmpty(lobbyCode) || lobbyCode.Length != 6 || string.IsNullOrEmpty(name))
            {
                TxtJoinStatus.Text = "ロビーコードとプレイヤー名を入力してください";
                return;
            }

            TxtJoinStatus.Text = "接続中...";

            // Try localhost:5000 as default host URL
            var hostUrl = "http://localhost:5000/";
            var reg = new { name, lobbyCode, version = AppVersion.Version };
            var content = new StringContent(JsonSerializer.Serialize(reg), Encoding.UTF8, "application/json");

            try
            {
                var res = await _httpClient.PostAsync(new Uri(new Uri(hostUrl), "/register"), content);

                if (res.IsSuccessStatusCode)
                {
                    TxtJoinStatus.Text = "ロビーに参加しました！";
                    _profileService.Save(name);
                    ShowPanel(GamePanel);
                    
                    // Cancel any existing poll loop
                    _pollStateCts?.Cancel();
                    _pollStateCts?.Dispose();
                    
                    // Start new poll loop
                    _pollStateCts = new CancellationTokenSource();
                    _ = PollStateLoopAsync(hostUrl, _pollStateCts.Token);
                }
                else if (res.StatusCode == System.Net.HttpStatusCode.BadRequest)
                {
                    var errorJson = await res.Content.ReadAsStringAsync();
                    try
                    {
                        var errorObj = JsonSerializer.Deserialize<JsonElement>(errorJson);
                        if (errorObj.TryGetProperty("error", out var errorMsg))
                        {
                            var msg = errorMsg.GetString();
                            if (msg == "Version mismatch")
                            {
                                TxtJoinStatus.Text = "バージョンが一致しません。アプリを更新してください";
                            }
                            else
                            {
                                TxtJoinStatus.Text = "参加できませんでした";
                            }
                        }
                        else
                        {
                            TxtJoinStatus.Text = "参加できませんでした";
                        }
                    }
                    catch
                    {
                        TxtJoinStatus.Text = "参加できませんでした";
                    }
                }
                else if (res.StatusCode == System.Net.HttpStatusCode.Forbidden)
                {
                    TxtJoinStatus.Text = "ロビーコードが正しくありません";
                }
                else if (res.StatusCode == System.Net.HttpStatusCode.Conflict)
                {
                    var errorJson = await res.Content.ReadAsStringAsync();
                    try
                    {
                        var errorObj = JsonSerializer.Deserialize<JsonElement>(errorJson);
                        if (errorObj.TryGetProperty("error", out var errorMsg))
                        {
                            var msg = errorMsg.GetString();
                            if (msg == "Lobby is full")
                            {
                                TxtJoinStatus.Text = "ロビーが満員です（最大4人）";
                            }
                            else
                            {
                                TxtJoinStatus.Text = "参加できませんでした";
                            }
                        }
                        else
                        {
                            TxtJoinStatus.Text = "参加できませんでした";
                        }
                    }
                    catch
                    {
                        TxtJoinStatus.Text = "参加できませんでした";
                    }
                }
                else
                {
                    TxtJoinStatus.Text = "接続に失敗しました";
                }
            }
            catch (Exception ex)
            {
                TxtJoinStatus.Text = "エラー: ホストに接続できません";
                Logger.LogError(ex);
            }
        }

        private async Task PollStateLoopAsync(string hostUrl, CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    await Task.Delay(500, cancellationToken);
                    var res = await _httpClient.GetAsync(new Uri(new Uri(hostUrl), "/state"), cancellationToken);

                    if (res.IsSuccessStatusCode)
                    {
                        var json = await res.Content.ReadAsStringAsync();
                        var state = JsonSerializer.Deserialize<StateDto>(json);
                        Dispatcher.Invoke(() => ApplyState(state));
                    }
                }
                catch (OperationCanceledException)
                {
                    // Loop was cancelled - exit gracefully
                    Logger.LogInfo("PollStateLoopAsync cancelled");
                    break;
                }
                catch (Exception ex)
                {
                    Logger.LogError(ex);
                }
            }
        }

        private void ApplyState(StateDto? state)
        {
            if (state == null) return;

            _gameState.QueuePosition = state.questionIndex;
            _gameState.RevealedText = state.revealed ?? "";

            if (state.sessionId != _lastClientSessionId)
            {
                _gameState.AttemptedThisQuestion.Clear();
                _gameState.BuzzOrder.Clear();
                _gameState.PausedForBuzz = false;
                _isAnswerDialogOpen = false;
                _isPreDisplay = false;
                _lastClientQuestionIndex = -1;
                _lastClientSessionId = state.sessionId;
                
                Dispatcher.Invoke(() =>
                {
                    AnswerOverlay.Visibility = Visibility.Collapsed;
                    AnswerOverlay.IsHitTestVisible = false;
                    TxtAnsweringBadge.Visibility = Visibility.Collapsed;
                });
            }

            if (state.questionIndex != _lastClientQuestionIndex)
            {
                _gameState.AttemptedThisQuestion.Clear();
                _gameState.BuzzOrder.Clear();
                _gameState.PausedForBuzz = false;
                _lastClientQuestionIndex = state.questionIndex;
            }

            _gameState.BuzzOrder.Clear();
            if (state.buzzOrder != null)
            {
                foreach (var b in state.buzzOrder)
                {
                    _gameState.BuzzOrder.Add(b);
                }
            }

            _gameState.Scores.Clear();
            if (state.scores != null)
            {
                foreach (var kv in state.scores)
                {
                    _gameState.Scores[kv.Key] = kv.Value;
                }
            }

            _gameState.Mistakes.Clear();
            if (state.mistakes != null)
            {
                foreach (var kv in state.mistakes)
                {
                    _gameState.Mistakes[kv.Key] = kv.Value;
                }
            }

            if (!_isAnswerDialogOpen && !_isPreDisplay)
            {
                TxtGameQuestion.Text = _gameState.RevealedText;
            }

            ListPlayersInGame.ItemsSource = _gameState.GetPlayerStates();

            UpdateBuzzButtonState();
        }
    }
}
