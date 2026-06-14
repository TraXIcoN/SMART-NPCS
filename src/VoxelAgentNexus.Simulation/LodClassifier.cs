using VoxelAgentNexus.Core.Simulation;

namespace VoxelAgentNexus.Simulation;

/// <summary>
/// Classifies an NPC's <see cref="SimulationLod"/> by its distance from the player.
/// Near agents are AI-eligible; Mid/Far are resolved locally. (DESIGN_BRIEF.md §5.)
/// </summary>
public sealed class LodClassifier
{
    private readonly long _nearSquared;
    private readonly long _midSquared;

    public LodClassifier(LodThresholds thresholds)
    {
        ArgumentNullException.ThrowIfNull(thresholds);
        if (thresholds.NearRadius < 0 || thresholds.MidRadius < thresholds.NearRadius)
        {
            throw new ArgumentException("Require 0 <= NearRadius <= MidRadius.", nameof(thresholds));
        }

        _nearSquared = (long)thresholds.NearRadius * thresholds.NearRadius;
        _midSquared = (long)thresholds.MidRadius * thresholds.MidRadius;
    }

    /// <summary>Band the agent falls into relative to the player.</summary>
    public SimulationLod Classify(VoxelPosition player, VoxelPosition agent)
    {
        var squared = player.SquaredDistanceTo(agent);
        if (squared <= _nearSquared)
        {
            return SimulationLod.Near;
        }

        return squared <= _midSquared ? SimulationLod.Mid : SimulationLod.Far;
    }
}
