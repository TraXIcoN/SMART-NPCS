namespace VoxelAgentNexus.Core.Ai;

/// <summary>
/// Static description of what a concrete adapter supports. Lets the router and
/// context assembler adapt behavior (e.g. skip cache markers on a local backend
/// that doesn't bill tokens). (DESIGN_BRIEF.md §6.)
/// </summary>
public sealed record AiAdapterCapabilities
{
    /// <summary>Human-readable provider/model family name for telemetry.</summary>
    public required string ProviderName { get; init; }

    /// <summary>True when inference runs on-device (no per-token billing, data stays local).</summary>
    public required bool IsLocal { get; init; }

    /// <summary>True when <see cref="INpcAiAdapter.StreamAsync"/> is meaningfully supported.</summary>
    public required bool SupportsStreaming { get; init; }

    /// <summary>True when the backend honors a cacheable prefix for prompt caching.</summary>
    public required bool SupportsPromptCaching { get; init; }

    /// <summary>True when the backend can return validated structured output.</summary>
    public required bool SupportsStructuredOutput { get; init; }

    /// <summary>Maximum total context window in tokens.</summary>
    public required int MaxContextTokens { get; init; }
}

/// <summary>
/// The single abstraction the rest of the game uses to talk to a language model.
///
/// This is the swap point in the Adapter Pattern: a cloud OpenAI-compatible
/// implementation is used for development/quality; an on-device SLM implementation
/// (via MLX) is the shipping target. Game and simulation code depend ONLY on this
/// interface. (DESIGN_BRIEF.md §2.3, §4, §6.)
///
/// Contract:
///  - All members are fully asynchronous; implementations MUST NOT block.
///    They run on the async task pool, never on the render or simulation thread.
///  - Implementations are responsible for translating <see cref="NpcAiRequest"/>
///    into provider calls, applying prompt-cache markers to the cacheable prefix,
///    and parsing output into <see cref="NpcAiResponse"/>.
///  - Callers always have a deterministic fallback and treat failures/timeouts as
///    "no enrichment this tick" rather than a hard error. (DESIGN_BRIEF.md §2.2.)
/// </summary>
public interface INpcAiAdapter
{
    /// <summary>Capabilities of this concrete backend.</summary>
    AiAdapterCapabilities Capabilities { get; }

    /// <summary>
    /// Generate a complete response. Use for structured intents and short lines
    /// where streaming adds no value.
    /// </summary>
    ValueTask<NpcAiResponse> GenerateAsync(
        NpcAiRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Stream a response incrementally. Use for longer player-facing dialogue so
    /// text can surface as it arrives. Implementations that cannot stream should
    /// yield a single final chunk wrapping <see cref="GenerateAsync"/>.
    /// </summary>
    IAsyncEnumerable<NpcAiResponseChunk> StreamAsync(
        NpcAiRequest request,
        CancellationToken cancellationToken = default);
}
