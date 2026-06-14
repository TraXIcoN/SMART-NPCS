using VoxelAgentNexus.Core.Memory;
using VoxelAgentNexus.Memory;
using Xunit;

namespace VoxelAgentNexus.IntegrationTests;

public sealed class ReflectionTests
{
    private static readonly DateTimeOffset T0 = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

    private static MemoryRecord Observation(string npc, string content, float importance) => new()
    {
        Id = Guid.NewGuid(),
        NpcId = npc,
        Kind = MemoryKind.Observation,
        Content = content,
        Importance = importance,
        CreatedAt = T0,
        LastAccessedAt = T0,
    };

    [Fact]
    public void Trigger_Sums_Importance_Against_Threshold()
    {
        var memories = new[] { Observation("n", "a", 5f), Observation("n", "b", 4f) };

        Assert.Equal(9f, ReflectionTrigger.AccumulatedImportance(memories));
        Assert.True(ReflectionTrigger.ShouldReflect(memories, 8f));
        Assert.False(ReflectionTrigger.ShouldReflect(memories, 10f));
    }

    [Fact]
    public async Task Below_Threshold_Does_Not_Reflect()
    {
        var store = new FakeMemoryStore();
        await store.AppendAsync(Observation("brom", "saw a bird", 3f));
        var synthesizer = new RecordingSynthesizer("a belief");
        var system = new ReflectionSystem(store, synthesizer, importanceThreshold: 10f);

        var created = await system.MaybeReflectAsync("brom", T0.AddMinutes(1));

        Assert.Empty(created);
        Assert.Empty(synthesizer.Calls);
        Assert.DoesNotContain(store.Records, r => r.Kind == MemoryKind.Reflection);
    }

    [Fact]
    public async Task Above_Threshold_Creates_Belief_Linked_To_Evidence_And_Then_Resets()
    {
        var store = new FakeMemoryStore();
        await store.AppendAsync(Observation("brom", "player stole bread", 5f));
        await store.AppendAsync(Observation("brom", "player stole apples", 5f));
        await store.AppendAsync(Observation("brom", "player stole a coin", 5f));
        var synthesizer = new RecordingSynthesizer("The player is a thief.");
        var system = new ReflectionSystem(store, synthesizer, importanceThreshold: 10f);

        var created = await system.MaybeReflectAsync("brom", T0.AddMinutes(1));

        var belief = Assert.Single(created);
        Assert.Equal(MemoryKind.Reflection, belief.Kind);
        Assert.Equal("The player is a thief.", belief.Content);
        Assert.Equal(3, belief.EvidenceIds.Count);
        Assert.Single(synthesizer.Calls);

        // No new observations after the reflection → a second pass is a no-op.
        var again = await system.MaybeReflectAsync("brom", T0.AddMinutes(2));
        Assert.Empty(again);
    }

    private sealed class RecordingSynthesizer : IReflectionSynthesizer
    {
        private readonly IReadOnlyList<string> _beliefs;

        public RecordingSynthesizer(params string[] beliefs) => _beliefs = beliefs;

        public List<IReadOnlyList<MemoryRecord>> Calls { get; } = [];

        public ValueTask<IReadOnlyList<string>> SynthesizeAsync(string npcId, IReadOnlyList<MemoryRecord> evidence, CancellationToken cancellationToken = default)
        {
            Calls.Add(evidence);
            return ValueTask.FromResult(_beliefs);
        }
    }
}
