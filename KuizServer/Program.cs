using Microsoft.AspNetCore.SignalR;
using KuizServer.Services;
using KuizServer.Hubs;

var builder = WebApplication.CreateBuilder(args);

// Railwayの動的ポートに対応
var port = Environment.GetEnvironmentVariable("PORT") ?? "8080";
builder.WebHost.UseUrls($"http://0.0.0.0:{port}");

// Add services to the container
builder.Services.AddSignalR();
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Add CORS for client connections
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

// Add singleton services for game management
builder.Services.AddSingleton<LobbyService>();
builder.Services.AddSingleton<GameRoomService>();
builder.Services.AddSingleton<QuestionService>();


var app = builder.Build();

// Initialize database (non-blocking, with error handling)
_ = Task.Run(async () =>
{
    try
    {
        using var scope = app.Services.CreateScope();
        var questionService = scope.ServiceProvider.GetRequiredService<QuestionService>();
        await questionService.InitializeDatabaseAsync();
        Console.WriteLine("? Database initialized successfully");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"?? Database initialization failed: {ex.Message}");
        Console.WriteLine("Application will continue without database");
    }
});

// Configure the HTTP request pipeline


if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Health check endpoint for Railway
app.MapGet("/", () => Results.Ok(new { 
    status = "healthy", 
    service = "KuizServer",
    timestamp = DateTime.UtcNow 
}));

app.MapGet("/health", () => Results.Ok(new { 
    status = "healthy", 
    timestamp = DateTime.UtcNow 
}));

app.UseCors();

app.UseAuthorization();

app.MapControllers();
app.MapHub<GameHub>("/gamehub");

app.Run();

