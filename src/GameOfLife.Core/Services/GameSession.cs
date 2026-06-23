using GameOfLife.Core;
using GameOfLife.Core.Protocol;

namespace GameOfLife.Core.Services;

public sealed class GameSession : IDisposable
{
    private readonly object _gate = new();
    private readonly GameEngine _engine = new();
    private CancellationTokenSource? _simulationCts;
    private Task? _simulationTask;
    private Coordinate _viewportOrigin;
    private bool _isRunning;

    public GameSession()
    {
        _viewportOrigin = new Coordinate(0, 0);
    }

    public event Action<UniverseSnapshotDto>? SnapshotChanged;

    public UniverseSnapshotDto GetSnapshot()
    {
        lock (_gate)
        {
            return BuildSnapshot();
        }
    }

    public void ApplyPattern(ApplyPatternRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        lock (_gate)
        {
            StopSimulationInternal();

            _engine.Universe.Clear();
            foreach (var cell in request.Cells)
            {
                _engine.Universe.SetAlive(new Coordinate(cell.X, cell.Y), true);
            }

            _viewportOrigin = new Coordinate(request.OriginX, request.OriginY);
            _engine.Reset(_engine.Universe, generation: 0);
            PublishSnapshot();
        }
    }

    public void ClearUniverse()
    {
        lock (_gate)
        {
            StopSimulationInternal();
            _engine.Universe.Clear();
            _engine.Reset(_engine.Universe, generation: 0);
            PublishSnapshot();
        }
    }

    public void SetViewport(SetViewportRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        lock (_gate)
        {
            _viewportOrigin = new Coordinate(request.OriginX, request.OriginY);
            PublishSnapshot();
        }
    }

    public void StartSimulation(int tickMilliseconds)
    {
        if (tickMilliseconds <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(tickMilliseconds), "Tick interval must be positive.");
        }

        lock (_gate)
        {
            if (_isRunning)
            {
                return;
            }

            _isRunning = true;
            _simulationCts = new CancellationTokenSource();
            var token = _simulationCts.Token;
            _simulationTask = Task.Run(() => RunSimulationAsync(tickMilliseconds, token), token);
            PublishSnapshot();
        }
    }

    public void StopSimulation()
    {
        lock (_gate)
        {
            StopSimulationInternal();
            PublishSnapshot();
        }
    }

    private async Task RunSimulationAsync(int tickMilliseconds, CancellationToken cancellationToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromMilliseconds(tickMilliseconds));

        try
        {
            while (await timer.WaitForNextTickAsync(cancellationToken).ConfigureAwait(false))
            {
                lock (_gate)
                {
                    _engine.Step();
                    PublishSnapshot();
                }
            }
        }
        catch (OperationCanceledException)
        {
            // Expected when simulation stops.
        }
    }

    private void StopSimulationInternal()
    {
        if (!_isRunning)
        {
            return;
        }

        _simulationCts?.Cancel();
        _simulationTask?.Wait(TimeSpan.FromSeconds(2));
        _simulationCts?.Dispose();
        _simulationCts = null;
        _simulationTask = null;
        _isRunning = false;
    }

    private UniverseSnapshotDto BuildSnapshot()
    {
        var viewport = _engine.Universe.RenderViewport(
            _viewportOrigin,
            GameConstants.ViewportWidth,
            GameConstants.ViewportHeight);

        var flat = new bool[GameConstants.ViewportWidth * GameConstants.ViewportHeight];
        for (var row = 0; row < GameConstants.ViewportHeight; row++)
        {
            for (var col = 0; col < GameConstants.ViewportWidth; col++)
            {
                flat[row * GameConstants.ViewportWidth + col] = viewport[col, row];
            }
        }

        return new UniverseSnapshotDto(
            _engine.Generation,
            _engine.Universe.LiveCellCount,
            new ViewportDto(
                _viewportOrigin.X,
                _viewportOrigin.Y,
                GameConstants.ViewportWidth,
                GameConstants.ViewportHeight,
                flat),
            _isRunning);
    }

    private void PublishSnapshot()
    {
        SnapshotChanged?.Invoke(BuildSnapshot());
    }

    public void Dispose()
    {
        lock (_gate)
        {
            StopSimulationInternal();
        }
    }
}
