namespace VoxelAgentNexus.Core.Simulation;

/// <summary>
/// An immutable per-tick view of one agent's identity and position — the input to
/// the proximity system. Mirrors the "render reads a snapshot" pattern: systems
/// consume snapshots, never live mutable agent state. (DESIGN_BRIEF.md §2.1, §5.)
/// </summary>
/// <param name="Id">Agent id (NPC id; the player can be included too).</param>
/// <param name="Position">Current voxel position.</param>
public readonly record struct AgentSnapshot(string Id, VoxelPosition Position);
