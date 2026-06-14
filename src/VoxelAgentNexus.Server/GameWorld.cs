using System.Collections.Concurrent;
using VoxelAgentNexus.Core.Ai;
using VoxelAgentNexus.Core.Memory;
using VoxelAgentNexus.Core.Simulation;
using VoxelAgentNexus.Memory;
using VoxelAgentNexus.Simulation;

namespace VoxelAgentNexus.Server;

/// <summary>A connected player's live state.</summary>
public sealed class PlayerState
{
    public required string Id { get; init; }
    public required string Name { get; init; }
    public VoxelPosition Position { get; set; }
}

/// <summary>
/// The single authoritative shared world. NPCs follow schedules and wander; players
/// join, move, and chat. The tick loop is the only writer of NPC state (single
/// shared runtime, like a16z's AI Town); player input arrives via thread-safe
/// structures. (DESIGN_BRIEF.md §2, §5.)
/// </summary>
public sealed class GameWorld
{
    private readonly IReadOnlyList<WorldNpc> _npcs;
    private readonly WorldClock _clock;
    private readonly MovementSystem _movement;
    private readonly ConcurrentDictionary<string, PlayerState> _players = new(StringComparer.Ordinal);

    private GameWorld(
        IReadOnlyDictionary<string, NpcPersona> personas,
        IReadOnlyList<WorldNpc> npcs,
        WorldClock clock,
        MovementSystem movement)
    {
        Personas = personas;
        _npcs = npcs;
        _clock = clock;
        _movement = movement;
    }

    /// <summary>NPC personas by id.</summary>
    public IReadOnlyDictionary<string, NpcPersona> Personas { get; }

    /// <summary>Seed a small town and return the world.</summary>
    public static async ValueTask<GameWorld> CreateAsync(
        IMemoryStore memory,
        IRelationshipGraph relationships,
        DateTimeOffset now,
        CancellationToken cancellationToken = default)
    {
        var personas = new Dictionary<string, NpcPersona>(StringComparer.Ordinal);
        var npcs = new List<WorldNpc>();
        var seeder = new WorldSeeder(memory, relationships);

        await AddNpcAsync(
            "npc_brom", "Brom",
            "You are Brom, a gruff but warm-hearted blacksmith. You speak plainly, in one or two short sentences, always in character.",
            home: new VoxelPosition(-8, 64, -8), work: new VoxelPosition(6, 64, 4),
            seedMemories: ["I have been the village blacksmith for twenty years."],
            personas, npcs, seeder, now, cancellationToken);

        await AddNpcAsync(
            "npc_mara", "Mara",
            "You are Mara, a curious and kindly herbalist. You speak warmly and briefly, always in character.",
            home: new VoxelPosition(10, 64, -6), work: new VoxelPosition(-4, 64, 8),
            seedMemories: ["I tend the herb garden by the well.", "I am wary of strangers who carry weapons."],
            personas, npcs, seeder, now, cancellationToken);

        var clock = new WorldClock(new TimeOnly(8, 0), gameSecondsPerTick: 30);
        var movement = new MovementSystem(new Random(), stepSize: 1, jitter: 1);
        return new GameWorld(personas, npcs, clock, movement);
    }

    private static async ValueTask AddNpcAsync(
        string id, string name, string systemPrompt,
        VoxelPosition home, VoxelPosition work, IReadOnlyList<string> seedMemories,
        Dictionary<string, NpcPersona> personas, List<WorldNpc> npcs, WorldSeeder seeder,
        DateTimeOffset now, CancellationToken cancellationToken)
    {
        personas[id] = new NpcPersona { NpcId = id, Name = name, SystemPrompt = systemPrompt };

        var schedule = DailySchedule.Create(
        [
            new ScheduleEntry(new TimeOnly(0, 0), Activity.Sleep, home),
            new ScheduleEntry(new TimeOnly(7, 0), Activity.Work, work),
            new ScheduleEntry(new TimeOnly(19, 0), Activity.Socialize, new VoxelPosition(0, 64, 0)),
            new ScheduleEntry(new TimeOnly(22, 0), Activity.Sleep, home),
        ]);

        npcs.Add(new WorldNpc(id, home, schedule));
        await seeder.SeedAsync(new NpcSeed { NpcId = id, SeedMemories = seedMemories }, now, cancellationToken);
    }

    public void AddPlayer(string connectionId, string name) =>
        _players[connectionId] = new PlayerState { Id = connectionId, Name = name, Position = new VoxelPosition(0, 64, 0) };

    public void RemovePlayer(string connectionId) => _players.TryRemove(connectionId, out _);

    public void MovePlayer(string connectionId, VoxelPosition position)
    {
        if (_players.TryGetValue(connectionId, out var player))
        {
            player.Position = position;
        }
    }

    public PlayerState? GetPlayer(string connectionId) =>
        _players.TryGetValue(connectionId, out var player) ? player : null;

    /// <summary>Closest NPC to a position (for directing a player's chat).</summary>
    public WorldNpc? NearestNpc(VoxelPosition position)
    {
        WorldNpc? best = null;
        var bestDistance = long.MaxValue;
        foreach (var npc in _npcs)
        {
            var distance = npc.Position.SquaredDistanceTo(position);
            if (distance < bestDistance)
            {
                bestDistance = distance;
                best = npc;
            }
        }

        return best;
    }

    /// <summary>Advance the world one tick and produce a broadcast snapshot.</summary>
    public SnapshotDto Tick(DateTimeOffset now)
    {
        _clock.Advance();

        foreach (var npc in _npcs)
        {
            var slot = ScheduleSystem.Resolve(npc.Schedule, _clock.TimeOfDay);
            npc.Position = _movement.Step(npc.Position, slot.Location);
        }

        var agents = new List<AgentDto>(_npcs.Count + _players.Count);
        foreach (var npc in _npcs)
        {
            var name = Personas.TryGetValue(npc.Id, out var persona) ? persona.Name : npc.Id;
            agents.Add(new AgentDto(npc.Id, name, "npc", npc.Position.X, npc.Position.Y, npc.Position.Z));
        }

        foreach (var player in _players.Values)
        {
            agents.Add(new AgentDto(player.Id, player.Name, "player", player.Position.X, player.Position.Y, player.Position.Z));
        }

        return new SnapshotDto(_clock.Tick, _clock.TimeOfDay.ToString("HH\\:mm"), agents);
    }
}
