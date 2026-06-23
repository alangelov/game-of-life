using GameOfLife.Client.ConsoleUi;
using GameOfLife.Core.Protocol;
using Microsoft.AspNetCore.SignalR.Client;

namespace GameOfLife.Client.Services;

public sealed class LifeClient
{
    private readonly HubConnection _connection;

    public LifeClient(HubConnection connection)
    {
        _connection = connection ?? throw new ArgumentNullException(nameof(connection));
    }

    public Task ApplyPatternAsync(ViewportEditor editor, ulong originX, ulong originY)
    {
        ArgumentNullException.ThrowIfNull(editor);

        var request = new ApplyPatternRequest(
            originX,
            originY,
            editor.ToCellDtos(new Core.Coordinate(originX, originY)),
            editor.Width,
            editor.Height);

        return _connection.InvokeAsync(LifeHubMethods.ApplyPattern, request);
    }
}
