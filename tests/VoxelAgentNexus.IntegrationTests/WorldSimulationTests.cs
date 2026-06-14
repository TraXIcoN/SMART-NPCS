using VoxelAgentNexus.Core.Memory;
using VoxelAgentNexus.Core.Simulation;
using VoxelAgentNexus.Simulation;
using Xunit;

namespace VoxelAgentNexus.IntegrationTests;

public sealed class WorldSimulationTests
{
    private static readonly DateTimeOffset Start = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

    private static DailySchedule AllDayAt(VoxelPosition location) =>
        DailySchedule.Create([new ScheduleEntry(new TimeOnly(0, 0), Activity.Idle, location)]);

    private static WorldSimulation BuildWorld(int seed, int jitter, VoxelPosition player, IReadOnlyList<WorldNpc> npcs)
    {
        var interactions = new ProximityInteractionSystem(
            new SpatialHashGrid(cellSize: 16),
            new LodClassifier(new LodThresholds { NearRadius = 10, MidRadius = 50 }),
            new InMemoryRelationshipGraph(),
            new ProximityRules());

        return new WorldSimulation(
            npcs,
            new WorldClock(new TimeOnly(8, 0), gameSecondsPerTick: 60),
            new MovementSystem(new Random(seed), stepSize: 2, jitter: jitter),
            interactions,
            player,
            Start,
            TimeSpan.FromSeconds(60));
    }

    [Fact]
    public async Task Npc_Walks_To_Its_Scheduled_Location()
    {
        var npc = new WorldNpc("brom", new VoxelPosition(0, 64, 0), AllDayAt(new VoxelPosition(10, 64, 10)));
        var world = BuildWorld(seed: 1, jitter: 0, player: new VoxelPosition(-100, 64, -100), [npc]);

        for (var i = 0; i < 20; i++)
        {
            await world.TickAsync();
        }

        Assert.Equal(new VoxelPosition(10, 64, 10), npc.Position);
    }

    [Fact]
    public async Task Same_Seed_Reproduces_Run_Different_Seed_Diverges()
    {
        async Task<List<VoxelPosition>> Run(int seed)
        {
            var wanderer = new WorldNpc("wanderer", new VoxelPosition(0, 64, 0), AllDayAt(new VoxelPosition(50, 64, 50)));
            var world = BuildWorld(seed, jitter: 3, player: new VoxelPosition(-100, 64, -100), [wanderer]);

            var trace = new List<VoxelPosition>();
            for (var i = 0; i < 30; i++)
            {
                await world.TickAsync();
                trace.Add(wanderer.Position);
            }

            return trace;
        }

        var runA = await Run(seed: 7);
        var runAagain = await Run(seed: 7);
        var runC = await Run(seed: 99);

        Assert.Equal(runA, runAagain);   // reproducible from the same seed
        Assert.NotEqual(runA, runC);     // different seed → different world
    }

    [Fact]
    public async Task Converging_Npcs_Produce_Proximity_Interactions()
    {
        var alice = new WorldNpc("alice", new VoxelPosition(0, 64, 0), AllDayAt(new VoxelPosition(5, 64, 5)));
        var bob = new WorldNpc("bob", new VoxelPosition(2, 64, 0), AllDayAt(new VoxelPosition(5, 64, 5)));
        var world = BuildWorld(seed: 1, jitter: 0, player: new VoxelPosition(5, 64, 5), [alice, bob]);

        var all = new List<InteractionResolution>();
        for (var i = 0; i < 15; i++)
        {
            var result = await world.TickAsync();
            all.AddRange(result.Interactions);
        }

        Assert.NotEmpty(all);
        Assert.Contains(all, r => r.Outcome == InteractionOutcome.TemplatedGreeting);
    }

    private sealed class InMemoryRelationshipGraph : IRelationshipGraph
    {
        private readonly Dictionary<(string, string), RelationshipEdge> _edges = new();

        public ValueTask<RelationshipEdge?> GetEdgeAsync(string fromId, string toId, CancellationToken cancellationToken = default) =>
            ValueTask.FromResult(_edges.TryGetValue((fromId, toId), out var edge) ? edge : null);

        public ValueTask UpsertEdgeAsync(RelationshipEdge edge, CancellationToken cancellationToken = default)
        {
            _edges[(edge.FromId, edge.ToId)] = edge;
            return ValueTask.CompletedTask;
        }

        public ValueTask<IReadOnlyList<RelationshipEdge>> GetEdgesFromAsync(string fromId, CancellationToken cancellationToken = default) =>
            ValueTask.FromResult<IReadOnlyList<RelationshipEdge>>(
                _edges.Values.Where(e => string.Equals(e.FromId, fromId, StringComparison.Ordinal)).ToList());
    }
}
