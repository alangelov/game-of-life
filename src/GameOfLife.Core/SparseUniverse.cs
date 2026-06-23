namespace GameOfLife.Core;

/// <summary>
/// Sparse representation of live cells on a toroidal 2^64 × 2^64 universe.
/// </summary>
public sealed class SparseUniverse
{
    private readonly HashSet<Coordinate> _liveCells = [];

    public int LiveCellCount => _liveCells.Count;

    public bool IsAlive(Coordinate coordinate) => _liveCells.Contains(coordinate);

    public void SetAlive(Coordinate coordinate, bool alive)
    {
        if (alive)
        {
            _liveCells.Add(coordinate);
        }
        else
        {
            _liveCells.Remove(coordinate);
        }
    }

    public void Clear() => _liveCells.Clear();

    public void ReplaceWith(IEnumerable<Coordinate> cells)
    {
        _liveCells.Clear();
        foreach (var cell in cells)
        {
            _liveCells.Add(cell);
        }
    }

    public IReadOnlyCollection<Coordinate> GetLiveCells() => _liveCells;

    public bool[,] RenderViewport(Coordinate origin, int width, int height)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(width);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(height);

        var viewport = new bool[width, height];
        for (var row = 0; row < height; row++)
        {
            for (var col = 0; col < width; col++)
            {
                var coordinate = origin.Offset(col, row);
                viewport[col, row] = IsAlive(coordinate);
            }
        }

        return viewport;
    }

    public SparseUniverse Clone()
    {
        var clone = new SparseUniverse();
        foreach (var cell in _liveCells)
        {
            clone._liveCells.Add(cell);
        }

        return clone;
    }
}
