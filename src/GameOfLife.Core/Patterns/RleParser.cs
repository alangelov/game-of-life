namespace GameOfLife.Core.Patterns;

public sealed record RlePattern(
    string Name,
    IReadOnlyList<Coordinate> Cells,
    int Width,
    int Height);

public static class RleParser
{
    public static RlePattern Parse(string content, string? name = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(content);

        var lines = content
            .Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(line => !line.StartsWith('#'))
            .ToArray();

        if (lines.Length == 0)
        {
            throw new FormatException("RLE content is empty.");
        }

        var header = lines[0];
        var headerParts = header.Split(',', StringSplitOptions.TrimEntries);
        if (headerParts.Length < 2)
        {
            throw new FormatException($"Invalid RLE header: '{header}'.");
        }

        var width = 0;
        var height = 0;
        foreach (var part in headerParts)
        {
            var trimmed = part.Trim();
            if (trimmed.StartsWith("x", StringComparison.OrdinalIgnoreCase))
            {
                if (!TryParseDimension(trimmed, out width))
                {
                    throw new FormatException($"Invalid RLE width: '{part}'.");
                }
            }
            else if (trimmed.StartsWith("y", StringComparison.OrdinalIgnoreCase))
            {
                if (!TryParseDimension(trimmed, out height))
                {
                    throw new FormatException($"Invalid RLE height: '{part}'.");
                }
            }
        }

        if (width <= 0 || height <= 0)
        {
            throw new FormatException($"Invalid RLE dimensions in header: '{header}'.");
        }

        var patternName = name ?? ExtractPatternName(headerParts);

        var runData = string.Concat(lines.Skip(1)).TrimEnd('!');
        var cells = new List<Coordinate>();

        var x = 0;
        var y = 0;
        var repeat = 0;

        foreach (var ch in runData)
        {
            if (char.IsDigit(ch))
            {
                repeat = repeat * 10 + (ch - '0');
                continue;
            }

            var count = repeat > 0 ? repeat : 1;
            repeat = 0;

            switch (ch)
            {
                case 'b':
                    x += count;
                    break;
                case 'o':
                    for (var i = 0; i < count; i++)
                    {
                        cells.Add(new Coordinate((ulong)x, (ulong)y));
                        x++;
                    }

                    break;
                case '$':
                    y += count;
                    x = 0;
                    break;
                default:
                    throw new FormatException($"Unexpected RLE character '{ch}'.");
            }
        }

        return new RlePattern(patternName, cells, width, height);
    }

    public static string Serialize(RlePattern pattern)
    {
        ArgumentNullException.ThrowIfNull(pattern);

        if (pattern.Cells.Count == 0)
        {
            return string.IsNullOrWhiteSpace(pattern.Name) || pattern.Name == "pattern"
                ? $"x = {pattern.Width}, y = {pattern.Height}, rule = B3/S23\n!"
                : $"x = {pattern.Width}, y = {pattern.Height}, rule = B3/S23, name = {pattern.Name}\n!";
        }

        var rows = pattern.Cells
            .GroupBy(cell => cell.Y)
            .OrderBy(group => group.Key)
            .ToDictionary(
                group => group.Key,
                group => group.Select(cell => cell.X).OrderBy(x => x).ToArray());

        var maxY = rows.Keys.Max();
        var builder = new System.Text.StringBuilder();
        if (string.IsNullOrWhiteSpace(pattern.Name) || pattern.Name == "pattern")
        {
            builder.AppendLine($"x = {pattern.Width}, y = {pattern.Height}, rule = B3/S23");
        }
        else
        {
            builder.AppendLine($"x = {pattern.Width}, y = {pattern.Height}, rule = B3/S23, name = {pattern.Name}");
        }

        for (ulong y = 0; y <= maxY; y++)
        {
            if (!rows.TryGetValue(y, out var liveColumns))
            {
                builder.Append('$');
                continue;
            }

            var x = 0UL;
            foreach (var liveX in liveColumns)
            {
                if (liveX > x)
                {
                    AppendRun(builder, liveX - x, 'b');
                    x = liveX;
                }

                AppendRun(builder, 1, 'o');
                x++;
            }

            if (y < maxY)
            {
                builder.Append('$');
            }
        }

        builder.Append('!');
        return builder.ToString();
    }

    private static void AppendRun(System.Text.StringBuilder builder, ulong count, char symbol)
    {
        if (count == 0)
        {
            return;
        }

        if (count > 1)
        {
            builder.Append(count);
        }

        builder.Append(symbol);
    }

    private static bool TryParseDimension(string token, out int value)
    {
        token = token.Trim();
        var equalsIndex = token.IndexOf('=');
        if (equalsIndex >= 0)
        {
            token = token[(equalsIndex + 1)..].Trim();
        }

        return int.TryParse(token, out value);
    }

    private static string ExtractPatternName(IReadOnlyList<string> headerParts)
    {
        foreach (var part in headerParts)
        {
            if (part.StartsWith("name = ", StringComparison.OrdinalIgnoreCase))
            {
                return part["name = ".Length..].Trim();
            }
        }

        return "pattern";
    }
}
