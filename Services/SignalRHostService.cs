using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR.Client;

namespace Kuiz.Services
{
    /// <summary>
    /// SignalRクライアントを使用したホスト機能
    /// Railway.appにデプロイされたKuizServerに接続
    /// </summary>
    public class SignalRHostService
    {
        public const int MaxPlayers = 4;

        private HubConnection? _connection;
        
        public string LobbyCode { get; private set; } = string.Empty;
        public int CurrentPlayerCount { get; set; } = 0;

        public bool IsRunning => _connection?.State == HubConnectionState.Connected;

        // Callbacks
        public Func<string, Task<bool>>? OnPlayerRegistered { get; set; }
        public Func<string, Task<bool>>? OnBuzzReceived { get; set; }
        public Func<string, string, Task<bool>>? OnAnswerReceived { get; set; }
        public Func<Task<object>>? OnStateRequested { get; set; }
        public Func<Task>? OnNextQuestionRequested { get; set; }

        public async Task<(bool Success, string? Error, string LobbyCode)> CreateLobbyAsync(string serverUrl, string hostName)
        {
            try
            {
                if (_connection != null && _connection.State == HubConnectionState.Connected)
                {
                    Logger.LogInfo("?? Lobby already created, returning existing lobby code");
                    return (true, null, LobbyCode);
                }

                // Ensure server URL doesn't end with /
                if (serverUrl.EndsWith('/'))
                    serverUrl = serverUrl.TrimEnd('/');

                Logger.LogInfo($"?? Creating lobby on server: {serverUrl}/gamehub");
                Logger.LogInfo($"?? Host: {hostName}");

                // SignalR接続を構築
                _connection = new HubConnectionBuilder()
                    .WithUrl($"{serverUrl}/gamehub")
                    .WithAutomaticReconnect()
                    .Build();

                Logger.LogInfo("?? HubConnection built, setting up event handlers...");

                // イベントハンドラー登録
                SetupEventHandlers();

                Logger.LogInfo("?? Starting connection...");

                // サーバーに接続
                await _connection.StartAsync();

                Logger.LogInfo($"? Connection established. State: {_connection.State}");
                Logger.LogInfo($"?? Creating lobby...");

                // ロビー作成
                LobbyCode = await _connection.InvokeAsync<string>("CreateLobby", hostName);
                CurrentPlayerCount = 1;

                Logger.LogInfo($"? Lobby created successfully! Code: {LobbyCode}");
                return (true, null, LobbyCode);
            }
            catch (Exception ex)
            {
                Logger.LogError($"? Failed to create lobby: {ex.Message}");
                Logger.LogError(ex);
                return (false, ex.Message, string.Empty);
            }
        }

        private void SetupEventHandlers()
        {
            if (_connection == null) return;

            // プレイヤー参加通知
            _connection.On<string>("PlayerJoined", async (playerName) =>
            {
                CurrentPlayerCount++;
                if (OnPlayerRegistered != null)
                {
                    await OnPlayerRegistered(playerName);
                }
            });

            // プレイヤー退出通知
            _connection.On<string>("PlayerLeft", (playerName) =>
            {
                CurrentPlayerCount--;
                Logger.LogInfo($"Player left: {playerName}");
            });

            // バズ受信
            _connection.On<string>("PlayerBuzzed", async (playerName) =>
            {
                if (OnBuzzReceived != null)
                {
                    await OnBuzzReceived(playerName);
                }
            });

            // 回答受信
            _connection.On<string, string>("PlayerAnswered", async (playerName, answer) =>
            {
                if (OnAnswerReceived != null)
                {
                    await OnAnswerReceived(playerName, answer);
                }
            });
        }

        public async Task StopAsync()
        {
            try
            {
                if (_connection != null && LobbyCode != null)
                {
                    await _connection.InvokeAsync("LeaveLobby", LobbyCode, "Host");
                    await _connection.StopAsync();
                    await _connection.DisposeAsync();
                    _connection = null;
                }

                LobbyCode = string.Empty;
                CurrentPlayerCount = 0;
            }
            catch (Exception ex)
            {
                Logger.LogError(ex);
            }
        }

        // ゲーム開始通知
        public async Task NotifyGameStartAsync(object gameSettings)
        {
            if (_connection != null && _connection.State == HubConnectionState.Connected)
            {
                await _connection.InvokeAsync("StartGame", LobbyCode, gameSettings);
            }
        }

        // ゲーム状態更新通知
        public async Task NotifyGameStateAsync(object gameState)
        {
            if (_connection != null && _connection.State == HubConnectionState.Connected)
            {
                await _connection.InvokeAsync("UpdateGameState", LobbyCode, gameState);
            }
        }

        // ゲーム終了通知
        public async Task NotifyGameEndAsync(object results)
        {
            if (_connection != null && _connection.State == HubConnectionState.Connected)
            {
                await _connection.InvokeAsync("EndGame", LobbyCode, results);
            }
        }

        // プレイヤーバズ通知
        public async Task NotifyPlayerBuzzedAsync(string playerName)
        {
            if (_connection != null && _connection.State == HubConnectionState.Connected)
            {
                await _connection.InvokeAsync("SendBuzz", LobbyCode, playerName);
            }
        }

        // 回答結果通知
        public async Task NotifyAnswerResultAsync(string playerName, bool isCorrect)
        {
            if (_connection != null && _connection.State == HubConnectionState.Connected)
            {
                await _connection.InvokeAsync("SendAnswerResult", LobbyCode, playerName, isCorrect);
            }
        }

        // 次の問題通知
        public async Task NotifyNextQuestionAsync()
        {
            if (_connection != null && _connection.State == HubConnectionState.Connected)
            {
                await _connection.InvokeAsync("SendNextQuestion", LobbyCode);
            }
        }
    }
}
