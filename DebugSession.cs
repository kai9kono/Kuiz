using System.IO;
using System.Text.Json;

namespace Kuiz;

internal static class DebugSession
{
    public static string? Role { get; private set; }
    public static string? Root { get; private set; }
    public static bool Smoke { get; private set; }
    public static string ServerUrl { get; private set; } = "http://127.0.0.1:5187";

    public static void Initialize(string[] args)
    {
#if DEBUG
        string? Value(string name)
        {
            var index = Array.IndexOf(args, name);
            return index >= 0 && index + 1 < args.Length ? args[index + 1] : null;
        }
        Role = Value("--debug-role");
        Smoke = args.Contains("--debug-smoke");
        if (Role is null) return;
        if (Role is not ("host" or "guest")) throw new ArgumentException("Invalid debug role.");
        Root = Path.GetFullPath(Value("--debug-root") ?? throw new ArgumentException("Missing --debug-root."));
        ServerUrl = Value("--server-url") ?? ServerUrl;
        var directory = Path.Combine(GetDataRoot(Environment.SpecialFolder.ApplicationData), "Kuiz");
        Directory.CreateDirectory(directory);
        File.WriteAllText(Path.Combine(directory, "config.json"), JsonSerializer.Serialize(new
        {
            ApiUrl = ServerUrl.TrimEnd('/') + "/api/question", ServerUrl, IsDebugMode = true
        }));
        File.WriteAllText(Path.Combine(directory, "profile.json"), JsonSerializer.Serialize(new
        {
            Name = Role == "host" ? "DebugHost" : "DebugGuest", IsDarkMode = false
        }));
#endif
    }

    public static string GetDataRoot(Environment.SpecialFolder folder) =>
        Role is null ? Environment.GetFolderPath(folder) : Path.Combine(Root!, Role);
}
