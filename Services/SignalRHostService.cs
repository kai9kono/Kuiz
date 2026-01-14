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
        // Railway.appのURL
        // 開発環境: http://localhost:8080
        // 本番環境: https://kuiz-production.up.railway.app
#if DEBUG
        private const string ServerUrl = "http://localhost:8080"; // 開発環境
#else
        private const string ServerUrl = "https://kuiz-production.up.railway.app"; // 本番環境
#endif
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

        public async Task<(bool Success, string? Error, string? ActualUrl, string LobbyCode)> StartAsync(string? playerName = null)
        {
            try
            {
                if (_connection != null && _connection.State == HubConnectionState.Connected)
                {
                    return (true, null, ServerUrl, LobbyCode);
                }

                // SignalR接続を構築
                _connection = new HubConnectionBuilder()
                    .WithUrl($"{ServerUrl}/gamehub")
                    .WithAutomaticReconnect()
                    .Build();

                // イベントハンドラー登録
                SetupEventHandlers();

                // サーバーに接続
                await _connection.StartAsync();

                // ロビー作成
                LobbyCode = await _connection.InvokeAsync<string>("CreateLobby", playerName ?? "Host");
                CurrentPlayerCount = 1;

                Logger.LogInfo($"SignalR connected to {ServerUrl}, Lobby: {LobbyCode}");
                return (true, null, ServerUrl, LobbyCode);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex);
                return (false, ex.Message, null, string.Empty);
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
    }
}
