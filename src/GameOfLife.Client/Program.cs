using GameOfLife.Core;
using GameOfLife.Core.Patterns;
using GameOfLife.Core.Persistence;
using GameOfLife.Core.Protocol;
using GameOfLife.Client.ConsoleUi;
using GameOfLife.Client.Services;
using Microsoft.AspNetCore.SignalR.Client;

var serverUrl = args.FirstOrDefault() ?? "http://localhost:5050";
var patternPath = ResolvePatternPath(args);

Console.WriteLine("Conway's Game of Life — Multiplayer Client");
Console.WriteLine($"Connecting to {serverUrl} ...");

await using var connection = new HubConnectionBuilder()
    .WithUrl($"{serverUrl.TrimEnd('/')}/hubs/life")
    .WithAutomaticReconnect()
    .Build();

var latestSnapshot = (UniverseSnapshotDto?)null;
var snapshotReceived = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

connection.On<UniverseSnapshotDto>(LifeHubMethods.ReceiveSnapshot, snapshot =>
{
    latestSnapshot = snapshot;
    snapshotReceived.TrySetResult();
});

await connection.StartAsync();
await connection.InvokeAsync(LifeHubMethods.RequestSnapshot);
await snapshotReceived.Task.WaitAsync(TimeSpan.FromSeconds(5));

var editor = new ViewportEditor(GameConstants.ViewportWidth, GameConstants.ViewportHeight);
var originX = latestSnapshot?.Viewport.OriginX ?? 0UL;
var originY = latestSnapshot?.Viewport.OriginY ?? 0UL;

if (latestSnapshot is not null)
{
    editor.LoadFromSnapshot(latestSnapshot.Viewport);
}
else if (File.Exists(patternPath))
{
    var (universe, pattern) = await UniversePersistence.LoadAsync(patternPath, new Coordinate(originX, originY));
    editor.LoadFromUniverse(universe, new Coordinate(originX, originY));
    Console.WriteLine($"Loaded pattern '{pattern.Name}' from {patternPath}");
}

var client = new LifeClient(connection);
var mode = AppMode.Edit;
var cursorX = 0;
var cursorY = 0;
var running = true;

RenderScreen(mode, editor, latestSnapshot, cursorX, cursorY, originX, originY, serverUrl);

while (running)
{
    if (!Console.KeyAvailable)
    {
        if (mode == AppMode.Observe && latestSnapshot is not null)
        {
            originX = latestSnapshot.Viewport.OriginX;
            originY = latestSnapshot.Viewport.OriginY;
            editor.LoadFromSnapshot(latestSnapshot.Viewport);
            RenderScreen(mode, editor, latestSnapshot, cursorX, cursorY, originX, originY, serverUrl);
        }

        await Task.Delay(50);
        continue;
    }

    var key = Console.ReadKey(intercept: true);
    switch (key.Key)
    {
        case ConsoleKey.E when mode == AppMode.Observe:
            mode = AppMode.Edit;
            break;
        case ConsoleKey.O when mode == AppMode.Edit:
            mode = AppMode.Observe;
            await connection.InvokeAsync(LifeHubMethods.SetViewport, new SetViewportRequest(originX, originY));
            break;
        case ConsoleKey.UpArrow when mode == AppMode.Edit:
            cursorY = Math.Max(0, cursorY - 1);
            break;
        case ConsoleKey.DownArrow when mode == AppMode.Edit:
            cursorY = Math.Min(GameConstants.ViewportHeight - 1, cursorY + 1);
            break;
        case ConsoleKey.LeftArrow when mode == AppMode.Edit:
            cursorX = Math.Max(0, cursorX - 1);
            break;
        case ConsoleKey.RightArrow when mode == AppMode.Edit:
            cursorX = Math.Min(GameConstants.ViewportWidth - 1, cursorX + 1);
            break;
        case ConsoleKey.Spacebar when mode == AppMode.Edit:
            editor.Toggle(cursorX, cursorY);
            break;
        case ConsoleKey.C when mode == AppMode.Edit:
            editor.Clear();
            break;
        case ConsoleKey.L when mode == AppMode.Edit:
            if (File.Exists(patternPath))
            {
                var (universe, pattern) = await UniversePersistence.LoadAsync(patternPath, new Coordinate(originX, originY));
                editor.LoadFromUniverse(universe, new Coordinate(originX, originY));
                Console.WriteLine($"Loaded '{pattern.Name}'.");
            }
            else
            {
                Console.WriteLine($"Pattern file not found: {patternPath}");
            }

            break;
        case ConsoleKey.S when mode == AppMode.Edit:
            await SavePatternAsync(editor, originX, originY);
            break;
        case ConsoleKey.A when mode == AppMode.Edit:
            await client.ApplyPatternAsync(editor, originX, originY);
            mode = AppMode.Observe;
            break;
        case ConsoleKey.R:
            await connection.InvokeAsync(
                LifeHubMethods.StartSimulation,
                new StartSimulationRequest(GameConstants.DefaultTickMilliseconds));
            mode = AppMode.Observe;
            break;
        case ConsoleKey.P:
            await connection.InvokeAsync(LifeHubMethods.StopSimulation);
            break;
        case ConsoleKey.G:
            originX = unchecked(originX + GameConstants.ViewportWidth);
            await connection.InvokeAsync(LifeHubMethods.SetViewport, new SetViewportRequest(originX, originY));
            break;
        case ConsoleKey.H:
            if (originX >= GameConstants.ViewportWidth)
            {
                originX -= (ulong)GameConstants.ViewportWidth;
            }

            await connection.InvokeAsync(LifeHubMethods.SetViewport, new SetViewportRequest(originX, originY));
            break;
        case ConsoleKey.J:
            originY = unchecked(originY + GameConstants.ViewportHeight);
            await connection.InvokeAsync(LifeHubMethods.SetViewport, new SetViewportRequest(originX, originY));
            break;
        case ConsoleKey.K:
            if (originY >= GameConstants.ViewportHeight)
            {
                originY -= (ulong)GameConstants.ViewportHeight;
            }

            await connection.InvokeAsync(LifeHubMethods.SetViewport, new SetViewportRequest(originX, originY));
            break;
        case ConsoleKey.Q:
            running = false;
            continue;
    }

    RenderScreen(mode, editor, latestSnapshot, cursorX, cursorY, originX, originY, serverUrl);
}

