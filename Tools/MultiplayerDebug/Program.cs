using System.Text.Json;
using KuizServer.Hubs;
using KuizServer.Services;
using Microsoft.AspNetCore.SignalR.Client;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddSignalR();
builder.Services.AddSingleton<LobbyService>();
builder.Logging.SetMinimumLevel(LogLevel.Warning);
var app = builder.Build();
app.Urls.Add("http://127.0.0.1:5187");
app.MapHub<GameHub>("/gamehub");
app.MapGet("/health", () => new { status = "healthy", service = "Kuiz local debug" });
app.MapGet("/api/question", () => new[]
{
    new { id = 1, text = "1 + 1 はいくつ？", answer = "2", author = "Debug" },
    new { id = 2, text = "日本の首都は？", answer = "東京", author = "Debug" },
    new { id = 3, text = "英語で猫は？", answer = "cat", author = "Debug" }
});
app.MapGet("/api/lobby/{code}", (string code, LobbyService service) => service.GetLobbyStateByCode(code));
await app.StartAsync();
if (args.Contains("--serve"))
{
    await app.WaitForShutdownAsync();
    return;
}

try
{
    await using var host = new HubConnectionBuilder().WithUrl("http://127.0.0.1:5187/gamehub").Build();
    await using var guest = new HubConnectionBuilder().WithUrl("http://127.0.0.1:5187/gamehub").Build();
    await Task.WhenAll(host.StartAsync(), guest.StartAsync());
    var code = await host.InvokeAsync<string>("CreateLobby", "SmokeHost");
    Check(await guest.InvokeAsync<bool>("JoinLobby", code, "SmokeGuest"), "join lobby");
    using var http = new HttpClient();
    var lobby = JsonDocument.Parse(await http.GetStringAsync($"http://127.0.0.1:5187/api/lobby/{code}"));
    Check(lobby.RootElement.GetProperty("playerCount").GetInt32() == 2, "HTTP lobby lookup has both players");

    var start = new TaskCompletionSource<JsonElement>(TaskCreationOptions.RunContinuationsAsynchronously);
    guest.On<JsonElement>("GameStarting", value => start.TrySetResult(value));
    await host.InvokeAsync("StartGame", new { PointsToWin = 3, Questions = new[] { new { Text = "1+1", Answer = "2" } } });
    var settings = await start.Task.WaitAsync(TimeSpan.FromSeconds(5));
    Check(settings.GetProperty("pointsToWin").GetInt32() == 3, "game settings arrive as an object");

    var state = new TaskCompletionSource<JsonElement>(TaskCreationOptions.RunContinuationsAsynchronously);
    guest.On<JsonElement>("GameStateUpdated", value => state.TrySetResult(value));
    await host.InvokeAsync("UpdateGameState", new { scores = new Dictionary<string, int> { ["SmokeGuest"] = 1 }, revealed = "1+1" });
    Check((await state.Task.WaitAsync(TimeSpan.FromSeconds(5))).GetProperty("scores").GetProperty("SmokeGuest").GetInt32() == 1, "score and question state");

    var buzz = new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);
    host.On<string>("PlayerBuzzed", value => buzz.TrySetResult(value));
    await guest.InvokeAsync("SendBuzz");
    Check(await buzz.Task.WaitAsync(TimeSpan.FromSeconds(5)) == "SmokeGuest", "buzz identifies guest");
    var answer = new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);
    host.On<string, string>("PlayerAnswered", (name, value) => answer.TrySetResult(name + ":" + value));
    await guest.InvokeAsync("SendAnswer", "2");
    Check(await answer.Task.WaitAsync(TimeSpan.FromSeconds(5)) == "SmokeGuest:2", "answer reaches host");
    var result = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
    guest.On<string, bool>("AnswerResult", (name, correct) => result.TrySetResult(name == "SmokeGuest" && correct));
    await host.InvokeAsync("SendAnswerResult", "SmokeGuest", true);
    Check(await result.Task.WaitAsync(TimeSpan.FromSeconds(5)), "two-argument answer result");
    var next = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
    guest.On("NextQuestion", () => next.TrySetResult());
    await host.InvokeAsync("SendNextQuestion");
    await next.Task.WaitAsync(TimeSpan.FromSeconds(5));
    Check(true, "zero-argument next question");
    var ended = new TaskCompletionSource<JsonElement>(TaskCreationOptions.RunContinuationsAsynchronously);
    guest.On<JsonElement>("GameEnded", value => ended.TrySetResult(value));
    await host.InvokeAsync("EndGame", new { winner = "SmokeGuest" });
    Check((await ended.Task.WaitAsync(TimeSpan.FromSeconds(5))).GetProperty("winner").GetString() == "SmokeGuest", "winner notification");
    bool rejected = false;
    try { await guest.InvokeAsync("StartGame", new { }); }
    catch (Microsoft.AspNetCore.SignalR.HubException) { rejected = true; }
    Check(rejected, "guest cannot start a game");
    var left = new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);
    host.On<string>("PlayerLeft", value => left.TrySetResult(value));
    await guest.InvokeAsync("LeaveLobby");
    Check(await left.Task.WaitAsync(TimeSpan.FromSeconds(5)) == "SmokeGuest", "departure notification");
    Console.WriteLine("PASS: all multiplayer transport checks");
}
finally { await app.StopAsync(); }

static void Check(bool condition, string description)
{
    if (!condition) throw new InvalidOperationException("FAIL: " + description);
    Console.WriteLine("PASS: " + description);
}
