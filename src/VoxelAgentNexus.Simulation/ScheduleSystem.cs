using VoxelAgentNexus.Core.Simulation;

namespace VoxelAgentNexus.Simulation;

/// <summary>
/// Resolves what an NPC should be doing right now from its <see cref="DailySchedule"/>.
/// Pure and deterministic — it runs for every NPC on the simulation thread at zero
/// token cost; the LLM is invoked only for salient moments layered on top.
/// (DESIGN_BRIEF.md §5.)
/// </summary>
public static class ScheduleSystem
{
    /// <summary>
    /// Return the active schedule entry for <paramref name="timeOfDay"/>. The
    /// routine wraps at midnight: before the first entry, the previous day's last
    /// entry is still in effect.
    /// </summary>
    public static ScheduleEntry Resolve(DailySchedule schedule, TimeOnly timeOfDay)
    {
        ArgumentNullException.ThrowIfNull(schedule);

        ScheduleEntry? active = null;
        foreach (var entry in schedule.Entries) // ascending by StartTime
        {
            if (entry.StartTime <= timeOfDay)
            {
                active = entry;
            }
            else
            {
                break;
            }
        }

        // Before the first slot of the day → wrap to the last slot.
        return active ?? schedule.Entries[^1];
    }
}
