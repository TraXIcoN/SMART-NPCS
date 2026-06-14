namespace VoxelAgentNexus.Core.Memory;

/// <summary>Layer of the memory stream a record belongs to. (DESIGN_BRIEF.md §3.)</summary>
public enum MemoryKind
{
    /// <summary>Raw observed event (bottom of the stream).</summary>
    Observation = 0,

    /// <summary>Synthesized higher-level inference produced by a reflection pass.</summary>
    Reflection = 1,

    /// <summary>A forward-looking intention/plan.</summary>
    Plan = 2,
}

/// <summary>
/// The plaintext, in-RAM representation of a single memory. This type lives only
/// in process memory; on persistence it is serialized and sealed into a
/// <see cref="Security.SealedBlob"/>. (DESIGN_BRIEF.md §3, §3.1.)
///
/// Fields mirror the Generative Agents memory stream: natural-language
/// <see cref="Content"/>, an <see cref="Importance"/> score, timestamps for the
/// recency term, an <see cref="Embedding"/> for the relevance term, and
/// <see cref="EvidenceIds"/> linking a reflection to the memories that produced it.
/// </summary>
public sealed record MemoryRecord
{
    /// <summary>Stable unique id.</summary>
    public required Guid Id { get; init; }

    /// <summary>Owning NPC (also the store partition key).</summary>
    public required string NpcId { get; init; }

    /// <summary>Which layer of the stream this is.</summary>
    public required MemoryKind Kind { get; init; }

    /// <summary>Natural-language description of the memory.</summary>
    public required string Content { get; init; }

    /// <summary>Poignancy score (1–10, Generative Agents). Drives the importance term and reflection trigger.</summary>
    public required float Importance { get; init; }

    /// <summary>When the memory was formed.</summary>
    public required DateTimeOffset CreatedAt { get; init; }

    /// <summary>When the memory was last retrieved (recency decays from here).</summary>
    public DateTimeOffset LastAccessedAt { get; init; }

    /// <summary>Embedding of <see cref="Content"/> for relevance scoring. Empty until computed.</summary>
    public ReadOnlyMemory<float> Embedding { get; init; }

    /// <summary>For reflections: the memories this inference was drawn from.</summary>
    public IReadOnlyList<Guid> EvidenceIds { get; init; } = [];
}
