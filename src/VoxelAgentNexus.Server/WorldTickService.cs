using Microsoft.AspNetCore.SignalR;
using VoxelAgentNexus.Core.Ai;
using VoxelAgentNexus.Core.Memory;
using VoxelAgentNexus.Core.Simulation;
using VoxelAgentNexus.Simulation;

namespace VoxelAgentNexus.Server;

/// <summary>
/// The single authoritative tick loop. Advances the world ~10x/second and broadcasts
/// a snapshot. Every few ticks it also runs the proximity interaction gate over the
/// NPCs (when a player is around to witness it): low-affinity meetings get an ambient
/// templated nod + relationship tick, and salient ones escalate to real AI dialogue —
/// so relationships and NPC↔NPC conversation emerge as the town roams. (DESIGN_BRIEF.md §5.)
/// </summary>
public sealed class WorldTickService : BackgroundService
{
    private static readonly TimeSpan TickInterval = TimeSpan.FromMilliseconds(100);
    private const int InteractionEveryNTicks = 5; // run the gate at ~2 Hz

    private readonly GameWorld _world;
    private readonly IHubContext<WorldHub> _hub;
    private readonly ProximityInteractionSystem _interactions;

    public WorldTickService(
        GameWorld world,
        IHubContext<WorldHub> hub,
        IRelationshipGraph relationships,
        IMemoryStore memory,
        INpcAiAdapter ai)
    {
        _world = world;
        _hub = hub;

        var escalation = new NpcEncounterEscalation(world, memory, ai, hub);
        _interactions = new ProximityInteractionSystem(
            new SpatialHashGrid(cellSize: 16),
            new LodClassifier(new LodThresholds { NearRadius = 12, MidRadius = 40 }),
            relationships,
            new ProximityRules(),
            escalation);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TickInterval);
        long n = 0;
        try
        {
            while (await timer.WaitForNextTickAsync(stoppingToken))
            {
                var now = DateTimeOffset.UtcNow;
                var snapshot = _world.Tick(now);
                await _hub.Clients.All.SendAsync("Snapshot", snapshot, stoppingToken);

                if (n++ % InteractionEveryNTicks == 0 && _world.FirstPlayerPosition() is { } playerPos)
                {
                    var resolutions = await _interactions.TickAsync(_world.NpcSnapshots(), playerPos, now, stoppingToken);
                    await BroadcastAmbientAsync(resolutions, stoppingToken);
                }
            }
        }
        catch (OperationCanceledException)
        {
            // Normal shutdown.
        }
    }

    // Templated (non-AI) greetings give the town ambient life immediately; salient
    // encounters are handled by the AI escalation handler, which broadcasts itself.
    private async Task BroadcastAmbientAsync(IReadOnlyList<InteractionResolution> resolutions, CancellationToken cancellationToken)
    {
        foreach (var r in resolutions)
        {
            if (r.Outcome == InteractionOutcome.TemplatedGreeting)
            {
                var line = $"*nods to {_world.NameOf(r.AgentB)}*";
                await _hub.Clients.All.SendAsync("Dialogue", new DialogueDto(r.AgentA, _world.NameOf(r.AgentA), line), cancellationToken);
            }
        }
    }
}
