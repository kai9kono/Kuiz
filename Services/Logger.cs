using System;
using System.IO;
using System.Text;

namespace Kuiz
{
    /// <summary>
    /// アプリケーションのログ出力を担当
    /// MSIX対応: LocalApplicationDataに保存
    /// </summary>
    internal static class Logger
    {
        private static readonly object _lock = new();
        private static readonly string _dir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), 
            "Kuiz", 
            "logs");
        private static readonly string _file = Path.Combine(_dir, $"kuiz_{DateTime.Now:yyyyMMdd}.log");


        static Logger()
        {
            try
            {
                if (!Directory.Exists(_dir))
                    Directory.CreateDirectory(_dir);
            }
            catch { }
        }

        public static void LogInfo(string message)
        {
            Write("INFO", message);
        }

        public static void LogError(Exception ex)
        {
            if (ex == null) return;
            Write("ERROR", ex.ToString());
        }

        public static void LogError(string message)
        {
            Write("ERROR", message);
        }
        
        public static string GetLogFilePath()
        {
            return _file;
        }
        
        public static string GetLogDirectory()
        {
            return _dir;
        }

        private static void Write(string level, string message)
        {
            try
            {
                var line = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}][{level}] {message}{Environment.NewLine}";
                lock (_lock)
                {
                    File.AppendAllText(_file, line, Encoding.UTF8);
                }
            }
            catch
            {
                // swallow - logging should not crash app
            }
        }
    }
}
