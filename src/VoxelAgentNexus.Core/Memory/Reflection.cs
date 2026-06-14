namespace VoxelAgentNexus.Core.Memory;

/// <summary>
/// Decides when an NPC has accumulated enough salient experience to reflect.
/// Follows Generative Agents: sum the importance of observations since the last
/// reflection; cross a threshold → reflect. Pure and deterministic.
/// (DESIGN_BRIEF.md §3, §9.)
/// </summary>
public static class ReflectionTrigger
{
    /// <summary>Total importance across a set of memories.</summary>
    public static float AccumulatedImportance(IEnumerable<MemoryRecord> memories)
    {
        ArgumentNullException.ThrowIfNull(memories);

        var total = 0f;
        foreach (var memory in memories)
        {
            total += memory.Importance;
        }

        return total;
    }

    /// <summary>True when accumulated importance meets or exceeds the threshold.</summary>
    public static bool ShouldReflect(IEnumerable<MemoryRecord> memories, float importanceThreshold) =>
        AccumulatedImportance(memories) >= importanceThreshold;
}

/// <summary>
/// Distills a set of observations into higher-level beliefs — the abstraction step
/// that turns "more memories" into "better understanding". The simulation depends
/// on this seam; the AI layer supplies the implementation. (DESIGN_BRIEF.md §3, §9.)
/// </summary>
public interface IReflectionSynthesizer
{
    /// <summary>Produce zero or more concise belief statements from the evidence.</summary>
    ValueTask<IReadOnlyList<string>> SynthesizeAsync(
        string npcId,
        IReadOnlyList<MemoryRecord> evidence,
        CancellationToken cancellationToken = default);
}
