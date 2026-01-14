using System;
using System.IO;
using System.Text.Json;

namespace Kuiz.Services
{
    /// <summary>
    /// プロフィール（プレイヤー名）と設定の保存・読み込みを担当
    /// </summary>
    public class ProfileService
    {
        private readonly string _profileDir;
        private readonly string _profilePath;

        public string? PlayerName { get; private set; }
        public bool IsDarkMode { get; private set; }

        public ProfileService()
        {
            _profileDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Kuiz");
            _profilePath = Path.Combine(_profileDir, "profile.json");
        }

        public void Load()
        {
            try
            {
                if (File.Exists(_profilePath))
                {
                    var text = File.ReadAllText(_profilePath);
                    var obj = JsonSerializer.Deserialize<ProfileData>(text);
                    if (obj != null)
                    {
                        if (!string.IsNullOrWhiteSpace(obj.Name))
                        {
                            PlayerName = obj.Name;
                        }
                        else
                        {
                            PlayerName = "ちびすけ明太子";
                        }
                        IsDarkMode = obj.IsDarkMode;
                    }
                }
                else
                {
                    // プロファイルファイルが存在しない場合、デフォルト名を設定
                    PlayerName = "ちびすけ明太子";
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex);
                PlayerName = "ちびすけ明太子";
            }
        }


        public void Save(string name, bool? darkMode = null)
        {
            try
            {
                PlayerName = name;
                if (darkMode.HasValue)
                {
                    IsDarkMode = darkMode.Value;
                }
                
                Directory.CreateDirectory(_profileDir);
                var data = new ProfileData { Name = name, IsDarkMode = IsDarkMode };
                File.WriteAllText(_profilePath, JsonSerializer.Serialize(data));
            }
            catch (Exception ex)
            {
                Logger.LogError(ex);
            }
        }

        public void SaveDarkMode(bool isDarkMode)
        {
            try
            {
                IsDarkMode = isDarkMode;
                Directory.CreateDirectory(_profileDir);
                var data = new ProfileData { Name = PlayerName, IsDarkMode = isDarkMode };
                File.WriteAllText(_profilePath, JsonSerializer.Serialize(data));
            }
            catch (Exception ex)
            {
                Logger.LogError(ex);
            }
        }

        private class ProfileData
        {
            public string? Name { get; set; }
            public bool IsDarkMode { get; set; }
        }
    }
}
