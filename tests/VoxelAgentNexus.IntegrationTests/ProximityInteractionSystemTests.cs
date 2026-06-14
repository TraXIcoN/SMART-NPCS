using VoxelAgentNexus.Core.Memory;
using VoxelAgentNexus.Core.Simulation;
using VoxelAgentNexus.Simulation;
using Xunit;

namespace VoxelAgentNexus.IntegrationTests;

public sealed class ProximityInteractionSystemTests
{
    private static readonly VoxelPosition Player = new(0, 0, 0);
    private static readonly DateTimeOffset T0 = new(2026, 1, 1, 12, 0, 0, TimeSpan.Zero);

    private static ProximityInteractionSystem NewSystem(
        IRelationshipGraph relationships,
        IDialogueEscalation? escalation = null,
        ProximityRules? rules = null) => new(
            new SpatialHashGrid(cellSize: 16),
            new LodClassifier(new LodThresholds { NearRadius = 10, MidRadius = 50 }),
            relationships,
            rules ?? new ProximityRules(),
            escalation);

    [Fact]
    public async Task Near_Strangers_Get_Templated_Greeting_And_Affinity_Bump()
    {
        var graph = new FakeRelationshipGraph();
        var escalation = new CapturingEscalation();
        var system = NewSystem(graph, escalation);

        var agents = new[]
        {
            new AgentSnapshot("alice", new VoxelPosition(2, 0, 0)),
            new AgentSnapshot("bob", new VoxelPosition(3, 0, 0)),
        };

        var resolutions = await system.TickAsync(agents, Player, T0);

        var resolution = Assert.Single(resolutions);
        Assert.Equal(InteractionOutcome.TemplatedGreeting, resolution.Outcome);
        Assert.Equal(SimulationLod.Near, resolution.Lod);
        Assert.True(resolution.AffinityDelta > 0f);
        Assert.Empty(escalation.Calls);
        Assert.True(graph.Peek("alice", "bob")!.Affinity > 0f);
    }

    [Fact]
    public async Task Mid_Range_Gets_Relationship_Tick_No_Dialogue()
    {
        var graph = new FakeRelationshipGraph();
        var escalation = new CapturingEscalation();
        var system = NewSystem(graph, escalation);

        var agents = new[]
        {
            new AgentSnapshot("alice", new VoxelPosition(30, 0, 0)),
            new AgentSnapshot("bob", new VoxelPosition(31, 0, 0)),
        };

        var resolutions = await system.TickAsync(agents, Player, T0);

        var resolution = Assert.Single(resolutions);
        Assert.Equal(InteractionOutcome.RelationshipTick, resolution.Outcome);
        Assert.Equal(SimulationLod.Mid, resolution.Lod);
        Assert.Empty(escalation.Calls);
    }

    [Fact]
    public async Task Far_Range_Is_Ignored()
    {
        var graph = new FakeRelationshipGraph();
        var system = NewSystem(graph);

        var agents = new[]
        {
            new AgentSnapshot("alice", new VoxelPosition(80, 0, 0)),
            new AgentSnapshot("bob", new VoxelPosition(81, 0, 0)),
        };

        var resolutions = await system.TickAsync(agents, Player, T0);

        Assert.Empty(resolutions);
    }

    [Fact]
    public async Task Cooldown_Debounces_Repeat_Interactions()
    {
        var graph = new FakeRelationshipGraph();
        var rules = new ProximityRules { Cooldown = TimeSpan.FromSeconds(30) };
        var system = NewSystem(graph, rules: rules);

        var agents = new[]
        {
            new AgentSnapshot("alice", new VoxelPosition(2, 0, 0)),
            new AgentSnapshot("bob", new VoxelPosition(3, 0, 0)),
        };

        var first = await system.TickAsync(agents, Player, T0);
        var withinCooldown = await system.TickAsync(agents, Player, T0.AddSeconds(10));
        var afterCooldown = await system.TickAsync(agents, Player, T0.AddSeconds(31));

        Assert.Single(first);
        Assert.Empty(withinCooldown);
        Assert.Single(afterCooldown);
    }

    [Fact]
    public async Task Near_Salient_Pair_Escalates_To_Dialogue()
    {
        var graph = new FakeRelationshipGraph();
        graph.Seed(new RelationshipEdge { FromId = "alice", ToId = "bob", Affinity = 0.5f, Trust = 0.4f });
        var escalation = new CapturingEscalation();
        var system = NewSystem(graph, escalation);

        var agents = new[]
        {
            new AgentSnapshot("alice", new VoxelPosition(2, 0, 0)),
            new AgentSnapshot("bob", new VoxelPosition(3, 0, 0)),
        };

        var resolutions = await system.TickAsync(agents, Player, T0);

        var resolution = Assert.Single(resolutions);
        Assert.Equal(InteractionOutcome.EscalateToDialogue, resolution.Outcome);
        var escalated = Assert.Single(escalation.Calls);
        Assert.Equal("alice", escalated.AgentA);
        Assert.Equal("bob", escalated.AgentB);
    }

    private sealed class FakeRelationshipGraph : IRelationshipGraph
    {
        private readonly Dictionary<(string, string), RelationshipEdge> _edges = new();

        public void Seed(RelationshipEdge edge) => _edges[(edge.FromId, edge.ToId)] = edge;

        public RelationshipEdge? Peek(string from, string to) =>
            _edges.TryGetValue((from, to), out var edge) ? edge : null;

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

    private sealed class CapturingEscalation : IDialogueEscalation
    {
        public List<InteractionResolution> Calls { get; } = [];

        public ValueTask HandleAsync(InteractionResolution resolution, CancellationToken cancellationToken = default)
        {
            Calls.Add(resolution);
            return ValueTask.CompletedTask;
        }
    }
}
