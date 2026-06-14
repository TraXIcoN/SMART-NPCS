namespace VoxelAgentNexus.Ai;

/// <summary>
/// Configuration for <see cref="OpenAiCompatibleAdapter"/>. The same shape points
/// at OpenAI, Anthropic-compatible gateways, OpenRouter, or a local server
/// (Ollama / LM Studio / an MLX server) — the swap is config, not code.
/// (DESIGN_BRIEF.md §6.)
/// </summary>
public sealed record OpenAiAdapterOptions
{
    /// <summary>Base URL including the API version segment, e.g. https://api.openai.com/v1 .</summary>
    public required Uri BaseUrl { get; init; }

    /// <summary>Model identifier to request.</summary>
    public required string Model { get; init; }

    /// <summary>Bearer API key. Omit for unauthenticated local servers.</summary>
    public string? ApiKey { get; init; }

    /// <summary>Provider name for telemetry.</summary>
    public string ProviderName { get; init; } = "openai-compatible";

    /// <summary>True when this endpoint is an on-device server (no per-token billing).</summary>
    public bool IsLocal { get; init; }

    /// <summary>Whether the backend honors prefix prompt caching.</summary>
    public bool SupportsPromptCaching { get; init; } = true;

    /// <summary>Backend context window in tokens.</summary>
    public int MaxContextTokens { get; init; } = 128_000;
}
