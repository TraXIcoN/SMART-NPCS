namespace VoxelAgentNexus.Core.Simulation;

/// <summary>
/// What the rules-of-engagement gate decided to do with a proximity candidate,
/// cheapest first. (DESIGN_BRIEF.md §5.)
/// </summary>
public enum InteractionOutcome
{
    /// <summary>Below simulation fidelity (Far) or on cooldown — nothing happens.</summary>
    Ignored = 0,

    /// <summary>Off-screen (Mid): apply a relationship delta + memory stub, no model call.</summary>
    RelationshipTick = 1,

    /// <summary>Near but not salient: a cheap, templated nicety + relationship delta.</summary>
    TemplatedGreeting = 2,

    /// <summary>Near and salient: hand off to a real dialogue generation call.</summary>
    EscalateToDialogue = 3,
}

/// <summary>
/// The result of evaluating one proximity candidate. Carries enough context for an
/// orchestrator to act (e.g. drive a dialogue call) and for telemetry.
/// </summary>
public sealed record InteractionResolution
{
    /// <summary>First agent (ordinal-lower id of the pair).</summary>
    public required string AgentA { get; init; }

    /// <summary>Second agent.</summary>
    public required string AgentB { get; init; }

    /// <summary>What the gate decided.</summary>
    public required InteractionOutcome Outcome { get; init; }

    /// <summary>The LOD band the pair was evaluated at.</summary>
    public required SimulationLod Lod { get; init; }

    /// <summary>Relationship affinity delta applied by this interaction (0 for escalations/ignores).</summary>
    public float AffinityDelta { get; init; }
}
