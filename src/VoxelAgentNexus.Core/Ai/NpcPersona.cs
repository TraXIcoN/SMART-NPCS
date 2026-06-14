namespace VoxelAgentNexus.Core.Ai;

/// <summary>
/// The stable identity of an NPC. <see cref="SystemPrompt"/> holds personality,
/// backstory, world lore, and rules of engagement — it forms the cache-immutable
/// prefix of every request for this NPC, so it must not change mid-session.
/// (DESIGN_BRIEF.md §4.)
/// </summary>
public sealed record NpcPersona
{
    /// <summary>Stable unique id, matching the memory partition key.</summary>
    public required string NpcId { get; init; }

    /// <summary>Display name.</summary>
    public required string Name { get; init; }

    /// <summary>Persona + lore + rules; the cacheable system prefix.</summary>
    public required string SystemPrompt { get; init; }
}
