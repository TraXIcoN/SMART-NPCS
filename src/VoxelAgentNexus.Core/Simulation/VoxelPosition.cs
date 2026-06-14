namespace VoxelAgentNexus.Core.Simulation;

/// <summary>
/// An integer voxel-grid coordinate. A value type so positions can sit in tight
/// ECS arrays without allocation. (DESIGN_BRIEF.md §2.1, §5.)
/// </summary>
public readonly record struct VoxelPosition(int X, int Y, int Z)
{
    /// <summary>Squared Euclidean distance — avoids a sqrt for radius comparisons.</summary>
    public long SquaredDistanceTo(VoxelPosition other)
    {
        long dx = X - other.X;
        long dy = Y - other.Y;
        long dz = Z - other.Z;
        return (dx * dx) + (dy * dy) + (dz * dz);
    }

    /// <summary>Euclidean distance.</summary>
    public double DistanceTo(VoxelPosition other) => Math.Sqrt(SquaredDistanceTo(other));

    /// <summary>Chebyshev (chessboard) distance — maps cleanly to chunk rings.</summary>
    public int ChebyshevDistanceTo(VoxelPosition other) =>
        Math.Max(Math.Abs(X - other.X), Math.Max(Math.Abs(Y - other.Y), Math.Abs(Z - other.Z)));
}
