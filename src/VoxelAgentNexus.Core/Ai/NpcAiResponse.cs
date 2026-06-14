namespace VoxelAgentNexus.Core.Ai;

/// <summary>Why generation stopped.</summary>
public enum AiFinishReason
{
    Stop = 0,
    MaxTokens = 1,
    ContentFilter = 2,
    Error = 3,
}

/// <summary>
/// Token accounting for a single call. <see cref="CachedInputTokens"/> are billed
/// at the cached rate (~10% of base); surfacing them lets us verify prompt-cache
/// hit rates in telemetry. (DESIGN_BRIEF.md §4.)
/// </summary>
public readonly record struct AiTokenUsage(
    int InputTokens,
    int CachedInputTokens,
    int OutputTokens)
{
    public int TotalTokens => InputTokens + OutputTokens;
}

/// <summary>
/// A compact, structured NPC decision. Preferred over prose for non-dialogue
/// outcomes because it is cheap to emit and trivial to validate/clamp before it
/// touches the simulation. (DESIGN_BRIEF.md §4, §5 guardrail.)
/// </summary>
public sealed record NpcIntent
{
    /// <summary>Verb the NPC wants to perform (e.g. "greet", "trade", "flee").</summary>
    public required string Action { get; init; }

    /// <summary>Optional target entity id (NPC or player).</summary>
    public string? TargetId { get; init; }

    /// <summary>Optional spoken line, if the action involves speech.</summary>
    public string? Dialogue { get; init; }

    /// <summary>
    /// Optional relationship delta toward the target, clamped by the simulation
    /// before application. Keeps emergent outcomes bounded (Radiant AI lesson).
    /// </summary>
    public float? RelationshipDelta { get; init; }
}

/// <summary>A completed response from an <see cref="INpcAiAdapter"/>.</summary>
public sealed record NpcAiResponse
{
    /// <summary>Free-form text, present when <see cref="AiResponseFormat.Text"/> was requested.</summary>
    public string? Text { get; init; }

    /// <summary>Parsed structured intent, present when <see cref="AiResponseFormat.StructuredIntent"/> was requested.</summary>
    public NpcIntent? Intent { get; init; }

    /// <summary>Token accounting for cost/telemetry.</summary>
    public required AiTokenUsage Usage { get; init; }

    /// <summary>Why generation stopped.</summary>
    public required AiFinishReason FinishReason { get; init; }

    /// <summary>Identifier of the backend that actually served the request (for routing telemetry).</summary>
    public required string ServedByModel { get; init; }
}

/// <summary>An incremental chunk emitted while streaming a response.</summary>
/// <param name="TextDelta">Newly generated text fragment.</param>
/// <param name="IsFinal">True for the terminal chunk.</param>
public readonly record struct NpcAiResponseChunk(string TextDelta, bool IsFinal);
