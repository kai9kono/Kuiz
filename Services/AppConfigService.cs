using System;
using System.IO;
using System.Text.Json;

namespace Kuiz.Services
{
    /// <summary>
    /// アプリケーション設定を管理
    /// </summary>
    public class AppConfigService
    {
        private static readonly string ConfigPath = Path.Combine(
            Kuiz.DebugSession.GetDataRoot(Environment.SpecialFolder.ApplicationData),
            "Kuiz",
            "config.json"
        );

        public AppConfig Config { get; private set; } = new();

        public AppConfigService()
        {
            Load();
        }

        public void Load()
        {
            try
            {
                if (File.Exists(ConfigPath))
                {
                    var json = File.ReadAllText(ConfigPath);
                    Config = JsonSerializer.Deserialize<AppConfig>(json) ?? new AppConfig();
                    // Migrate only the retired Kuiz endpoint; preserve custom/debug servers.
                    var migrated = false;
                    if (IsRetiredEndpoint(Config.ApiUrl))
                    {
                        Config.ApiUrl = new AppConfig().ApiUrl;
                        migrated = true;
                    }
                    if (IsRetiredEndpoint(Config.ServerUrl))
                    {
                        Config.ServerUrl = new AppConfig().ServerUrl;
                        migrated = true;
                    }
                    if (migrated) Save();
                }
                else
                {
                    Config = new AppConfig();
                    Save();
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex);
                Config = new AppConfig();
            }
        }

        private static bool IsRetiredEndpoint(string? url) =>
            Uri.TryCreate(url, UriKind.Absolute, out var uri) &&
            uri.Host.Equals("kuiz-production.up.railway.app", StringComparison.OrdinalIgnoreCase);

        public void Save()
        {
            try
            {
                var dir = Path.GetDirectoryName(ConfigPath);
                if (dir != null && !Directory.Exists(dir))
                {
                    Directory.CreateDirectory(dir);
                }

                var json = JsonSerializer.Serialize(Config, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(ConfigPath, json);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex);
            }
        }
    }

    public class AppConfig
    {
        /// <summary>
        /// 本番の問題APIのURL
        /// </summary>
        public string ApiUrl { get; set; } = "https://kuiz-server.onrender.com/api/question";

        /// <summary>
        /// 本番ゲームサーバーのベースURL
        /// </summary>
        public string ServerUrl { get; set; } = "https://kuiz-server.onrender.com";

        /// <summary>
        /// デバッグモード
        /// </summary>
        public bool IsDebugMode { get; set; } = false;
    }
}
