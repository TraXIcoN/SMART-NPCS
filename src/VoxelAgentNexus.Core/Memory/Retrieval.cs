namespace VoxelAgentNexus.Core.Memory;

/// <summary>
/// A retrieval request scored against an NPC's in-RAM working set. The composite
/// score is <c>recency·w_r + importance·w_i + relevance·w_v</c>, following the
/// Generative Agents retrieval function. (DESIGN_BRIEF.md §3.)
/// </summary>
public sealed record RetrievalQuery
{
    /// <summary>NPC whose memories to search.</summary>
    public required string NpcId { get; init; }

    /// <summary>Embedding of the current situation/query, for the relevance term.</summary>
    public required ReadOnlyMemory<float> QueryEmbedding { get; init; }

    /// <summary>Reference "now" used to compute recency decay.</summary>
    public required DateTimeOffset Now { get; init; }

    /// <summary>How many memories to return for the prompt's top-k budget.</summary>
    public int TopK { get; init; } = 8;

    /// <summary>Weight on the recency term.</summary>
    public float RecencyWeight { get; init; } = 1f;

    /// <summary>Weight on the importance term.</summary>
    public float ImportanceWeight { get; init; } = 1f;

    /// <summary>Weight on the relevance (embedding-similarity) term.</summary>
    public float RelevanceWeight { get; init; } = 1f;

    /// <summary>Exponential recency half-life. Older memories decay below this horizon.</summary>
    public TimeSpan RecencyHalfLife { get; init; } = TimeSpan.FromHours(1);
}

/// <summary>A memory paired with its composite retrieval score.</summary>
/// <param name="Memory">The retrieved memory.</param>
/// <param name="Score">Composite recency/importance/relevance score.</param>
public readonly record struct ScoredMemory(MemoryRecord Memory, float Score);
