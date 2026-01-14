using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using Npgsql;
using Kuiz.Models;

namespace Kuiz.Services
{
    /// <summary>
    /// 問題のデータベース操作を担当
    /// </summary>
    public class QuestionService
    {
        public const string DefaultConnectionString = "Host=127.0.0.1;Username=postgres;Password=postgres;Database=kuiz";

        public List<Question> Questions { get; private set; } = new();

        public async Task<List<Question>> LoadQuestionsAsync(IProgress<int>? progress = null)
        {
            var list = new List<Question>();

            try
            {
                await using var connection = new NpgsqlConnection(DefaultConnectionString);
                await connection.OpenAsync();

                await EnsureTableExistsAsync(connection);

                int total = await GetQuestionCountAsync(connection);

                if (total == 0)
                {
                    await InsertDummyQuestionsAsync(connection, progress);
                    total = 100;
                }

                list = await ReadQuestionsAsync(connection, total, progress);
                Questions = list;
            }
            catch (Exception ex)
            {
                Logger.LogError(ex);
                throw;
            }

            return list;
        }

        public async Task<int> ImportQuestionsAsync(string jsonPath)
        {
            var text = await File.ReadAllTextAsync(jsonPath);
            var items = JsonSerializer.Deserialize<List<QuestionImportDto>>(text);
            
            if (items == null || items.Count == 0)
                return 0;

            await using var connection = new NpgsqlConnection(DefaultConnectionString);
            await connection.OpenAsync();
            await using var transaction = await connection.BeginTransactionAsync();

            foreach (var item in items)
            {
                var cmd = new NpgsqlCommand(
                    "INSERT INTO questions (text, answer, author) VALUES (@t, @a, @au)",
                    connection,
                    (NpgsqlTransaction)transaction);
                cmd.Parameters.AddWithValue("@t", item.Text ?? "");
                cmd.Parameters.AddWithValue("@a", item.Answer ?? "");
                cmd.Parameters.AddWithValue("@au", item.Author ?? "");
                await cmd.ExecuteNonQueryAsync();
            }

            await transaction.CommitAsync();
            return items.Count;
        }

        public async Task<int> AddQuestionAsync(string text, string answer, string author)
        {
            await using var connection = new NpgsqlConnection(DefaultConnectionString);
            await connection.OpenAsync();
            
            var cmd = new NpgsqlCommand(
                "INSERT INTO questions (text, answer, author) VALUES (@t, @a, @au) RETURNING id",
                connection);
            cmd.Parameters.AddWithValue("@t", text);
            cmd.Parameters.AddWithValue("@a", answer);
            cmd.Parameters.AddWithValue("@au", author);
            
            var result = await cmd.ExecuteScalarAsync();
            return result != null ? Convert.ToInt32(result) : 0;
        }

        public async Task UpdateQuestionAsync(int id, string text, string answer, string author)
        {
            await using var connection = new NpgsqlConnection(DefaultConnectionString);
            await connection.OpenAsync();
            
            var cmd = new NpgsqlCommand(
                "UPDATE questions SET text=@t, answer=@a, author=@au WHERE id=@id",
                connection);
            cmd.Parameters.AddWithValue("@t", text);
            cmd.Parameters.AddWithValue("@a", answer);
            cmd.Parameters.AddWithValue("@au", author);
            cmd.Parameters.AddWithValue("@id", id);
            
            await cmd.ExecuteNonQueryAsync();
        }

        public async Task DeleteQuestionAsync(int id)
        {
            await using var connection = new NpgsqlConnection(DefaultConnectionString);
            await connection.OpenAsync();
            
            var cmd = new NpgsqlCommand("DELETE FROM questions WHERE id=@id", connection);
            cmd.Parameters.AddWithValue("@id", id);
            
            await cmd.ExecuteNonQueryAsync();
        }

        public async Task TestConnectionAsync()
        {
            await using var connection = new NpgsqlConnection(DefaultConnectionString);
            await connection.OpenAsync();
            await using var cmd = new NpgsqlCommand("SELECT version()", connection);
            await cmd.ExecuteScalarAsync();
        }

        private async Task EnsureTableExistsAsync(NpgsqlConnection connection)
        {
            // テーブル作成
            const string createSql = @"
                CREATE TABLE IF NOT EXISTS questions (
                    id serial primary key,
                    text text not null,
                    answer text not null,
                    author text not null default ''
                );";

            await using var cmd = new NpgsqlCommand(createSql, connection);
            await cmd.ExecuteNonQueryAsync();
            
            // authorカラムが存在しない場合は追加
            const string addColumnSql = @"
                DO $$
                BEGIN
                    IF NOT EXISTS (
                        SELECT 1 FROM information_schema.columns 
                        WHERE table_name='questions' AND column_name='author'
                    ) THEN
                        ALTER TABLE questions ADD COLUMN author text NOT NULL DEFAULT '';
                    END IF;
                END $$;";
            
            await using var cmd2 = new NpgsqlCommand(addColumnSql, connection);
            await cmd2.ExecuteNonQueryAsync();
        }

        private async Task<int> GetQuestionCountAsync(NpgsqlConnection connection)
        {
            await using var cmd = new NpgsqlCommand("SELECT count(*) FROM questions", connection);
            var result = await cmd.ExecuteScalarAsync();
            return result != null ? Convert.ToInt32(result) : 0;
        }

        private async Task InsertDummyQuestionsAsync(NpgsqlConnection connection, IProgress<int>? progress)
        {
            await using var transaction = await connection.BeginTransactionAsync();

            for (int i = 1; i <= 100; i++)
            {
                var text = $"ダミー問題 {i}: これはサンプルの問題文です。";
                var answer = $"答え{i}";

                await using var cmd = new NpgsqlCommand(
                    "INSERT INTO questions (text, answer, author) VALUES (@t, @a, @au)",
                    connection,
                    (NpgsqlTransaction)transaction);
                cmd.Parameters.AddWithValue("@t", text);
                cmd.Parameters.AddWithValue("@a", answer);
                cmd.Parameters.AddWithValue("@au", "System");
                await cmd.ExecuteNonQueryAsync();

                if (i % 5 == 0)
                {
                    progress?.Report(i);
                }
            }

            await transaction.CommitAsync();
        }

        private async Task<List<Question>> ReadQuestionsAsync(NpgsqlConnection connection, int total, IProgress<int>? progress)
        {
            var list = new List<Question>();

            await using var cmd = new NpgsqlCommand("SELECT id, text, answer, COALESCE(author, '') as author FROM questions ORDER BY id", connection);
            await using var reader = await cmd.ExecuteReaderAsync();

            int index = 0;
            while (await reader.ReadAsync())
            {
                index++;
                var question = new Question
                {
                    Id = reader.GetInt32(0),
                    Text = reader.GetString(1),
                    Answer = reader.GetString(2),
                    Author = reader.GetString(3)
                };
                list.Add(question);

                if (total > 0)
                {
                    var percent = (int)((index / (double)total) * 100);
                    progress?.Report(percent);
                }
            }

            return list;
        }
    }
}
