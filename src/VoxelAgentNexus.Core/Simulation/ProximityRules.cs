namespace VoxelAgentNexus.Core.Simulation;

/// <summary>
/// Tunable thresholds for the rules-of-engagement gate. (DESIGN_BRIEF.md §5.)
/// </summary>
public sealed record ProximityRules
{
    /// <summary>Max voxel distance for two agents to count as "interacting".</summary>
    public int InteractionRadius { get; init; } = 4;

    /// <summary>Minimum time between interactions for the same pair (debounce).</summary>
    public TimeSpan Cooldown { get; init; } = TimeSpan.FromSeconds(30);

    /// <summary>Affinity nudge applied by a cheap (non-dialogue) interaction; clamped to [-1, 1].</summary>
    public float RelationshipTickDelta { get; init; } = 0.02f;

    /// <summary>
    /// At Near range, a pair whose existing affinity magnitude meets this threshold
    /// is considered salient and escalated to real dialogue; otherwise it gets a
    /// templated greeting. Models "story-relevant pairs talk; strangers nod".
    /// </summary>
    public float DialogueAffinityThreshold { get; init; } = 0.2f;
}
