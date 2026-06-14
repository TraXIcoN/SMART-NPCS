namespace VoxelAgentNexus.Core.Simulation;

/// <summary>
/// One slot in a daily routine: from <see cref="StartTime"/> the NPC pursues
/// <see cref="Activity"/> at <see cref="Location"/>, until the next entry begins.
/// </summary>
public readonly record struct ScheduleEntry(TimeOnly StartTime, Activity Activity, VoxelPosition Location);

/// <summary>
/// A repeating daily routine. Entries are kept sorted by start time; the routine
/// wraps at midnight, so the slot before the first entry is the last entry of the
/// previous day. (DESIGN_BRIEF.md §5.)
/// </summary>
public sealed record DailySchedule
{
    /// <summary>Entries in ascending start-time order; never empty.</summary>
    public required IReadOnlyList<ScheduleEntry> Entries { get; init; }

    /// <summary>Build a schedule, sorting entries and validating non-emptiness.</summary>
    public static DailySchedule Create(IEnumerable<ScheduleEntry> entries)
    {
        ArgumentNullException.ThrowIfNull(entries);

        var sorted = entries.OrderBy(e => e.StartTime).ToList();
        if (sorted.Count == 0)
        {
            throw new ArgumentException("A schedule needs at least one entry.", nameof(entries));
        }

        return new DailySchedule { Entries = sorted };
    }
}
