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
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
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
        /// Railway APIのURL（デフォルト: 本番環境）
        /// ローカル開発時は "http://localhost:8080/api/question" に変更可能
        /// </summary>
        public string ApiUrl { get; set; } = "https://kuiz-production.up.railway.app/api/question";

        /// <summary>
        /// デバッグモード
        /// </summary>
        public bool IsDebugMode { get; set; } = false;
    }
}