static string ResolvePatternPath(string[] args)
{
    var explicitPath = args.Skip(1).FirstOrDefault();
    if (!string.IsNullOrWhiteSpace(explicitPath))
    {
        return explicitPath;
    }

    var candidates = new[]
    {
        Path.Combine(AppContext.BaseDirectory, "patterns", "gosper_glider_gun.rle"),
        Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "patterns", "gosper_glider_gun.rle")),
        Path.GetFullPath("patterns/gosper_glider_gun.rle"),
    };

    return candidates.FirstOrDefault(File.Exists) ?? candidates[^1];
}

static async Task SavePatternAsync(ViewportEditor editor, ulong originX, ulong originY)
{
    var path = Path.GetFullPath("saved_pattern.rle");
    var universe = editor.ToUniverse(new Coordinate(originX, originY));
    await UniversePersistence.SaveAsync(
        universe,
        path,
        new Coordinate(0, 0),
        GameConstants.ViewportWidth,
        GameConstants.ViewportHeight,
        "saved_pattern");
    Console.WriteLine($"Saved to {path}");
}

static void RenderScreen(
    AppMode mode,
    ViewportEditor editor,
    UniverseSnapshotDto? snapshot,
    int cursorX,
    int cursorY,
    ulong originX,
    ulong originY,
    string serverUrl)
{
    Console.Clear();
    Console.WriteLine("Conway's Game of Life — Multiplayer Client");
    Console.WriteLine($"Server: {serverUrl} | Mode: {mode} | Viewport origin: ({originX}, {originY})");
    if (snapshot is not null)
    {
        Console.WriteLine(
            $"Generation: {snapshot.Generation} | Live cells: {snapshot.LiveCellCount} | Running: {snapshot.IsRunning}");
    }

    Console.WriteLine(new string('-', GameConstants.ViewportWidth + 2));
    for (var row = 0; row < GameConstants.ViewportHeight; row++)
    {
        Console.Write(' ');
        for (var col = 0; col < GameConstants.ViewportWidth; col++)
        {
            var alive = editor.IsAlive(col, row);
            var glyph = alive ? 'O' : '.';
            if (mode == AppMode.Edit && col == cursorX && row == cursorY)
            {
                Console.Write('X');
            }
            else
            {
                Console.Write(glyph);
            }
        }

        Console.WriteLine();
    }

    Console.WriteLine(new string('-', GameConstants.ViewportWidth + 2));
    Console.WriteLine("Edit: arrows move | space toggle | c clear | l load | s save | a apply");
    Console.WriteLine("Sim:  r run | p pause | o observe | e edit | g/h/j/k pan viewport | q quit");
}

enum AppMode
{
    Edit,
    Observe,
}
