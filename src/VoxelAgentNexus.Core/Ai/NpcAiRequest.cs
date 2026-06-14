namespace VoxelAgentNexus.Core.Ai;

/// <summary>
/// Role of a message in an NPC AI exchange. Mirrors the OpenAI-compatible chat
/// schema so a single adapter shape works against cloud providers and local
/// runtimes (llama.cpp, Ollama, LM Studio, MLX) alike. (DESIGN_BRIEF.md §6.)
/// </summary>
public enum AiRole
{
    System,
    User,
    Assistant,
    Tool,
}

/// <summary>A single message in a chat-style request.</summary>
/// <param name="Role">Who authored the message.</param>
/// <param name="Content">Natural-language content.</param>
public readonly record struct AiMessage(AiRole Role, string Content)
{
    public static AiMessage System(string content) => new(AiRole.System, content);
    public static AiMessage User(string content) => new(AiRole.User, content);
    public static AiMessage Assistant(string content) => new(AiRole.Assistant, content);
}

/// <summary>
/// How important/visible this interaction is, used to route to the cheapest
/// capable backend. (DESIGN_BRIEF.md §4 "salience routing".)
/// </summary>
public enum NpcSalience
{
    /// <summary>Filler ("hello traveler"). Templated or tiniest local model.</summary>
    Ambient = 0,

    /// <summary>Off-screen / mid-LOD. Local model or statistical resolution.</summary>
    Background = 1,

    /// <summary>Live, player-facing conversation. Local SLM or cloud.</summary>
    Conversational = 2,

    /// <summary>Story-relevant, named NPC. Cloud frontier permitted.</summary>
    Story = 3,
}

/// <summary>Desired shape of the model output.</summary>
public enum AiResponseFormat
{
    /// <summary>Free-form dialogue text.</summary>
    Text = 0,

    /// <summary>A compact structured <see cref="NpcIntent"/> (keeps output tokens low).</summary>
    StructuredIntent = 1,
}

/// <summary>
/// A single request to an <see cref="INpcAiAdapter"/>.
///
/// The split between <see cref="CacheablePrefix"/> and <see cref="Volatile"/> is
/// load-bearing: the prefix is the immutable persona/lore/rules block that an
/// adapter may mark for prompt caching, and it MUST be byte-for-byte identical
/// across calls for the same NPC or the cache silently invalidates
/// ("don't break the cache" — DESIGN_BRIEF.md §4). Only <see cref="Volatile"/>
/// (retrieved memories + recent turns) changes per turn.
/// </summary>
public sealed record NpcAiRequest
{
    /// <summary>The NPC this request is for. Used for routing, logging, correlation.</summary>
    public required string NpcId { get; init; }

    /// <summary>
    /// Immutable, cache-friendly prefix: persona, world lore, rules of
    /// engagement, output-format examples. Never reorder or append mid-session.
    /// </summary>
    public required IReadOnlyList<AiMessage> CacheablePrefix { get; init; }

    /// <summary>
    /// Per-turn content placed AFTER the prefix: top-k retrieved memories,
    /// rolling conversation summary, and the last few dialogue turns.
    /// </summary>
    public required IReadOnlyList<AiMessage> Volatile { get; init; }

    /// <summary>Drives backend selection (cheapest capable wins).</summary>
    public NpcSalience Salience { get; init; } = NpcSalience.Conversational;

    /// <summary>Requested output shape.</summary>
    public AiResponseFormat ResponseFormat { get; init; } = AiResponseFormat.Text;

    /// <summary>Hard cap on output tokens. Output costs ~4-5x input, so keep tight.</summary>
    public int MaxOutputTokens { get; init; } = 256;

    /// <summary>Sampling temperature (0-2). Lower = more consistent in-character.</summary>
    public float Temperature { get; init; } = 0.8f;

    /// <summary>Optional explicit model override; null lets the router decide.</summary>
    public string? ModelHint { get; init; }
}
