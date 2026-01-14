using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR.Client;

namespace Kuiz.Services
{
    /// <summary>
    /// SignalRクライアント（プレイヤー側）
    /// Railway.appにデプロイされたKuizServerに接続
    /// </summary>
    public class SignalRClientService
    {
        private HubConnection? _connection;
        private string _playerName = string.Empty;
        private string _lobbyCode = string.Empty;
        private string _serverUrl = string.Empty;

        public bool IsConnected => _connection?.State == HubConnectionState.Connected;

        // イベントハンドラー
        public event Action<object>? OnGameStarting;
        public event Action<string>? OnPlayerJoined;
        public event Action<string>? OnPlayerLeft;
        public event Action<string>? OnPlayerBuzzed;
        public event Action<object>? OnGameStateUpdated;
        public event Action<object>? OnGameEnded;

        public async Task<bool> ConnectAsync(string serverUrl, string lobbyCode, string playerName)
        {
            try
            {
                _playerName = playerName;
                _lobbyCode = lobbyCode;
                _serverUrl = serverUrl;

                // Ensure server URL doesn't end with /
                if (_serverUrl.EndsWith('/'))
                    _serverUrl = _serverUrl.TrimEnd('/');

                Logger.LogInfo($"Connecting to server: {_serverUrl}/gamehub");

                // SignalR接続を構築
                _connection = new HubConnectionBuilder()
                    .WithUrl($"{_serverUrl}/gamehub")
                    .WithAutomaticReconnect()
                    .Build();

                // イベントハンドラー登録
                SetupEventHandlers();

                // サーバーに接続
                await _connection.StartAsync();

                // ロビーに参加
                var success = await _connection.InvokeAsync<bool>("JoinLobby", lobbyCode, playerName);
                
                if (success)
                {
                    Logger.LogInfo($"Joined lobby {lobbyCode} as {playerName}");
                }

                return success;
            }
            catch (Exception ex)
            {
                Logger.LogError(ex);
                return false;
            }
        }

        private void SetupEventHandlers()
        {
            if (_connection == null) return;

            _connection.On<object>("GameStarting", (settings) =>
            {
                OnGameStarting?.Invoke(settings);
            });

            _connection.On<string>("PlayerJoined", (playerName) =>
            {
                OnPlayerJoined?.Invoke(playerName);
            });

            _connection.On<string>("PlayerLeft", (playerName) =>
            {
                OnPlayerLeft?.Invoke(playerName);
            });

            _connection.On<string>("PlayerBuzzed", (playerName) =>
            {
                OnPlayerBuzzed?.Invoke(playerName);
            });

            _connection.On<object>("GameStateUpdated", (state) =>
            {
                OnGameStateUpdated?.Invoke(state);
            });

            _connection.On<object>("GameEnded", (results) =>
            {
                OnGameEnded?.Invoke(results);
            });
        }

        public async Task SendBuzzAsync()
        {
            if (_connection != null && _connection.State == HubConnectionState.Connected)
            {
                await _connection.InvokeAsync("SendBuzz", _lobbyCode, _playerName);
            }
        }

        public async Task SendAnswerAsync(string answer)
        {
            if (_connection != null && _connection.State == HubConnectionState.Connected)
            {
                await _connection.InvokeAsync("SendAnswer", _lobbyCode, _playerName, answer);
            }
        }

        public async Task<object> GetLobbyStateAsync()
        {
            if (_connection != null && _connection.State == HubConnectionState.Connected)
            {
                return await _connection.InvokeAsync<object>("GetLobbyState", _lobbyCode);
            }
            return new { exists = false };
        }

        public async Task DisconnectAsync()
        {
            try
            {
                if (_connection != null && _connection.State == HubConnectionState.Connected)
                {
                    await _connection.InvokeAsync("LeaveLobby", _lobbyCode, _playerName);
                    await _connection.StopAsync();
                    await _connection.DisposeAsync();
                }
                _connection = null;
            }
            catch (Exception ex)
            {
                Logger.LogError(ex);
            }
        }
    }
}
