using VoxelAgentNexus.Core.Memory;

namespace VoxelAgentNexus.Memory;

/// <summary>
/// Generative-Agents-style retrieval: each memory is scored as a weighted sum of
/// recency (exponential decay), importance (normalized poignancy), and relevance
/// (cosine similarity to the query embedding). Pure and synchronous — it runs
/// over the already-decrypted in-RAM working set and touches nothing else.
/// (DESIGN_BRIEF.md §3, §3.1.)
/// </summary>
public sealed class CompositeMemoryRetriever : IMemoryRetriever
{
    /// <inheritdoc />
    public IReadOnlyList<ScoredMemory> Retrieve(RetrievalQuery query, IReadOnlyList<MemoryRecord> workingSet)
    {
        ArgumentNullException.ThrowIfNull(query);
        ArgumentNullException.ThrowIfNull(workingSet);

        var scored = new List<ScoredMemory>(workingSet.Count);
        foreach (var memory in workingSet)
        {
            if (!string.Equals(memory.NpcId, query.NpcId, StringComparison.Ordinal))
            {
                continue;
            }

            var lastTouched = memory.LastAccessedAt == default ? memory.CreatedAt : memory.LastAccessedAt;
            var recency = Recency(query.Now, lastTouched, query.RecencyHalfLife);
            var importance = Math.Clamp(memory.Importance / 10f, 0f, 1f);
            var relevance = CosineSimilarity(query.QueryEmbedding.Span, memory.Embedding.Span);

            var score =
                (recency * query.RecencyWeight) +
                (importance * query.ImportanceWeight) +
                (relevance * query.RelevanceWeight);

            scored.Add(new ScoredMemory(memory, score));
        }

        scored.Sort(static (a, b) => b.Score.CompareTo(a.Score));
        if (scored.Count > query.TopK)
        {
            scored.RemoveRange(query.TopK, scored.Count - query.TopK);
        }

        return scored;
    }

    private static float Recency(DateTimeOffset now, DateTimeOffset lastTouched, TimeSpan halfLife)
    {
        var age = now - lastTouched;
        if (age < TimeSpan.Zero)
        {
            age = TimeSpan.Zero;
        }

        if (halfLife <= TimeSpan.Zero)
        {
            return age == TimeSpan.Zero ? 1f : 0f;
        }

        var lambda = Math.Log(2) / halfLife.TotalSeconds;
        return (float)Math.Exp(-lambda * age.TotalSeconds);
    }

    private static float CosineSimilarity(ReadOnlySpan<float> a, ReadOnlySpan<float> b)
    {
        if (a.Length == 0 || a.Length != b.Length)
        {
            return 0f;
        }

        double dot = 0, normA = 0, normB = 0;
        for (var i = 0; i < a.Length; i++)
        {
            dot += a[i] * b[i];
            normA += a[i] * a[i];
            normB += b[i] * b[i];
        }

        if (normA == 0 || normB == 0)
        {
            return 0f;
        }

        var cosine = dot / (Math.Sqrt(normA) * Math.Sqrt(normB));
        return (float)Math.Clamp(cosine, 0d, 1d);
    }
}
