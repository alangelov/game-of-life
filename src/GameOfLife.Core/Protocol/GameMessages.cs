namespace GameOfLife.Core.Protocol;

public static class GameConstants
{
    public const int ViewportWidth = 100;
    public const int ViewportHeight = 100;
    public const int DefaultTickMilliseconds = 100;
}

public sealed record CellDto(ulong X, ulong Y);

public sealed record ViewportDto(
    ulong OriginX,
    ulong OriginY,
    int Width,
    int Height,
    bool[] Cells);

public sealed record UniverseSnapshotDto(
    ulong Generation,
    long LiveCellCount,
    ViewportDto Viewport,
    bool IsRunning);

public sealed record ApplyPatternRequest(
    ulong OriginX,
    ulong OriginY,
    IReadOnlyList<CellDto> Cells,
    int PatternWidth,
    int PatternHeight);

public sealed record SetViewportRequest(ulong OriginX, ulong OriginY);

public sealed record StartSimulationRequest(int TickMilliseconds);

public static class LifeHubMethods
{
    public const string ReceiveSnapshot = "ReceiveSnapshot";
    public const string ApplyPattern = "ApplyPattern";
    public const string ClearUniverse = "ClearUniverse";
    public const string SetViewport = "SetViewport";
    public const string StartSimulation = "StartSimulation";
    public const string StopSimulation = "StopSimulation";
    public const string RequestSnapshot = "RequestSnapshot";
}
