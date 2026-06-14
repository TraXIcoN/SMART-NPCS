using System.Text;
using VoxelAgentNexus.Core.Ai;
using VoxelAgentNexus.Core.Memory;

namespace VoxelAgentNexus.Ai;

/// <summary>
/// Assembles an <see cref="NpcAiRequest"/> from an NPC's persona, the memories
/// retrieved for the current situation, and recent dialogue. This is the bridge
/// from the (encrypted) memory layer to the AI layer.
///
/// It enforces the cache discipline from DESIGN_BRIEF.md §4: the persona goes in
/// the immutable <see cref="NpcAiRequest.CacheablePrefix"/>, and only the
/// per-turn memories and dialogue go in <see cref="NpcAiRequest.Volatile"/>.
/// </summary>
public static class NpcPromptBuilder
{
    /// <summary>Build a request for one NPC turn.</summary>
    /// <param name="persona">Stable identity (becomes the cache prefix).</param>
    /// <param name="retrieved">Top-k memories from the retriever, most salient first.</param>
    /// <param name="recentTurns">Recent conversation turns, oldest first.</param>
    public static NpcAiRequest Build(
        NpcPersona persona,
        IReadOnlyList<ScoredMemory> retrieved,
        IReadOnlyList<AiMessage> recentTurns,
        NpcSalience salience = NpcSalience.Conversational,
        AiResponseFormat responseFormat = AiResponseFormat.Text,
        int maxOutputTokens = 256)
    {
        ArgumentNullException.ThrowIfNull(persona);
        ArgumentNullException.ThrowIfNull(retrieved);
        ArgumentNullException.ThrowIfNull(recentTurns);

        var prefix = new[] { AiMessage.System(persona.SystemPrompt) };

        var volatileMessages = new List<AiMessage>(retrieved.Count + recentTurns.Count);
        if (retrieved.Count > 0)
        {
            volatileMessages.Add(AiMessage.System(FormatMemories(retrieved)));
        }

        volatileMessages.AddRange(recentTurns);

        return new NpcAiRequest
        {
            NpcId = persona.NpcId,
            CacheablePrefix = prefix,
            Volatile = volatileMessages,
            Salience = salience,
            ResponseFormat = responseFormat,
            MaxOutputTokens = maxOutputTokens,
        };
    }

    private static string FormatMemories(IReadOnlyList<ScoredMemory> retrieved)
    {
        var sb = new StringBuilder();
        sb.AppendLine("Relevant memories (most salient first):");
        foreach (var scored in retrieved)
        {
            sb.Append("- ").AppendLine(scored.Memory.Content);
        }

        return sb.ToString().TrimEnd();
    }
}
