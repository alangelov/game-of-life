using GameOfLife.Core;
using Xunit;
using GameOfLife.Core.Patterns;

namespace GameOfLife.Core.Tests;

public class ConwayRulesTests
{
    [Theory]
    [InlineData(true, 2, true)]
    [InlineData(true, 3, true)]
    [InlineData(true, 1, false)]
    [InlineData(true, 4, false)]
    [InlineData(false, 3, true)]
    [InlineData(false, 2, false)]
    public void NextState_FollowsConwayRules(bool alive, int neighbors, bool expected) =>
        Assert.Equal(expected, ConwayRules.NextState(alive, neighbors));
}
