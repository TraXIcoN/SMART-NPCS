namespace VoxelAgentNexus.Core.Simulation;

/// <summary>
/// Simulation fidelity for an NPC, tied to distance from the player. This is the
/// lever that bounds AI cost to "NPCs near the player". (DESIGN_BRIEF.md §5.)
/// </summary>
public enum SimulationLod
{
    /// <summary>On-screen / nearby. Full behavior; cloud-eligible dialogue.</summary>
    Near = 0,

    /// <summary>Loaded but off-screen. Schedule + proximity local-only; no model calls.</summary>
    Mid = 1,

    /// <summary>Unloaded. Fast-forwarded abstractly on chunk reload.</summary>
    Far = 2,
}

/// <summary>Radii (in voxels) that separate the LOD bands. Near ≤ Mid.</summary>
public sealed record LodThresholds
{
    /// <summary>Inclusive radius of the Near band.</summary>
    public required int NearRadius { get; init; }

    /// <summary>Inclusive radius of the Mid band; beyond it is Far.</summary>
    public required int MidRadius { get; init; }
}
