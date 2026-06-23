using GameOfLife.Core;
using GameOfLife.Core.Patterns;
using GameOfLife.Core.Persistence;
using GameOfLife.Core.Protocol;

namespace GameOfLife.Core.Services;

public static class PatternBootstrap
{
    public static async Task TryLoadCenterPatternAsync(
        GameSession session,
        string patternPath,
        CancellationToken cancellationToken = default)
    {
        if (!File.Exists(patternPath))
        {
            return;
        }

        var content = await File.ReadAllTextAsync(patternPath, cancellationToken).ConfigureAwait(false);
        var pattern = RleParser.Parse(content, Path.GetFileNameWithoutExtension(patternPath));
        var center = new Coordinate(1UL << 63, 1UL << 63);
        var placement = center.Offset(-pattern.Width / 2, -pattern.Height / 2);

        var cells = pattern.Cells
            .Select(cell => new CellDto(
                placement.Offset((int)cell.X, (int)cell.Y).X,
                placement.Offset((int)cell.X, (int)cell.Y).Y))
            .ToArray();

        session.ApplyPattern(new ApplyPatternRequest(
            placement.X,
            placement.Y,
            cells,
            pattern.Width,
            pattern.Height));
    }
}
