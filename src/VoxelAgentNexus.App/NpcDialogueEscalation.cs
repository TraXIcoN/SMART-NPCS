using VoxelAgentNexus.Ai;
using VoxelAgentNexus.Core.Ai;
using VoxelAgentNexus.Core.Memory;
using VoxelAgentNexus.Core.Simulation;

namespace VoxelAgentNexus.App;

/// <summary>
/// Composition-root glue that closes the loop: when the proximity gate escalates a
/// Near-range interaction, this builds a prompt for the speaker, calls the AI
/// adapter, and writes the exchange back into encrypted memory. Lives in App
/// because only the composition root knows the persona registry and concrete
/// adapter/store. (DESIGN_BRIEF.md §5.)
/// </summary>
internal sealed class NpcDialogueEscalation : IDialogueEscalation
{
    private readonly IReadOnlyDictionary<string, NpcPersona> _personas;
    private readonly INpcAiAdapter _ai;
    private readonly IMemoryStore _memory;
    private readonly Action<string>? _log;

    public NpcDialogueEscalation(
        IReadOnlyDictionary<string, NpcPersona> personas,
        INpcAiAdapter ai,
        IMemoryStore memory,
        Action<string>? log = null)
    {
        _personas = personas;
        _ai = ai;
        _memory = memory;
        _log = log;
    }

    public async ValueTask HandleAsync(InteractionResolution resolution, CancellationToken cancellationToken = default)
    {
        // AgentA addresses AgentB. Without a persona for the speaker, we can't talk.
        if (!_personas.TryGetValue(resolution.AgentA, out var persona))
        {
            return;
        }

        var workingSet = await _memory.LoadWorkingSetAsync(resolution.AgentA, cancellationToken);
        var beliefs = BeliefProjection.TopBeliefs(workingSet, maxBeliefs: 5);

        var prompt = NpcPromptBuilder.Build(
            persona,
            retrieved: [],
            recentTurns: [AiMessage.User($"You cross paths with {resolution.AgentB}. Greet them in one short line, in character.")],
            beliefs: beliefs,
            maxOutputTokens: 64);

        var reply = await _ai.GenerateAsync(prompt, cancellationToken);
        var line = reply.Text ?? string.Empty;
        _log?.Invoke($"{persona.Name} -> {resolution.AgentB}: {line}");

        await _memory.AppendAsync(
            new MemoryRecord
            {
                Id = Guid.NewGuid(),
                NpcId = resolution.AgentA,
                Kind = MemoryKind.Observation,
                Content = $"I crossed paths with {resolution.AgentB} and said: \"{line}\".",
                Importance = 3f,
                CreatedAt = DateTimeOffset.UtcNow,
                LastAccessedAt = DateTimeOffset.UtcNow,
            },
            cancellationToken);
    }
}
