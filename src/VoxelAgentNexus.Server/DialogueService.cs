using Microsoft.AspNetCore.SignalR;
using VoxelAgentNexus.Ai;
using VoxelAgentNexus.Core.Ai;
using VoxelAgentNexus.Core.Memory;

namespace VoxelAgentNexus.Server;

/// <summary>
/// Handles a player speaking: echoes the player's line, routes it to the nearest
/// NPC, generates an in-character reply (persona + beliefs + memory via the AI
/// adapter), broadcasts it, and records the exchange in the NPC's encrypted memory.
/// Runs off the world tick, so AI latency never stalls the simulation.
/// (DESIGN_BRIEF.md §2.2, §5.)
/// </summary>
public sealed class DialogueService
{
    private readonly GameWorld _world;
    private readonly IMemoryStore _memory;
    private readonly INpcAiAdapter _ai;
    private readonly IHubContext<WorldHub> _hub;

    public DialogueService(GameWorld world, IMemoryStore memory, INpcAiAdapter ai, IHubContext<WorldHub> hub)
    {
        _world = world;
        _memory = memory;
        _ai = ai;
        _hub = hub;
    }

    public async Task SayAsync(string connectionId, string text, CancellationToken cancellationToken = default)
    {
        var player = _world.GetPlayer(connectionId);
        if (player is null || string.IsNullOrWhiteSpace(text))
        {
            return;
        }

        // Echo the player's line to everyone.
        await _hub.Clients.All.SendAsync("Dialogue", new DialogueDto(player.Id, player.Name, text), cancellationToken);

        var npc = _world.NearestNpc(player.Position);
        if (npc is null || !_world.Personas.TryGetValue(npc.Id, out var persona))
        {
            return;
        }

        var workingSet = await _memory.LoadWorkingSetAsync(npc.Id, cancellationToken);
        var beliefs = BeliefProjection.TopBeliefs(workingSet, maxBeliefs: 5);

        var request = NpcPromptBuilder.Build(
            persona,
            retrieved: [],
            recentTurns: [AiMessage.User($"{player.Name} says to you: \"{text}\". Reply in one short line, in character.")],
            beliefs: beliefs,
            maxOutputTokens: 80);

        var reply = await _ai.GenerateAsync(request, cancellationToken);
        var line = string.IsNullOrWhiteSpace(reply.Text) ? "..." : reply.Text;

        await _hub.Clients.All.SendAsync("Dialogue", new DialogueDto(npc.Id, persona.Name, line), cancellationToken);

        await _memory.AppendAsync(
            new MemoryRecord
            {
                Id = Guid.NewGuid(),
                NpcId = npc.Id,
                Kind = MemoryKind.Observation,
                Content = $"{player.Name} said: \"{text}\". I replied: \"{line}\".",
                Importance = 5f,
                CreatedAt = DateTimeOffset.UtcNow,
                LastAccessedAt = DateTimeOffset.UtcNow,
            },
            cancellationToken);
    }
}
