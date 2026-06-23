namespace GameOfLife.Core;

public sealed class GameEngine
{
    public ulong Generation { get; private set; }

    public SparseUniverse Universe { get; }

    public GameEngine(SparseUniverse? universe = null)
    {
        Universe = universe ?? new SparseUniverse();
    }

    public void Reset(SparseUniverse universe, ulong generation = 0)
    {
        ArgumentNullException.ThrowIfNull(universe);
        Universe.ReplaceWith(universe.GetLiveCells());
        Generation = generation;
    }

    public void Step()
    {
        var neighborCounts = CountNeighbors();
        var nextGeneration = new HashSet<Coordinate>();

        foreach (var (coordinate, count) in neighborCounts)
        {
            var isAlive = Universe.IsAlive(coordinate);
            if (ConwayRules.NextState(isAlive, count))
            {
                nextGeneration.Add(coordinate);
            }
        }

        Universe.ReplaceWith(nextGeneration);
        Generation++;
    }

    private Dictionary<Coordinate, int> CountNeighbors()
    {
        var counts = new Dictionary<Coordinate, int>();

        foreach (var liveCell in Universe.GetLiveCells())
        {
            foreach (var neighbor in liveCell.Neighbors())
            {
                counts.TryGetValue(neighbor, out var count);
                counts[neighbor] = count + 1;
            }
        }

        return counts;
    }
}
