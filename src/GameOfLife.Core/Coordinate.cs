namespace GameOfLife.Core;

/// <summary>
/// A cell coordinate on a 2^64 × 2^64 toroidal universe.
/// Unsigned arithmetic provides natural wrap-around on overflow.
/// </summary>
public readonly record struct Coordinate(ulong X, ulong Y) : IComparable<Coordinate>, IComparable
{
    private static readonly (int Dx, int Dy)[] NeighborOffsets =
    [
        (-1, -1), (0, -1), (1, -1),
        (-1,  0),          (1,  0),
        (-1,  1), (0,  1), (1,  1),
    ];

    public Coordinate Offset(int dx, int dy) =>
        new(AddSigned(X, dx), AddSigned(Y, dy));

    public IEnumerable<Coordinate> Neighbors()
    {
        foreach (var (dx, dy) in NeighborOffsets)
        {
            yield return Offset(dx, dy);
        }
    }

    private static ulong AddSigned(ulong value, int delta) =>
        unchecked((ulong)((long)value + delta));

    public int CompareTo(Coordinate other)
    {
        var x = X.CompareTo(other.X);
        return x != 0 ? x : Y.CompareTo(other.Y);
    }

    public int CompareTo(object? obj) =>
        obj is Coordinate other ? CompareTo(other) : throw new ArgumentException("Expected Coordinate.", nameof(obj));
}
