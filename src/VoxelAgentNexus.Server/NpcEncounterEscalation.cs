using Microsoft.AspNetCore.SignalR;
using VoxelAgentNexus.Ai;
using VoxelAgentNexus.Core.Ai;
using VoxelAgentNexus.Core.Memory;
using VoxelAgentNexus.Core.Simulation;

namespace VoxelAgentNexus.Server;

/// <summary>
/// Server-side escalation for NPC↔NPC encounters. When the proximity gate decides
/// two NPCs have a salient meeting, one greets the other through the AI adapter and
/// the line is broadcast + remembered. Generation runs OFF the tick (fire-and-forget)
/// so AI latency never stalls the world loop. (DESIGN_BRIEF.md §2.2, §5.)
/// </summary>
public sealed class NpcEncounterEscalation : IDialogueEscalation
{
    private readonly GameWorld _world;
    private readonly IMemoryStore _memory;
    private readonly INpcAiAdapter _ai;
    private readonly IHubContext<WorldHub> _hub;

    public NpcEncounterEscalation(GameWorld world, IMemoryStore memory, INpcAiAdapter ai, IHubContext<WorldHub> hub)
    {
        _world = world;
        _memory = memory;
        _ai = ai;
        _hub = hub;
    }

    public ValueTask HandleAsync(InteractionResolution resolution, CancellationToken cancellationToken = default)
    {
        _ = GenerateAsync(resolution);
        return ValueTask.CompletedTask;
    }

    private async Task GenerateAsync(InteractionResolution resolution)
    {
        try
        {
            if (!_world.Personas.TryGetValue(resolution.AgentA, out var persona))
            {
                return;
            }

            var otherName = _world.NameOf(resolution.AgentB);
            var workingSet = await _memory.LoadWorkingSetAsync(resolution.AgentA);
            var beliefs = BeliefProjection.TopBeliefs(workingSet, maxBeliefs: 5);

            var request = NpcPromptBuilder.Build(
                persona,
                retrieved: [],
                recentTurns: [AiMessage.User($"You meet {otherName} in the village square. Say one short line to them, in character.")],
                beliefs: beliefs,
                maxOutputTokens: 60);

            var reply = await _ai.GenerateAsync(request);
            var line = string.IsNullOrWhiteSpace(reply.Text) ? "..." : reply.Text;

            await _hub.Clients.All.SendAsync("Dialogue", new DialogueDto(resolution.AgentA, persona.Name, line));

            await _memory.AppendAsync(new MemoryRecord
            {
                Id = Guid.NewGuid(),
                NpcId = resolution.AgentA,
                Kind = MemoryKind.Observation,
                Content = $"I spoke with {otherName} in the square. I said: \"{line}\".",
                Importance = 4f,
                CreatedAt = DateTimeOffset.UtcNow,
                LastAccessedAt = DateTimeOffset.UtcNow,
            });
        }
        catch
        {
            // Background task — never crash the host on a single failed encounter.
        }
    }
}
