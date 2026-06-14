using VoxelAgentNexus.Core.Simulation;

namespace VoxelAgentNexus.Simulation;

/// <summary>
/// Mutable per-NPC simulation state owned by the world. This is the authoritative
/// state the tick loop mutates; the immutable <see cref="AgentSnapshot"/> is what
/// systems read each tick. Kept deliberately minimal so off-screen abstraction
/// can't corrupt much. (DESIGN_BRIEF.md §5.)
/// </summary>
public sealed class WorldNpc
{
    public WorldNpc(string id, VoxelPosition start, DailySchedule schedule)
    {
        ArgumentException.ThrowIfNullOrEmpty(id);
        ArgumentNullException.ThrowIfNull(schedule);
        Id = id;
        Position = start;
        Schedule = schedule;
    }

    public string Id { get; }

    public VoxelPosition Position { get; set; }

    public DailySchedule Schedule { get; }
}
