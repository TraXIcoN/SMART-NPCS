namespace VoxelAgentNexus.Core.Memory;

/// <summary>
/// Bootstrap state for one NPC at world creation: the "presets and preloaded
/// conversations" that give an NPC a starting voice before lived experience makes
/// it drift. Written to the encrypted store by the world seeder.
/// (DESIGN_BRIEF.md §9.)
/// </summary>
public sealed record NpcSeed
{
    /// <summary>NPC this seed is for.</summary>
    public required string NpcId { get; init; }

    /// <summary>Preloaded observation memories (e.g. backstory, prior conversations).</summary>
    public IReadOnlyList<string> SeedMemories { get; init; } = [];

    /// <summary>Preloaded relationship edges (initial trust/affinity with others).</summary>
    public IReadOnlyList<RelationshipEdge> SeedRelationships { get; init; } = [];

    /// <summary>Importance assigned to each seed memory.</summary>
    public float SeedMemoryImportance { get; init; } = 5f;
}
