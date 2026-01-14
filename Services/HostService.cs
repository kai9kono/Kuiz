using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Security.Principal;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Kuiz.Services
{
    /// <summary>
    /// HTTPリスナーによるホスト機能を管理
    /// </summary>
    public class HostService
    {
        public const string DefaultListenUrl = "http://+:5000/";
        public const int MaxPlayers = 4;

        private HttpListener? _httpListener;
        private Task? _listenerTask;
        private CancellationTokenSource? _cts;
        private bool _isListening;
        
        public string LobbyCode { get; private set; } = string.Empty;
        public int CurrentPlayerCount { get; set; } = 0;

        public bool IsRunning
        {
            get
            {
                try
                {
                    return _isListening && _httpListener != null && _httpListener.IsListening;
                }
                catch
                {
                    return false;
                }
            }
        }

        // Callbacks (use properties instead of events for direct assignment)
        public Func<string, Task<bool>>? OnPlayerRegistered { get; set; }
        public Func<string, Task<bool>>? OnBuzzReceived { get; set; }
        public Func<string, string, Task<bool>>? OnAnswerReceived { get; set; }
        public Func<Task<object>>? OnStateRequested { get; set; }
        public Func<Task>? OnNextQuestionRequested { get; set; }

        public async Task<(bool Success, string? Error, string? ActualUrl, string LobbyCode)> StartAsync(string? url = null)
        {
            Logger.LogInfo($"StartAsync called. IsRunning={IsRunning}, _isListening={_isListening}, _httpListener={_httpListener != null}");
            
            // If already running, return success with current state
            if (IsRunning)
            {
                Logger.LogInfo("Host service already running, skipping start");
                return (true, null, url ?? DefaultListenUrl, LobbyCode);
            }

            // Clean up any existing listener before creating new one
            if (_httpListener != null)
            {
                Logger.LogInfo("Cleaning up existing HttpListener before starting new one");
                try
                {
                    if (_httpListener.IsListening)
                    {
                        _httpListener.Stop();
                    }
                    _httpListener.Close();
                }
                catch (Exception ex)
                {
                    Logger.LogError(ex);
                }
                finally
                {
                    _httpListener = null;
                }
            }
            
            // Reset listening flag before starting
            _isListening = false;

            url ??= DefaultListenUrl;
            if (!url.EndsWith('/')) url += "/";
            
            LobbyCode = GenerateLobbyCode();
            CurrentPlayerCount = 1;

            HttpListener? newListener = null;
            try
            {
                newListener = new HttpListener();
                newListener.Prefixes.Add(url);
                newListener.Start();
                
                // Only assign to _httpListener after successful start
                _httpListener = newListener;
                _isListening = true;
                _cts = new CancellationTokenSource();
                _listenerTask = Task.Run(() => ListenerLoopAsync(_cts.Token));
                Logger.LogInfo($"Successfully started listener on {url}");
            }
            catch (HttpListenerException hlex)
            {
                Logger.LogError(hlex);

                // Dispose failed listener
                if (newListener != null)
                {
                    try
                    {
                        newListener.Close();
                    }
                    catch { }
                    newListener = null;
                }

                // Try fallback: if URL used '+' try localhost instead
                if (url.Contains("+"))
                {
                    var fallback = url.Replace("://+:", "://localhost:");
                    if (fallback == url) fallback = url.Replace("+", "localhost");

                    try
                    {
                        var fallbackListener = new HttpListener();
                        fallbackListener.Prefixes.Add(fallback);
                        fallbackListener.Start();

                        _httpListener = fallbackListener;
                        _isListening = true;
                        _cts = new CancellationTokenSource();
                        _listenerTask = Task.Run(() => ListenerLoopAsync(_cts.Token));
                        Logger.LogInfo($"Started listener on fallback {fallback}");
                        return (true, null, fallback, LobbyCode);
                    }
                    catch (Exception exFb)
                    {
                        Logger.LogError(exFb);
                    }
                }

                var user = WindowsIdentity.GetCurrent()?.Name ?? Environment.UserName;
                var cmd = $"netsh http add urlacl url={url} user=\"{user}\"";
                var error = $"Failed to bind to {url} due to access denied.\nRun as administrator:\n{cmd}";

                _httpListener = null;
                _isListening = false;
                LobbyCode = string.Empty;
                CurrentPlayerCount = 0;
                return (false, error, null, string.Empty);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex);
                
                // Dispose failed listener
                if (newListener != null)
                {
                    try
                    {
                        newListener.Close();
                    }
                    catch { }
                }
                
                _httpListener = null;
                _isListening = false;
                LobbyCode = string.Empty;
                CurrentPlayerCount = 0;
                return (false, ex.Message, null, string.Empty);
            }

            Logger.LogInfo($"Listener task started for {url}");
            return (true, null, url, LobbyCode);
        }

        private string GenerateLobbyCode()
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            var random = new Random();
            var code = new char[6];
            for (int i = 0; i < 6; i++)
            {
                code[i] = chars[random.Next(chars.Length)];
            }
            return new string(code);
        }

        public async Task StopAsync()
        {
            Logger.LogInfo("StopAsync called");
            
            if (_httpListener == null)
            {
                Logger.LogInfo("StopAsync: _httpListener is null, nothing to stop");
                return;
            }

            _isListening = false;
            Logger.LogInfo("Set _isListening to false");

            try
            {
                _cts?.Cancel();
                Logger.LogInfo("Cancellation token canceled");
            }
            catch (Exception ex)
            {
                Logger.LogError(ex);
            }

            try
            {
                _httpListener?.Stop();
                _httpListener?.Close();
                Logger.LogInfo("HttpListener stopped and closed");
            }
            catch (Exception ex)
            {
                Logger.LogError(ex);
            }

            // Wait for listener task with timeout
            try
            {
                if (_listenerTask != null && !_listenerTask.IsCompleted)
                {
                    Logger.LogInfo("Waiting for listener task to complete...");
                    var completed = await Task.WhenAny(_listenerTask, Task.Delay(1000));
                    if (completed == _listenerTask)
                    {
                        Logger.LogInfo("Listener task completed");
                        await _listenerTask; // Observe any exceptions
                    }
                    else
                    {
                        Logger.LogInfo("Listener task did not complete within timeout");
                    }
                }
                else
                {
                    Logger.LogInfo("Listener task already completed or null");
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex);
            }

            _httpListener = null;
            _listenerTask = null;
            _cts?.Dispose();
            _cts = null;
            LobbyCode = string.Empty;
            CurrentPlayerCount = 0;
            
            Logger.LogInfo("Host service stopped");
        }

        private async Task ListenerLoopAsync(CancellationToken ct)
        {
            while (!ct.IsCancellationRequested && _httpListener != null)
            {
                HttpListenerContext? ctx = null;
                try
                {
                    ctx = await _httpListener.GetContextAsync();
                }
                catch
                {
                    break;
                }

                _ = Task.Run(() => HandleRequestAsync(ctx!, ct), ct);
            }
        }

        private async Task HandleRequestAsync(HttpListenerContext context, CancellationToken ct)
        {
            var path = context.Request.Url?.AbsolutePath ?? "/";

            try
            {
                if (path == "/register" && context.Request.HttpMethod == "POST")
                {
                    await HandleRegisterAsync(context);
                }
                else if (path == "/buzz" && context.Request.HttpMethod == "POST")
                {
                    await HandleBuzzAsync(context);
                }
                else if (path == "/state" && context.Request.HttpMethod == "GET")
                {
                    await HandleStateAsync(context);
                }
                else if (path == "/question/next" && context.Request.HttpMethod == "POST")
                {
                    await HandleNextQuestionAsync(context);
                }
                else if (path == "/answer" && context.Request.HttpMethod == "POST")
                {
                    await HandleAnswerAsync(context);
                }
                else
                {
                    context.Response.StatusCode = 404;
                    context.Response.Close();
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex);
                try
                {
                    context.Response.StatusCode = 500;
                    context.Response.Close();
                }
                catch { }
            }
        }

        private async Task HandleRegisterAsync(HttpListenerContext context)
        {
            using var sr = new StreamReader(context.Request.InputStream);
            var body = await sr.ReadToEndAsync();
            var obj = JsonSerializer.Deserialize<JsonElement>(body);

            if (obj.TryGetProperty("name", out var nameProp) && 
                obj.TryGetProperty("lobbyCode", out var codeProp))
            {
                var name = nameProp.GetString();
                var code = codeProp.GetString();
                
                // Check version if provided
                if (obj.TryGetProperty("version", out var versionProp))
                {
                    var clientVersion = versionProp.GetString();
                    if (clientVersion != Kuiz.AppVersion.Version)
                    {
                        context.Response.StatusCode = 400;
                        await WriteJsonResponseAsync(context, new { error = "Version mismatch" });
                        return;
                    }
                }
                
                if (code != LobbyCode)
                {
                    context.Response.StatusCode = 403;
                    await WriteJsonResponseAsync(context, new { error = "Invalid lobby code" });
                    return;
                }
                
                if (CurrentPlayerCount >= MaxPlayers)
                {
                    context.Response.StatusCode = 409;
                    await WriteJsonResponseAsync(context, new { error = "Lobby is full" });
                    return;
                }
                
                if (!string.IsNullOrEmpty(name) && OnPlayerRegistered != null)
                {
                    var accepted = await OnPlayerRegistered(name);
                    if (!accepted)
                    {
                        context.Response.StatusCode = 409;
                        await WriteJsonResponseAsync(context, new { error = "Registration rejected" });
                        return;
                    }
                    
                    CurrentPlayerCount++;
                    await WriteJsonResponseAsync(context, new { ok = true });
                    return;
                }
            }

            context.Response.StatusCode = 400;
            context.Response.Close();
        }

        private async Task HandleBuzzAsync(HttpListenerContext context)
        {
            using var sr = new StreamReader(context.Request.InputStream);
            var body = await sr.ReadToEndAsync();
            var obj = JsonSerializer.Deserialize<JsonElement>(body);

            if (obj.TryGetProperty("name", out var nameProp))
            {
                var name = nameProp.GetString();
                if (!string.IsNullOrEmpty(name) && OnBuzzReceived != null)
                {
                    var accepted = await OnBuzzReceived(name);
                    if (!accepted)
                    {
                        context.Response.StatusCode = 409;
                        context.Response.Close();
                        return;
                    }

                    await WriteJsonResponseAsync(context, new { ok = true });
                    return;
                }
            }

            context.Response.StatusCode = 400;
            context.Response.Close();
        }

        private async Task HandleStateAsync(HttpListenerContext context)
        {
            if (OnStateRequested != null)
            {
                var state = await OnStateRequested();
                await WriteJsonResponseAsync(context, state);
            }
            else
            {
                context.Response.StatusCode = 500;
                context.Response.Close();
            }
        }

        private async Task HandleNextQuestionAsync(HttpListenerContext context)
        {
            if (OnNextQuestionRequested != null)
            {
                await OnNextQuestionRequested();
            }
            await WriteJsonResponseAsync(context, new { ok = true });
        }

        private async Task HandleAnswerAsync(HttpListenerContext context)
        {
            using var sr = new StreamReader(context.Request.InputStream);
            var body = await sr.ReadToEndAsync();
            var obj = JsonSerializer.Deserialize<JsonElement>(body);

            if (obj.TryGetProperty("name", out var nameProp) &&
                obj.TryGetProperty("answer", out var answerProp))
            {
                var name = nameProp.GetString();
                var answer = answerProp.GetString();

                if (!string.IsNullOrEmpty(name) && !string.IsNullOrEmpty(answer) && OnAnswerReceived != null)
                {
                    var correct = await OnAnswerReceived(name, answer);
                    await WriteJsonResponseAsync(context, new { correct });
                    return;
                }
            }

            context.Response.StatusCode = 400;
            context.Response.Close();
        }

        private async Task WriteJsonResponseAsync(HttpListenerContext context, object data)
        {
            context.Response.ContentType = "application/json";
            var bytes = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(data));
            await context.Response.OutputStream.WriteAsync(bytes, 0, bytes.Length);
            context.Response.Close();
        }
    }
}
