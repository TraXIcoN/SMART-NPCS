using VoxelAgentNexus.Core.Simulation;

namespace VoxelAgentNexus.Simulation;

/// <summary>
/// A uniform spatial hash over the voxel world. Agents are bucketed into cubic
/// cells so neighbor queries and proximity detection scan only nearby cells
/// instead of every agent — the cheap mechanism behind proximity interactions.
/// Rebuilt each simulation tick. (DESIGN_BRIEF.md §5.)
/// </summary>
public sealed class SpatialHashGrid
{
    private readonly int _cellSize;
    private readonly Dictionary<(int X, int Y, int Z), List<(string Id, VoxelPosition Pos)>> _cells = new();

    public SpatialHashGrid(int cellSize)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(cellSize);
        _cellSize = cellSize;
    }

    /// <summary>Remove all agents (call at the start of each rebuild).</summary>
    public void Clear() => _cells.Clear();

    /// <summary>Add an agent at a position.</summary>
    public void Insert(string agentId, VoxelPosition position)
    {
        ArgumentException.ThrowIfNullOrEmpty(agentId);

        var key = CellOf(position);
        if (!_cells.TryGetValue(key, out var bucket))
        {
            bucket = [];
            _cells[key] = bucket;
        }

        bucket.Add((agentId, position));
    }

    /// <summary>Ids of all agents within <paramref name="radius"/> voxels of a point.</summary>
    public IReadOnlyList<string> QueryNeighbors(VoxelPosition center, int radius)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(radius);

        var radiusSquared = (long)radius * radius;
        var found = new List<string>();
        foreach (var (id, pos) in CandidatesAround(center, radius))
        {
            if (center.SquaredDistanceTo(pos) <= radiusSquared)
            {
                found.Add(id);
            }
        }

        return found;
    }

    /// <summary>
    /// All unique unordered agent pairs within <paramref name="radius"/> voxels of
    /// each other. Pair order is normalized by id so each pair appears once.
    /// </summary>
    public IReadOnlyList<InteractionCandidate> FindProximityPairs(int radius)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(radius);

        var radiusSquared = (long)radius * radius;
        var seen = new HashSet<(string, string)>();
        var pairs = new List<InteractionCandidate>();

        foreach (var bucket in _cells.Values)
        {
            foreach (var (id, pos) in bucket)
            {
                foreach (var (otherId, otherPos) in CandidatesAround(pos, radius))
                {
                    if (string.Equals(id, otherId, StringComparison.Ordinal))
                    {
                        continue;
                    }

                    var squared = pos.SquaredDistanceTo(otherPos);
                    if (squared > radiusSquared)
                    {
                        continue;
                    }

                    var key = string.CompareOrdinal(id, otherId) < 0 ? (id, otherId) : (otherId, id);
                    if (seen.Add(key))
                    {
                        pairs.Add(new InteractionCandidate(key.Item1, key.Item2, Math.Sqrt(squared)));
                    }
                }
            }
        }

        return pairs;
    }

    private IEnumerable<(string Id, VoxelPosition Pos)> CandidatesAround(VoxelPosition center, int radius)
    {
        var cellRadius = (radius + _cellSize - 1) / _cellSize;
        var (cx, cy, cz) = CellOf(center);

        for (var dx = -cellRadius; dx <= cellRadius; dx++)
        {
            for (var dy = -cellRadius; dy <= cellRadius; dy++)
            {
                for (var dz = -cellRadius; dz <= cellRadius; dz++)
                {
                    if (_cells.TryGetValue((cx + dx, cy + dy, cz + dz), out var bucket))
                    {
                        foreach (var item in bucket)
                        {
                            yield return item;
                        }
                    }
                }
            }
        }
    }

    private (int X, int Y, int Z) CellOf(VoxelPosition p) =>
        (FloorDiv(p.X, _cellSize), FloorDiv(p.Y, _cellSize), FloorDiv(p.Z, _cellSize));

    // Floor division so negative coordinates bucket consistently.
    private static int FloorDiv(int a, int b)
    {
        var q = a / b;
        if (a % b != 0 && (a < 0) != (b < 0))
        {
            q--;
        }

        return q;
    }
}
