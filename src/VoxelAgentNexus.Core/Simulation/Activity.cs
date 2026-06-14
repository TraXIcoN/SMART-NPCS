namespace VoxelAgentNexus.Core.Simulation;

/// <summary>
/// What an NPC is doing in its daily routine. Driving NPC life from this cheap,
/// deterministic enum (rather than the LLM) is what makes hundreds of roaming
/// agents affordable. (DESIGN_BRIEF.md §5.)
/// </summary>
public enum Activity
{
    Idle = 0,
    Sleep = 1,
    Commute = 2,
    Work = 3,
    Eat = 4,
    Socialize = 5,
}
