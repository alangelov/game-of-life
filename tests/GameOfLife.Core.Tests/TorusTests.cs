using GameOfLife.Core;
using Xunit;

namespace GameOfLife.Core.Tests;

public class TorusTests
{
    [Fact]
    public void Coordinate_WrapsOnUnderflow()
    {
        var cell = new Coordinate(0, 0);
        var neighbor = cell.Offset(-1, -1);
        Assert.Equal(new Coordinate(ulong.MaxValue, ulong.MaxValue), neighbor);
    }

    [Fact]
    public void Coordinate_WrapsOnOverflow()
    {
        var cell = new Coordinate(ulong.MaxValue, ulong.MaxValue);
        var neighbor = cell.Offset(1, 1);
        Assert.Equal(new Coordinate(0, 0), neighbor);
    }

    [Fact]
    public void Blinker_PeriodTwo_OnTorus()
    {
        var engine = new GameEngine();
        engine.Universe.SetAlive(new Coordinate(1, 0), true);
        engine.Universe.SetAlive(new Coordinate(2, 0), true);
        engine.Universe.SetAlive(new Coordinate(3, 0), true);

        engine.Step();
        Assert.Equal(3, engine.Universe.LiveCellCount);
        Assert.True(engine.Universe.IsAlive(new Coordinate(2, ulong.MaxValue)));
        Assert.True(engine.Universe.IsAlive(new Coordinate(2, 1)));
        Assert.True(engine.Universe.IsAlive(new Coordinate(2, 0)));

        engine.Step();
        Assert.True(engine.Universe.IsAlive(new Coordinate(1, 0)));
        Assert.True(engine.Universe.IsAlive(new Coordinate(2, 0)));
        Assert.True(engine.Universe.IsAlive(new Coordinate(3, 0)));
    }
}
