namespace VoxelAgentNexus.Core.Memory;

/// <summary>
/// Scores and ranks an NPC's working set for a query. Pure, synchronous, and
/// CPU-only: it operates entirely over the already-decrypted in-RAM working set
/// and never touches ciphertext, the store, or the network. (DESIGN_BRIEF.md §3.1.)
/// </summary>
public interface IMemoryRetriever
{
    /// <summary>
    /// Return the top-k memories by composite recency/importance/relevance score.
    /// </summary>
    /// <param name="query">Scoring parameters and weights.</param>
    /// <param name="workingSet">Decrypted memories for the NPC (from <see cref="IMemoryStore.LoadWorkingSetAsync"/>).</param>
    IReadOnlyList<ScoredMemory> Retrieve(RetrievalQuery query, IReadOnlyList<MemoryRecord> workingSet);
}
