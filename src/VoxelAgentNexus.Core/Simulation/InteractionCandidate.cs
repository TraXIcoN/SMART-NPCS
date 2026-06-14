namespace VoxelAgentNexus.Core.Simulation;

/// <summary>
/// Two agents found within interaction range by the spatial index. This is only a
/// <em>candidate</em>: whether it escalates (templated greeting, local resolution,
/// or a cloud dialogue call) is decided downstream by relationship, cooldown,
/// importance, and LOD. (DESIGN_BRIEF.md §5.)
/// </summary>
/// <param name="AgentA">First agent id (ordinal-lower of the pair).</param>
/// <param name="AgentB">Second agent id.</param>
/// <param name="Distance">Euclidean distance between them.</param>
public readonly record struct InteractionCandidate(string AgentA, string AgentB, double Distance);
