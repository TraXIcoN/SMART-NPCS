using System.Runtime.CompilerServices;
using VoxelAgentNexus.Core.Ai;

namespace VoxelAgentNexus.Ai;

/// <summary>
/// A deterministic, offline <see cref="INpcAiAdapter"/> that requires no model or
/// network. It exists to embody the core rule from DESIGN_BRIEF.md §2.2: an NPC
/// ALWAYS has a fallback and is never frozen waiting on AI. Use it when no
/// endpoint is configured, or as the behavior an orchestrator falls back to when
/// a real adapter times out.
/// </summary>
public sealed class ScriptedFallbackAdapter : INpcAiAdapter
{
    /// <inheritdoc />
    public AiAdapterCapabilities Capabilities { get; } = new()
    {
        ProviderName = "scripted-fallback",
        IsLocal = true,
        SupportsStreaming = true,
        SupportsPromptCaching = false,
        SupportsStructuredOutput = false,
        MaxContextTokens = 0,
    };

    /// <inheritdoc />
    public ValueTask<NpcAiResponse> GenerateAsync(NpcAiRequest request, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ArgumentNullException.ThrowIfNull(request);

        // No model: acknowledge in-character and signal that recall is intact.
        var text = "*nods slowly* Aye, I know you. "
            + "(scripted fallback — set NEXUS_AI_BASE_URL and NEXUS_AI_MODEL for real dialogue)";

        var response = new NpcAiResponse
        {
            Text = text,
            Usage = new AiTokenUsage(0, 0, 0),
            FinishReason = AiFinishReason.Stop,
            ServedByModel = Capabilities.ProviderName,
        };
        return ValueTask.FromResult(response);
    }

    /// <inheritdoc />
    public async IAsyncEnumerable<NpcAiResponseChunk> StreamAsync(
        NpcAiRequest request,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var response = await GenerateAsync(request, cancellationToken);
        yield return new NpcAiResponseChunk(response.Text ?? string.Empty, IsFinal: true);
    }
}
