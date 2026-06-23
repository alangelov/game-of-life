using GameOfLife.Core.Protocol;
using GameOfLife.Core.Services;
using GameOfLife.Server.Hubs;
using GameOfLife.Server.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton<GameSession>();
builder.Services.AddSingleton<GameBroadcastService>();
builder.Services.AddHostedService(sp => sp.GetRequiredService<GameBroadcastService>());
builder.Services.AddSignalR();

var app = builder.Build();

var session = app.Services.GetRequiredService<GameSession>();
var patternPath = ResolvePatternPath(app.Environment.ContentRootPath);
await PatternBootstrap.TryLoadCenterPatternAsync(session, patternPath);

app.MapGet("/health", () => Results.Ok(new { status = "ok" }));
app.MapHub<LifeHub>("/hubs/life");

app.Run();

static string ResolvePatternPath(string contentRoot)
{
    var candidates = new[]
    {
        Path.Combine(contentRoot, "patterns", "gosper_glider_gun.rle"),
        Path.GetFullPath(Path.Combine(contentRoot, "..", "..", "patterns", "gosper_glider_gun.rle")),
        Path.GetFullPath("patterns/gosper_glider_gun.rle"),
    };

    return candidates.FirstOrDefault(File.Exists) ?? candidates[0];
}
