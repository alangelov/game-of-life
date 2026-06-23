using GameOfLife.Core.Patterns;

namespace GameOfLife.Core.Persistence;

public static class UniversePersistence
{
    public static async Task SaveAsync(
        SparseUniverse universe,
        string path,
        Coordinate anchor,
        int width,
        int height,
        string patternName,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(universe);
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        ArgumentException.ThrowIfNullOrWhiteSpace(patternName);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(width);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(height);

        var cells = new List<Coordinate>();
        for (var row = 0; row < height; row++)
        {
            for (var col = 0; col < width; col++)
            {
                var coordinate = anchor.Offset(col, row);
                if (universe.IsAlive(coordinate))
                {
                    cells.Add(new Coordinate((ulong)col, (ulong)row));
                }
            }
        }

        var pattern = new RlePattern(patternName, cells, width, height);
        var content = RleParser.Serialize(pattern);
        await File.WriteAllTextAsync(path, content, cancellationToken).ConfigureAwait(false);
    }

    public static async Task<(SparseUniverse Universe, RlePattern Pattern)> LoadAsync(
        string path,
        Coordinate placementOrigin,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        var content = await File.ReadAllTextAsync(path, cancellationToken).ConfigureAwait(false);
        var pattern = RleParser.Parse(content);
        if (pattern.Name == "pattern")
        {
            pattern = pattern with { Name = Path.GetFileNameWithoutExtension(path) };
        }

        var universe = new SparseUniverse();
        foreach (var cell in pattern.Cells)
        {
            universe.SetAlive(placementOrigin.Offset((int)cell.X, (int)cell.Y), true);
        }

        return (universe, pattern);
    }
}
