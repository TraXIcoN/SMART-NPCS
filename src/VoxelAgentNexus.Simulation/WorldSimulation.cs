using VoxelAgentNexus.Core.Simulation;

namespace VoxelAgentNexus.Simulation;

/// <summary>The outcome of advancing the world by one tick.</summary>
/// <param name="Tick">Tick number.</param>
/// <param name="TimeOfDay">In-game time after this tick.</param>
/// <param name="Interactions">Proximity interactions resolved this tick.</param>
public readonly record struct TickResult(
    long Tick,
    TimeOnly TimeOfDay,
    IReadOnlyList<InteractionResolution> Interactions);

/// <summary>
/// Headless fixed-step world loop. Each tick it: advances the clock, resolves each
/// NPC's schedule target and moves toward it, snapshots positions, then runs the
/// proximity gate. No rendering — this proves the simulation systems compose over
/// many ticks, and (given a fixed seed) reproduces exactly. (DESIGN_BRIEF.md §2.2, §9.)
/// </summary>
public sealed class WorldSimulation
{
    private readonly IReadOnlyList<WorldNpc> _npcs;
    private readonly WorldClock _clock;
    private readonly MovementSystem _movement;
    private readonly ProximityInteractionSystem _interactions;
    private readonly DateTimeOffset _startInstant;
    private readonly TimeSpan _tickDuration;

    private VoxelPosition _playerPosition;

    public WorldSimulation(
        IReadOnlyList<WorldNpc> npcs,
        WorldClock clock,
        MovementSystem movement,
        ProximityInteractionSystem interactions,
        VoxelPosition playerPosition,
        DateTimeOffset startInstant,
        TimeSpan tickDuration)
    {
        _npcs = npcs ?? throw new ArgumentNullException(nameof(npcs));
        _clock = clock ?? throw new ArgumentNullException(nameof(clock));
        _movement = movement ?? throw new ArgumentNullException(nameof(movement));
        _interactions = interactions ?? throw new ArgumentNullException(nameof(interactions));
        _playerPosition = playerPosition;
        _startInstant = startInstant;
        _tickDuration = tickDuration;
    }

    /// <summary>The in-game clock.</summary>
    public WorldClock Clock => _clock;

    /// <summary>Move the player (affects LOD classification of every pair).</summary>
    public void SetPlayerPosition(VoxelPosition position) => _playerPosition = position;

    /// <summary>Advance the world by one tick.</summary>
    public async ValueTask<TickResult> TickAsync(CancellationToken cancellationToken = default)
    {
        _clock.Advance();

        // 1. Schedule → target → move (deterministic system, zero token cost).
        foreach (var npc in _npcs)
        {
            var slot = ScheduleSystem.Resolve(npc.Schedule, _clock.TimeOfDay);
            npc.Position = _movement.Step(npc.Position, slot.Location);
        }

        // 2. Publish the immutable snapshot the gate reads.
        var snapshots = new List<AgentSnapshot>(_npcs.Count);
        foreach (var npc in _npcs)
        {
            snapshots.Add(new AgentSnapshot(npc.Id, npc.Position));
        }

        // 3. Proximity interactions, using synthetic (reproducible) time for cooldowns.
        var now = _startInstant + (_tickDuration * _clock.Tick);
        var interactions = await _interactions.TickAsync(snapshots, _playerPosition, now, cancellationToken);

        return new TickResult(_clock.Tick, _clock.TimeOfDay, interactions);
    }
}
