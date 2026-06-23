using GameOfLife.Core;
using GameOfLife.Core.Patterns;
using GameOfLife.Core.Persistence;
using Xunit;

namespace GameOfLife.Core.Tests;

public class RleTests
{
    [Fact]
    public void Parse_GosperGliderGun_ProducesLiveCells()
    {
        var path = LocatePattern("gosper_glider_gun.rle");
        var content = File.ReadAllText(path);
        var pattern = RleParser.Parse(content);

        Assert.True(pattern.Cells.Count > 30);
        Assert.Equal(36, pattern.Width);
        Assert.Equal(9, pattern.Height);
    }

    [Fact]
    public async Task SaveAndLoad_RoundTrip_PreservesCells()
    {
        var path = LocatePattern("gosper_glider_gun.rle");
        var (universe, _) = await UniversePersistence.LoadAsync(path, new Core.Coordinate(10, 20));

        var tempFile = Path.Combine(Path.GetTempPath(), $"life-test-{Guid.NewGuid():N}.rle");
        await UniversePersistence.SaveAsync(
            universe,
            tempFile,
            new Core.Coordinate(10, 20),
            100,
            100,
            "roundtrip");

        var (loaded, pattern) = await UniversePersistence.LoadAsync(tempFile, new Core.Coordinate(10, 20));
        Assert.Equal("roundtrip", pattern.Name);
        Assert.Equal(universe.LiveCellCount, loaded.LiveCellCount);

        foreach (var cell in universe.GetLiveCells())
        {
            Assert.True(loaded.IsAlive(cell));
        }

        File.Delete(tempFile);
    }

    private static string LocatePattern(string fileName)
    {
        var candidates = new[]
        {
            Path.Combine(AppContext.BaseDirectory, "patterns", fileName),
            Path.GetFullPath(Path.Combine("patterns", fileName)),
            Path.GetFullPath(Path.Combine("..", "..", "..", "..", "patterns", fileName)),
        };

        foreach (var candidate in candidates)
        {
            if (File.Exists(candidate))
            {
                return candidate;
            }
        }

        throw new FileNotFoundException($"Pattern file '{fileName}' was not found.", fileName);
    }
}
