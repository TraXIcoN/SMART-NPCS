using VoxelAgentNexus.Core.Memory;
using VoxelAgentNexus.Memory;
using Xunit;

namespace VoxelAgentNexus.IntegrationTests;

public sealed class WorldSeederTests
{
    [Fact]
    public async Task Seeds_Preset_Memories_And_Relationships()
    {
        var store = new FakeMemoryStore();
        var graph = new FakeGraph();
        var seeder = new WorldSeeder(store, graph);

        var seed = new NpcSeed
        {
            NpcId = "brom",
            SeedMemories =
            [
                "I have been the village blacksmith for twenty years.",
                "I once shod the horse of a traveling knight.",
            ],
            SeedRelationships =
            [
                new RelationshipEdge { FromId = "brom", ToId = "mara", Affinity = 0.3f, Trust = 0.5f },
            ],
            SeedMemoryImportance = 6f,
        };

        await seeder.SeedAsync(seed, DateTimeOffset.UtcNow);

        Assert.Equal(2, store.Records.Count);
        Assert.All(store.Records, r => Assert.Equal("brom", r.NpcId));
        Assert.All(store.Records, r => Assert.Equal(MemoryKind.Observation, r.Kind));
        Assert.All(store.Records, r => Assert.Equal(6f, r.Importance));

        var edge = Assert.Single(graph.Edges);
        Assert.Equal("brom", edge.FromId);
        Assert.Equal("mara", edge.ToId);
    }
}
