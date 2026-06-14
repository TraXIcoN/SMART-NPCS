using VoxelAgentNexus.Core.Memory;
using Xunit;

namespace VoxelAgentNexus.IntegrationTests;

public sealed class BeliefProjectionTests
{
    private static readonly DateTimeOffset T0 = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

    private static MemoryRecord Mem(MemoryKind kind, string content, float importance, DateTimeOffset at) => new()
    {
        Id = Guid.NewGuid(),
        NpcId = "brom",
        Kind = kind,
        Content = content,
        Importance = importance,
        CreatedAt = at,
        LastAccessedAt = at,
    };

    [Fact]
    public void Selects_Only_Reflections_Ranked_By_Importance_Then_Recency()
    {
        var memories = new[]
        {
            Mem(MemoryKind.Observation, "raw observation", 10f, T0), // excluded: not a reflection
            Mem(MemoryKind.Reflection, "low importance", 3f, T0),
            Mem(MemoryKind.Reflection, "high but older", 8f, T0),
            Mem(MemoryKind.Reflection, "high and newer", 8f, T0.AddHours(1)),
        };

        var beliefs = BeliefProjection.TopBeliefs(memories, maxBeliefs: 2);

        Assert.Equal(2, beliefs.Count);
        Assert.Equal("high and newer", beliefs[0]); // importance tie broken by recency
        Assert.Equal("high but older", beliefs[1]);
        Assert.DoesNotContain("raw observation", beliefs);
    }

    [Fact]
    public void Empty_When_No_Reflections()
    {
        var memories = new[] { Mem(MemoryKind.Observation, "just an observation", 9f, T0) };

        Assert.Empty(BeliefProjection.TopBeliefs(memories, maxBeliefs: 5));
    }
}
