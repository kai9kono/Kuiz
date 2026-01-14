using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Kuiz.Models;

namespace Kuiz.Services
{
    /// <summary>
    /// Railway API経由で問題を取得・管理
    /// PostgreSQLをローカルにインストール不要
    /// </summary>
    public class QuestionService
    {
        // Railway APIのURL
#if DEBUG
        private const string ApiUrl = "http://localhost:8080/api/question";
#else
        private const string ApiUrl = "https://kuiz-production.up.railway.app/api/question";
#endif

        private static readonly HttpClient _httpClient = new();
        
        public List<Question> Questions { get; private set; } = new();

        public async Task<List<Question>> LoadQuestionsAsync(IProgress<int>? progress = null)
        {
            try
            {
                progress?.Report(10);
                Logger.LogInfo($"Loading questions from {ApiUrl}");
                
                var response = await _httpClient.GetAsync(ApiUrl);
                
                if (!response.IsSuccessStatusCode)
                {
                    var error = await response.Content.ReadAsStringAsync();
                    throw new Exception($"Failed to load questions: {response.StatusCode} - {error}");
                }
                
                progress?.Report(50);
                
                var json = await response.Content.ReadAsStringAsync();
                var questions = JsonSerializer.Deserialize<List<Question>>(json, new JsonSerializerOptions 
                { 
                    PropertyNameCaseInsensitive = true 
                }) ?? new List<Question>();
                
                progress?.Report(90);
                
                Questions = questions;
                Logger.LogInfo($"Loaded {questions.Count} questions from Railway");
                
                progress?.Report(100);
                return questions;
            }
            catch (Exception ex)
            {
                Logger.LogError(ex);
                // フォールバック：空のリストを返す
                Questions = new List<Question>();
                return Questions;
            }
        }

        public async Task<Question?> GetQuestionByIdAsync(int id)
        {
            try
            {
                var response = await _httpClient.GetAsync($"{ApiUrl}/{id}");
                if (!response.IsSuccessStatusCode) return null;
                
                var json = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<Question>(json, new JsonSerializerOptions 
                { 
                    PropertyNameCaseInsensitive = true 
                });
            }
            catch (Exception ex)
            {
                Logger.LogError(ex);
                return null;
            }
        }

        public async Task<List<Question>> GetRandomQuestionsAsync(int count)
        {
            try
            {
                Logger.LogInfo($"?? Getting {count} random questions from {ApiUrl}/random/{count}");
                
                var response = await _httpClient.GetAsync($"{ApiUrl}/random/{count}");
                
                Logger.LogInfo($"?? Response status: {response.StatusCode}");
                
                if (!response.IsSuccessStatusCode)
                {
                    var error = await response.Content.ReadAsStringAsync();
                    Logger.LogError(new Exception($"Failed to get random questions: {response.StatusCode} - {error}"));
                    return new List<Question>();
                }
                
                var json = await response.Content.ReadAsStringAsync();
                Logger.LogInfo($"?? Response JSON length: {json.Length}");
                
                var questions = JsonSerializer.Deserialize<List<Question>>(json, new JsonSerializerOptions 
                { 
                    PropertyNameCaseInsensitive = true 
                }) ?? new List<Question>();
                
                Logger.LogInfo($"? Successfully deserialized {questions.Count} random questions");
                return questions;
            }

            catch (Exception ex)
            {
                Logger.LogError(ex);
                return new List<Question>();
            }
        }

        public async Task<int> AddQuestionAsync(string text, string answer, string? author = null)
        {
            try
            {
                var dto = new { Text = text, Answer = answer, Author = author };
                var json = JsonSerializer.Serialize(dto);
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                
                var response = await _httpClient.PostAsync(ApiUrl, content);
                
                if (response.IsSuccessStatusCode)
                {
                    // リストを再読み込み
                    await LoadQuestionsAsync();
                    return 1; // 成功を示す
                }
                
                return 0;
            }
            catch (Exception ex)
            {
                Logger.LogError(ex);
                return 0;
            }
        }

        public async Task UpdateQuestionAsync(int id, string text, string answer, string? author = null)
        {
            try
            {
                var dto = new { Text = text, Answer = answer, Author = author };
                var json = JsonSerializer.Serialize(dto);
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                
                var response = await _httpClient.PutAsync($"{ApiUrl}/{id}", content);
                
                if (response.IsSuccessStatusCode)
                {
                    await LoadQuestionsAsync();
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex);
                throw;
            }
        }

        public async Task DeleteQuestionAsync(int id)
        {
            try
            {
                var response = await _httpClient.DeleteAsync($"{ApiUrl}/{id}");
                
                if (response.IsSuccessStatusCode)
                {
                    await LoadQuestionsAsync();
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex);
                throw;
            }
        }

        public async Task TestConnectionAsync()
        {
            try
            {
                var response = await _httpClient.GetAsync(ApiUrl.Replace("/api/question", "/health"));
                if (!response.IsSuccessStatusCode)
                {
                    throw new Exception($"Server health check failed: {response.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex);
                throw new Exception($"Cannot connect to Railway server: {ex.Message}", ex);
            }
        }

        public async Task<int> ImportQuestionsAsync(string jsonPath)
        {
            // JSONファイルから読み込んで、APIに一括送信
            try
            {
                var json = await System.IO.File.ReadAllTextAsync(jsonPath);
                var questions = JsonSerializer.Deserialize<List<Question>>(json);
                
                if (questions == null) return 0;
                
                int count = 0;
                foreach (var q in questions)
                {
                    var result = await AddQuestionAsync(q.Text, q.Answer, q.Author);
                    if (result > 0) count++;
                }
                
                return count;
            }
            catch (Exception ex)
            {
                Logger.LogError(ex);
                throw;
            }
        }
    }
}
