using System.Collections.Generic;
using System.Threading.Tasks;
using Npgsql;
using KuizServer.Models;

namespace KuizServer.Services;

public class QuestionService
{
    private readonly string _connectionString;

    public QuestionService(IConfiguration configuration)
    {
        // RailwayのPostgreSQL接続文字列を環境変数から取得
        var databaseUrl = Environment.GetEnvironmentVariable("DATABASE_URL");
        
        Console.WriteLine("===========================================");
        Console.WriteLine("?? QuestionService Constructor");
        Console.WriteLine($"   DATABASE_URL env var: {(string.IsNullOrEmpty(databaseUrl) ? "NOT SET" : "SET")}");
        Console.WriteLine($"   DATABASE_URL value: {databaseUrl}");
        Console.WriteLine("===========================================");
        
        if (!string.IsNullOrEmpty(databaseUrl))
        {
            // RailwayのDATABASE_URLフォーマット: postgres://user:password@host:port/database
            // Npgsqlフォーマットに変換: Host=host;Port=port;Database=database;Username=user;Password=password
            try
            {
                var uri = new Uri(databaseUrl);
                var userInfo = uri.UserInfo.Split(':');
                _connectionString = $"Host={uri.Host};Port={uri.Port};Database={uri.LocalPath.TrimStart('/')};Username={userInfo[0]};Password={userInfo[1]};SSL Mode=Require;Trust Server Certificate=true";
                Console.WriteLine($"?? Using Railway DATABASE_URL: Host={uri.Host}, Database={uri.LocalPath.TrimStart('/')}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"?? Failed to parse DATABASE_URL: {ex.Message}");
                _connectionString = configuration.GetConnectionString("DefaultConnection")
                    ?? "Host=localhost;Port=5432;Database=kuiz;Username=postgres;Password=postgres";
            }
        }
        else
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection")
                ?? "Host=localhost;Port=5432;Database=kuiz;Username=postgres;Password=postgres";
            Console.WriteLine("?? Using local database connection");
        }
    }



    public async Task<List<Question>> GetAllQuestionsAsync()
    {
        var questions = new List<Question>();
        
        try
        {
            Console.WriteLine("?? Connecting to database...");
            Console.WriteLine($"   Connection string: {_connectionString.Replace(_connectionString.Split("Password=")[1].Split(";")[0], "***")}");
            
            await using var conn = new NpgsqlConnection(_connectionString);
            await conn.OpenAsync();
            
            Console.WriteLine("? Database connection opened");
            
            await using var cmd = new NpgsqlCommand("SELECT id, text, answer, author, created_at, played_at FROM questions ORDER BY id", conn);
            await using var reader = await cmd.ExecuteReaderAsync();
            
            Console.WriteLine("? Query executed");
            
            while (await reader.ReadAsync())
            {
                questions.Add(new Question
                {
                    Id = reader.GetInt32(0),
                    Text = reader.GetString(1),
                    Answer = reader.GetString(2),
                    Author = reader.IsDBNull(3) ? null : reader.GetString(3),
                    CreatedAt = reader.GetDateTime(4),
                    PlayedAt = reader.IsDBNull(5) ? null : reader.GetDateTime(5)
                });
            }
            
            Console.WriteLine($"? Retrieved {questions.Count} questions from database");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"? Database error: {ex.Message}");
            Console.WriteLine($"   Stack trace: {ex.StackTrace}");
            throw;
        }
        
