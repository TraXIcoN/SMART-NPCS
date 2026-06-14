namespace VoxelAgentNexus.Core.Simulation;

/// <summary>
/// The seam between the simulation gate and the (expensive) AI/memory layers. The
/// <see cref="ProximityInteractionSystem"/>-equivalent calls this only for
/// <see cref="InteractionOutcome.EscalateToDialogue"/>, keeping the simulation
/// project free of any direct AI dependency. The composition root supplies an
/// implementation that builds a prompt, calls an <c>INpcAiAdapter</c>, and writes
/// the encrypted memory. (DESIGN_BRIEF.md §2.2, §5.)
/// </summary>
public interface IDialogueEscalation
{
    /// <summary>Handle a salient Near-range interaction (generate dialogue, record memory).</summary>
    ValueTask HandleAsync(InteractionResolution resolution, CancellationToken cancellationToken = default);
}
