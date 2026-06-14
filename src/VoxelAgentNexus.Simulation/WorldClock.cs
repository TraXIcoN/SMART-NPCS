namespace VoxelAgentNexus.Simulation;

/// <summary>
/// Advances in-game time on a fixed step. Time-of-day wraps at midnight so daily
/// schedules repeat. Kept separate from wall-clock so the simulation is fully
/// reproducible from a seed. (DESIGN_BRIEF.md §9.)
/// </summary>
public sealed class WorldClock
{
    private readonly double _gameSecondsPerTick;

    public WorldClock(TimeOnly start, double gameSecondsPerTick)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(gameSecondsPerTick);
        TimeOfDay = start;
        _gameSecondsPerTick = gameSecondsPerTick;
    }

    /// <summary>Number of ticks elapsed since construction.</summary>
    public long Tick { get; private set; }

    /// <summary>Current in-game time of day.</summary>
    public TimeOnly TimeOfDay { get; private set; }

    /// <summary>Advance one tick.</summary>
    public void Advance()
    {
        Tick++;
        TimeOfDay = TimeOfDay.Add(TimeSpan.FromSeconds(_gameSecondsPerTick));
    }
}
