using VoxelAgentNexus.Core.Memory;
using VoxelAgentNexus.Core.Simulation;

namespace VoxelAgentNexus.Simulation;

/// <summary>
/// The "rules of engagement" gate. Each tick it rebuilds the spatial grid from the
/// agent snapshots, finds proximity candidates, and for each decides an outcome by
/// LOD + per-pair cooldown + relationship:
///
///   Far              → Ignored (below simulation fidelity)
///   Mid              → RelationshipTick   (cheap: affinity delta, no model)
///   Near, not salient → TemplatedGreeting (cheap: affinity delta, no model)
///   Near, salient    → EscalateToDialogue (hand off to <see cref="IDialogueEscalation"/>)
///
/// This is the single place simulation, AI, and memory meet — and it depends only
/// on Core abstractions, so the expensive layers stay swappable. Relationship
/// deltas are clamped (the Radiant AI guardrail). (DESIGN_BRIEF.md §5.)
/// </summary>
public sealed class ProximityInteractionSystem
{
    private readonly SpatialHashGrid _grid;
    private readonly LodClassifier _lod;
    private readonly IRelationshipGraph _relationships;
    private readonly ProximityRules _rules;
    private readonly IDialogueEscalation? _escalation;
    private readonly Dictionary<(string, string), DateTimeOffset> _lastInteraction = new();

    public ProximityInteractionSystem(
        SpatialHashGrid grid,
        LodClassifier lod,
        IRelationshipGraph relationships,
        ProximityRules rules,
        IDialogueEscalation? escalation = null)
    {
        _grid = grid ?? throw new ArgumentNullException(nameof(grid));
        _lod = lod ?? throw new ArgumentNullException(nameof(lod));
        _relationships = relationships ?? throw new ArgumentNullException(nameof(relationships));
        _rules = rules ?? throw new ArgumentNullException(nameof(rules));
        _escalation = escalation;
    }

    /// <summary>Evaluate all proximity interactions for one simulation tick.</summary>
    public async ValueTask<IReadOnlyList<InteractionResolution>> TickAsync(
        IReadOnlyList<AgentSnapshot> agents,
        VoxelPosition playerPosition,
        DateTimeOffset now,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(agents);

        _grid.Clear();
        var positions = new Dictionary<string, VoxelPosition>(agents.Count, StringComparer.Ordinal);
        foreach (var agent in agents)
        {
            _grid.Insert(agent.Id, agent.Position);
            positions[agent.Id] = agent.Position;
        }

        var resolutions = new List<InteractionResolution>();
        foreach (var pair in _grid.FindProximityPairs(_rules.InteractionRadius))
        {
            cancellationToken.ThrowIfCancellationRequested();

            var lod = Nearer(
                _lod.Classify(playerPosition, positions[pair.AgentA]),
                _lod.Classify(playerPosition, positions[pair.AgentB]));

            if (lod == SimulationLod.Far)
            {
                continue; // not simulated at interaction fidelity
            }

            var key = (pair.AgentA, pair.AgentB); // already ordinal-normalized by the grid
            if (_lastInteraction.TryGetValue(key, out var last) && now - last < _rules.Cooldown)
            {
                continue; // debounced
            }

            _lastInteraction[key] = now;

            var resolution = await ResolveAsync(pair.AgentA, pair.AgentB, lod, now, cancellationToken);

            if (resolution.Outcome == InteractionOutcome.EscalateToDialogue && _escalation is not null)
            {
                await _escalation.HandleAsync(resolution, cancellationToken);
            }

            resolutions.Add(resolution);
        }

        return resolutions;
    }

    private async ValueTask<InteractionResolution> ResolveAsync(
        string a, string b, SimulationLod lod, DateTimeOffset now, CancellationToken cancellationToken)
    {
        InteractionOutcome outcome;
        var appliedDelta = 0f;

        if (lod == SimulationLod.Mid)
        {
            outcome = InteractionOutcome.RelationshipTick;
            appliedDelta = await BumpAffinityAsync(a, b, now, cancellationToken);
        }
        else // Near
        {
            var affinity = await CurrentAffinityAsync(a, b, cancellationToken);
            if (Math.Abs(affinity) >= _rules.DialogueAffinityThreshold)
            {
                outcome = InteractionOutcome.EscalateToDialogue;
            }
            else
            {
                outcome = InteractionOutcome.TemplatedGreeting;
                appliedDelta = await BumpAffinityAsync(a, b, now, cancellationToken);
            }
        }

        return new InteractionResolution
        {
            AgentA = a,
            AgentB = b,
            Outcome = outcome,
            Lod = lod,
            AffinityDelta = appliedDelta,
        };
    }

    private async ValueTask<float> CurrentAffinityAsync(string a, string b, CancellationToken cancellationToken)
    {
        var edge = await _relationships.GetEdgeAsync(a, b, cancellationToken);
        return edge?.Affinity ?? 0f;
    }

    private async ValueTask<float> BumpAffinityAsync(string a, string b, DateTimeOffset now, CancellationToken cancellationToken)
    {
        await BumpDirectedAsync(a, b, now, cancellationToken);
        await BumpDirectedAsync(b, a, now, cancellationToken);
        return _rules.RelationshipTickDelta;
    }

    private async ValueTask BumpDirectedAsync(string from, string to, DateTimeOffset now, CancellationToken cancellationToken)
    {
        var edge = await _relationships.GetEdgeAsync(from, to, cancellationToken);
        var affinity = Math.Clamp((edge?.Affinity ?? 0f) + _rules.RelationshipTickDelta, -1f, 1f);

        await _relationships.UpsertEdgeAsync(
            new RelationshipEdge
            {
                FromId = from,
                ToId = to,
                Affinity = affinity,
                Trust = edge?.Trust ?? 0f,
                UpdatedAt = now,
            },
            cancellationToken);
    }

    private static SimulationLod Nearer(SimulationLod x, SimulationLod y) =>
        (int)x <= (int)y ? x : y;
}
