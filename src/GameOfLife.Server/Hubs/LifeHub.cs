using GameOfLife.Core.Protocol;
using GameOfLife.Core.Services;
using GameOfLife.Server.Services;
using Microsoft.AspNetCore.SignalR;

namespace GameOfLife.Server.Hubs;

public sealed class LifeHub : Hub
{
    private readonly GameSession _session;
    private readonly GameBroadcastService _broadcastService;

    public LifeHub(GameSession session, GameBroadcastService broadcastService)
    {
        _session = session;
        _broadcastService = broadcastService;
    }

    public override Task OnConnectedAsync()
    {
        _broadcastService.RegisterClient(Context.ConnectionId);
        return Clients.Caller.SendAsync(LifeHubMethods.ReceiveSnapshot, _session.GetSnapshot());
    }

    public override Task OnDisconnectedAsync(Exception? exception)
    {
        _broadcastService.UnregisterClient(Context.ConnectionId);
        return Task.CompletedTask;
    }

    public Task RequestSnapshot() =>
        Clients.Caller.SendAsync(LifeHubMethods.ReceiveSnapshot, _session.GetSnapshot());

    public Task ApplyPattern(ApplyPatternRequest request)
    {
        _session.ApplyPattern(request);
        return Task.CompletedTask;
    }

    public Task ClearUniverse()
    {
        _session.ClearUniverse();
        return Task.CompletedTask;
    }

    public Task SetViewport(SetViewportRequest request)
    {
        _session.SetViewport(request);
        return Task.CompletedTask;
    }

    public Task StartSimulation(StartSimulationRequest request)
    {
        _session.StartSimulation(request.TickMilliseconds);
        return Task.CompletedTask;
    }

    public Task StopSimulation()
    {
        _session.StopSimulation();
        return Task.CompletedTask;
    }
}