        return questions;
    }

    public async Task<Question?> GetQuestionByIdAsync(int id)
    {
        await using var conn = new NpgsqlConnection(_connectionString);
        await conn.OpenAsync();
        
        await using var cmd = new NpgsqlCommand("SELECT id, text, answer, author, created_at, played_at FROM questions WHERE id = @id", conn);
        cmd.Parameters.AddWithValue("@id", id);
        
        await using var reader = await cmd.ExecuteReaderAsync();
        
        if (await reader.ReadAsync())
        {
            return new Question
            {
                Id = reader.GetInt32(0),
                Text = reader.GetString(1),
                Answer = reader.GetString(2),
                Author = reader.IsDBNull(3) ? null : reader.GetString(3),
                CreatedAt = reader.GetDateTime(4),
                PlayedAt = reader.IsDBNull(5) ? null : reader.GetDateTime(5)
            };
        }
        
        return null;
    }

    public async Task<Question> CreateQuestionAsync(CreateQuestionDto dto)
    {
        await using var conn = new NpgsqlConnection(_connectionString);
        await conn.OpenAsync();
        
        await using var cmd = new NpgsqlCommand(
            "INSERT INTO questions (text, answer, author, created_at) VALUES (@text, @answer, @author, @created_at) RETURNING id, created_at", 
            conn);
        
        cmd.Parameters.AddWithValue("@text", dto.Text);
        cmd.Parameters.AddWithValue("@answer", dto.Answer);
        cmd.Parameters.AddWithValue("@author", (object?)dto.Author ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@created_at", DateTime.UtcNow);
        
        await using var reader = await cmd.ExecuteReaderAsync();
        await reader.ReadAsync();
        
        return new Question
        {
            Id = reader.GetInt32(0),
            Text = dto.Text,
            Answer = dto.Answer,
            Author = dto.Author,
            CreatedAt = reader.GetDateTime(1)
        };
    }

    public async Task<bool> DeleteQuestionAsync(int id)
    {
        await using var conn = new NpgsqlConnection(_connectionString);
        await conn.OpenAsync();
        
        await using var cmd = new NpgsqlCommand("DELETE FROM questions WHERE id = @id", conn);
        cmd.Parameters.AddWithValue("@id", id);
        
        var rowsAffected = await cmd.ExecuteNonQueryAsync();
        return rowsAffected > 0;
    }

    public async Task<bool> UpdateQuestionAsync(int id, CreateQuestionDto dto)
    {
        await using var conn = new NpgsqlConnection(_connectionString);
        await conn.OpenAsync();
        
        await using var cmd = new NpgsqlCommand(
            "UPDATE questions SET text = @text, answer = @answer, author = @author WHERE id = @id", 
            conn);
        
        cmd.Parameters.AddWithValue("@id", id);
        cmd.Parameters.AddWithValue("@text", dto.Text);
        cmd.Parameters.AddWithValue("@answer", dto.Answer);
        cmd.Parameters.AddWithValue("@author", (object?)dto.Author ?? DBNull.Value);
        
        var rowsAffected = await cmd.ExecuteNonQueryAsync();
        return rowsAffected > 0;
    }

    public async Task<List<Question>> GetRandomQuestionsAsync(int count)
    {
        var questions = new List<Question>();
        
        await using var conn = new NpgsqlConnection(_connectionString);
        await conn.OpenAsync();
        
        await using var cmd = new NpgsqlCommand(
            "SELECT id, text, answer, author, created_at, played_at FROM questions ORDER BY RANDOM() LIMIT @count", 
            conn);
        cmd.Parameters.AddWithValue("@count", count);
        
        await using var reader = await cmd.ExecuteReaderAsync();
        
        while (await reader.ReadAsync())
        {
            questions.Add(new Question
            {
                Id = reader.GetInt32(0),
                Text = reader.GetString(1),
                Answer = reader.GetString(2),
                Author = reader.IsDBNull(3) ? null : reader.GetString(3),
                CreatedAt = reader.GetDateTime(4),
                PlayedAt = reader.IsDBNull(5) ? null : reader.GetDateTime(5)
            });
        }
        
        return questions;
    }

    public async Task InitializeDatabaseAsync()
    {
        try
        {
            Console.WriteLine("?? Initializing database...");
            Console.WriteLine($"?? Connection string: {MaskPassword(_connectionString)}");
            
            await using var conn = new NpgsqlConnection(_connectionString);
            await conn.OpenAsync();
            Console.WriteLine("? Database connection established");
            
            // Create questions table if not exists
            await using var cmd = new NpgsqlCommand(@"
                CREATE TABLE IF NOT EXISTS questions (
                    id SERIAL PRIMARY KEY,
                    text TEXT NOT NULL,
                    answer TEXT NOT NULL,
                    author TEXT,
                    created_at TIMESTAMP NOT NULL DEFAULT NOW(),
                    played_at TIMESTAMP
                )", conn);
            
            await cmd.ExecuteNonQueryAsync();
            Console.WriteLine("? Questions table ready");
            
            // Check question count
            await using var countCmd = new NpgsqlCommand("SELECT COUNT(*) FROM questions", conn);
            var count = Convert.ToInt32(await countCmd.ExecuteScalarAsync());
            Console.WriteLine($"?? Total questions in database: {count}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"? Database initialization error: {ex.Message}");
            Console.WriteLine($"?? Stack trace: {ex.StackTrace}");
            throw;
        }
    }
    
    private string MaskPassword(string connectionString)
    {
        if (string.IsNullOrEmpty(connectionString)) return "";
        
        var parts = connectionString.Split(';');
        var masked = new List<string>();
        
        foreach (var part in parts)
        {
            if (part.StartsWith("Password=", StringComparison.OrdinalIgnoreCase))
            {
                masked.Add("Password=***");
            }
            else
            {
                masked.Add(part);
            }
        }
        
        return string.Join(";", masked);
    }
}


