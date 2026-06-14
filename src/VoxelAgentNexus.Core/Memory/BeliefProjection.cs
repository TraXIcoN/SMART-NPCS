namespace VoxelAgentNexus.Core.Memory;

/// <summary>
/// Selects an NPC's current durable beliefs from its memories — the reflection
/// records, ranked by importance then recency. These are folded back into the
/// NPC's prompt so its accumulated understanding shapes how it speaks.
/// (DESIGN_BRIEF.md §3, §9.)
/// </summary>
public static class BeliefProjection
{
    /// <summary>Top reflection contents for an NPC, most salient first.</summary>
    public static IReadOnlyList<string> TopBeliefs(IReadOnlyList<MemoryRecord> memories, int maxBeliefs)
    {
        ArgumentNullException.ThrowIfNull(memories);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(maxBeliefs);

        return memories
            .Where(m => m.Kind == MemoryKind.Reflection)
            .OrderByDescending(m => m.Importance)
            .ThenByDescending(m => m.CreatedAt)
            .Take(maxBeliefs)
            .Select(m => m.Content)
            .ToList();
    }
}
