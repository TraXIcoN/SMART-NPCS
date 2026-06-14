namespace VoxelAgentNexus.Core.Memory;

/// <summary>
/// A directed social edge from one actor to another. Relationships are stored as
/// structured, queryable rows rather than free text so proximity ticks and
/// off-screen LOD resolution can update them cheaply. (DESIGN_BRIEF.md §3, §5.)
/// </summary>
public sealed record RelationshipEdge
{
    /// <summary>Actor holding the sentiment (NPC id).</summary>
    public required string FromId { get; init; }

    /// <summary>Actor the sentiment is about (NPC or player id).</summary>
    public required string ToId { get; init; }

    /// <summary>Affection, -1 (hostile) … +1 (devoted).</summary>
    public float Affinity { get; init; }

    /// <summary>Trust, 0 … 1.</summary>
    public float Trust { get; init; }

    /// <summary>Last time this edge changed.</summary>
    public DateTimeOffset UpdatedAt { get; init; }
}

/// <summary>
/// The social graph over NPCs and the player. Persisted through the same encrypted
/// store as memories. Implemented by VoxelAgentNexus.Memory. (DESIGN_BRIEF.md §3.)
/// </summary>
public interface IRelationshipGraph
{
    /// <summary>Get the directed edge from one actor to another, or null.</summary>
    ValueTask<RelationshipEdge?> GetEdgeAsync(string fromId, string toId, CancellationToken cancellationToken = default);

    /// <summary>Insert or update an edge. Callers clamp deltas before upsert (Radiant AI guardrail).</summary>
    ValueTask UpsertEdgeAsync(RelationshipEdge edge, CancellationToken cancellationToken = default);

    /// <summary>All edges originating from an actor (e.g. to assemble social context for a prompt).</summary>
    ValueTask<IReadOnlyList<RelationshipEdge>> GetEdgesFromAsync(string fromId, CancellationToken cancellationToken = default);
}
