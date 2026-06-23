using GameOfLife.Core.Protocol;
using GameOfLife.Core.Services;
using GameOfLife.Server.Hubs;
using Microsoft.AspNetCore.SignalR;

namespace GameOfLife.Server.Services;

public sealed class GameBroadcastService : IHostedService
{
    private readonly GameSession _session;
    private readonly IHubContext<LifeHub> _hubContext;

    public GameBroadcastService(GameSession session, IHubContext<LifeHub> hubContext)
    {
        _session = session;
        _hubContext = hubContext;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _session.SnapshotChanged += OnSnapshotChanged;
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _session.SnapshotChanged -= OnSnapshotChanged;
        return Task.CompletedTask;
    }

    public void RegisterClient(string connectionId)
    {
        // Reserved for future per-client viewport subscriptions.
        _ = connectionId;
    }

    public void UnregisterClient(string connectionId)
    {
        _ = connectionId;
    }

    private void OnSnapshotChanged(UniverseSnapshotDto snapshot) =>
        _ = _hubContext.Clients.All.SendAsync(LifeHubMethods.ReceiveSnapshot, snapshot);
}
