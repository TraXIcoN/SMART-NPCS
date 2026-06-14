namespace VoxelAgentNexus.Core.Ai;

/// <summary>
/// Produces vector embeddings for memory text. Kept separate from
/// <see cref="INpcAiAdapter"/> because embeddings are high-frequency grunt work
/// that should run on-device (free, private) even when dialogue generation goes
/// to the cloud. Feeds the relevance term of memory retrieval.
/// (DESIGN_BRIEF.md §3, §4.)
/// </summary>
public interface IEmbeddingProvider
{
    /// <summary>Dimensionality of vectors produced by this provider.</summary>
    int Dimensions { get; }

    /// <summary>
    /// Embed a batch of texts. Batching amortizes model invocation cost; callers
    /// should dedupe and cache before calling. (DESIGN_BRIEF.md §3 housekeeping.)
    /// </summary>
    ValueTask<IReadOnlyList<ReadOnlyMemory<float>>> EmbedAsync(
        IReadOnlyList<string> inputs,
        CancellationToken cancellationToken = default);
}
