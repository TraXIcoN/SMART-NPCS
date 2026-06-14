using VoxelAgentNexus.Core.Simulation;

namespace VoxelAgentNexus.Simulation;

/// <summary>
/// Moves an agent one step toward a target each tick, with optional random lateral
/// jitter so paths wander organically. All randomness flows from the injected
/// <see cref="Random"/>, so a fixed seed reproduces a run exactly while a random
/// seed makes every run diverge. (DESIGN_BRIEF.md §9.)
/// </summary>
public sealed class MovementSystem
{
    private readonly Random _rng;
    private readonly int _stepSize;
    private readonly int _jitter;

    public MovementSystem(Random rng, int stepSize = 1, int jitter = 0)
    {
        _rng = rng ?? throw new ArgumentNullException(nameof(rng));
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(stepSize);
        ArgumentOutOfRangeException.ThrowIfNegative(jitter);
        _stepSize = stepSize;
        _jitter = jitter;
    }

    /// <summary>Compute the next position one step toward <paramref name="target"/>.</summary>
    public VoxelPosition Step(VoxelPosition from, VoxelPosition target)
    {
        var nx = MoveAxis(from.X, target.X);
        var ny = MoveAxis(from.Y, target.Y);
        var nz = MoveAxis(from.Z, target.Z);

        if (_jitter > 0)
        {
            // Non-crypto gameplay randomness is intentional here.
#pragma warning disable CA5394
            nx += _rng.Next(-_jitter, _jitter + 1);
            nz += _rng.Next(-_jitter, _jitter + 1);
#pragma warning restore CA5394
        }

        return new VoxelPosition(nx, ny, nz);
    }

    private int MoveAxis(int from, int to)
    {
        var diff = to - from;
        if (diff == 0)
        {
            return from;
        }

        var step = Math.Sign(diff) * Math.Min(_stepSize, Math.Abs(diff));
        return from + step;
    }
}
