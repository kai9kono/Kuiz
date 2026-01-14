using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using Kuiz.Models;

namespace Kuiz.Services
{
    /// <summary>
    /// èoëËóöóÇÃï€ë∂ÅEì«Ç›çûÇ›ÇíSìñ
    /// </summary>
    public class QuestionHistoryService
    {
        private static readonly string HistoryFilePath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Kuiz",
            "question_history.json"
        );

        public List<QuestionHistoryEntry> History { get; private set; } = new();

        public async Task LoadAsync()
        {
            try
            {
                if (File.Exists(HistoryFilePath))
                {
                    var json = await File.ReadAllTextAsync(HistoryFilePath);
                    var history = JsonSerializer.Deserialize<List<QuestionHistoryEntry>>(json);
                    if (history != null)
                    {
                        History = history;
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex);
                History = new List<QuestionHistoryEntry>();
            }
        }

        public async Task SaveAsync()
        {
            try
            {
                var dir = Path.GetDirectoryName(HistoryFilePath);
                if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                {
                    Directory.CreateDirectory(dir);
                }

                var json = JsonSerializer.Serialize(History, new JsonSerializerOptions { WriteIndented = true });
                await File.WriteAllTextAsync(HistoryFilePath, json);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex);
            }
        }

        public async Task AddEntryAsync(Question question)
        {
            var entry = new QuestionHistoryEntry
            {
                QuestionId = question.Id,
                Text = question.Text,
                Answer = question.Answer,
                Author = question.Author,
                PlayedAt = DateTime.Now
            };

            History.Insert(0, entry); // ç≈êVÇêÊì™Ç…
            await SaveAsync();
        }

        public async Task ClearHistoryAsync()
        {
            History.Clear();
            await SaveAsync();
        }
    }
}
