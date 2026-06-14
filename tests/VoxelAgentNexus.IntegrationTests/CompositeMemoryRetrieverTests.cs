using VoxelAgentNexus.Core.Memory;
using VoxelAgentNexus.Memory;
using Xunit;

namespace VoxelAgentNexus.IntegrationTests;

public sealed class CompositeMemoryRetrieverTests
{
    [Fact]
    public void Recent_Relevant_Important_Memory_Ranks_First()
    {
        var now = DateTimeOffset.UtcNow;
        var retriever = new CompositeMemoryRetriever();

        var winner = new MemoryRecord
        {
            Id = Guid.NewGuid(),
            NpcId = "n",
            Kind = MemoryKind.Observation,
            Content = "recent, relevant, important",
            Importance = 9f,
            CreatedAt = now,
            LastAccessedAt = now,
            Embedding = new float[] { 1f, 0f },
        };
        var loser = new MemoryRecord
        {
            Id = Guid.NewGuid(),
            NpcId = "n",
            Kind = MemoryKind.Observation,
            Content = "old, irrelevant, trivial",
            Importance = 1f,
            CreatedAt = now - TimeSpan.FromDays(3),
            LastAccessedAt = now - TimeSpan.FromDays(3),
            Embedding = new float[] { 0f, 1f },
        };

        var query = new RetrievalQuery
        {
            NpcId = "n",
            QueryEmbedding = new float[] { 1f, 0f },
            Now = now,
            TopK = 2,
            RecencyHalfLife = TimeSpan.FromHours(1),
        };

        var ranked = retriever.Retrieve(query, [loser, winner]);

        Assert.Equal(2, ranked.Count);
        Assert.Equal(winner.Id, ranked[0].Memory.Id);
        Assert.True(ranked[0].Score > ranked[1].Score);
    }

    [Fact]
    public void TopK_Limits_Result_Count()
    {
        var now = DateTimeOffset.UtcNow;
        var retriever = new CompositeMemoryRetriever();

        var working = new List<MemoryRecord>();
        for (var i = 0; i < 10; i++)
        {
            working.Add(new MemoryRecord
            {
                Id = Guid.NewGuid(),
                NpcId = "n",
                Kind = MemoryKind.Observation,
                Content = $"memory {i}",
                Importance = i,
                CreatedAt = now,
                LastAccessedAt = now,
            });
        }

        var query = new RetrievalQuery
        {
            NpcId = "n",
            QueryEmbedding = ReadOnlyMemory<float>.Empty,
            Now = now,
            TopK = 3,
        };

        var ranked = retriever.Retrieve(query, working);

        Assert.Equal(3, ranked.Count);
    }
}
