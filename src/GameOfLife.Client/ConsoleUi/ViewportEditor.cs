using GameOfLife.Core;
using GameOfLife.Core.Protocol;

namespace GameOfLife.Client.ConsoleUi;

public sealed class ViewportEditor
{
    private readonly bool[,] _cells;

    public ViewportEditor(int width, int height)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(width);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(height);
        _cells = new bool[width, height];
    }

    public int Width => _cells.GetLength(0);
    public int Height => _cells.GetLength(1);

    public bool IsAlive(int x, int y) => _cells[x, y];

    public void Toggle(int x, int y) => _cells[x, y] = !_cells[x, y];

    public void Clear()
    {
        Array.Clear(_cells, 0, _cells.Length);
    }

    public void LoadFromUniverse(SparseUniverse universe, Coordinate origin)
    {
        ArgumentNullException.ThrowIfNull(universe);
        Clear();

        for (var row = 0; row < Height; row++)
        {
            for (var col = 0; col < Width; col++)
            {
                _cells[col, row] = universe.IsAlive(origin.Offset(col, row));
            }
        }
    }

    public void LoadFromSnapshot(ViewportDto viewport)
    {
        ArgumentNullException.ThrowIfNull(viewport);
        if (viewport.Width != Width || viewport.Height != Height)
        {
            throw new ArgumentException("Viewport dimensions do not match editor.");
        }

        for (var row = 0; row < Height; row++)
        {
            for (var col = 0; col < Width; col++)
            {
                _cells[col, row] = viewport.Cells[row * Width + col];
            }
        }
    }

    public SparseUniverse ToUniverse(Coordinate origin)
    {
        var universe = new SparseUniverse();
        for (var row = 0; row < Height; row++)
        {
            for (var col = 0; col < Width; col++)
            {
                if (_cells[col, row])
                {
                    universe.SetAlive(origin.Offset(col, row), true);
                }
            }
        }

        return universe;
    }

    public IReadOnlyList<CellDto> ToCellDtos(Coordinate origin)
    {
        var cells = new List<CellDto>();
        for (var row = 0; row < Height; row++)
        {
            for (var col = 0; col < Width; col++)
            {
                if (_cells[col, row])
                {
                    var coordinate = origin.Offset(col, row);
                    cells.Add(new CellDto(coordinate.X, coordinate.Y));
                }
            }
        }

        return cells;
    }
}
