using System.Text;
using VoxelAgentNexus.Core.Ai;
using VoxelAgentNexus.Core.Memory;

namespace VoxelAgentNexus.Ai;

/// <summary>
/// LLM-backed <see cref="IReflectionSynthesizer"/>. Prompts the model to distill a
/// batch of observations into a few durable, higher-level beliefs — the step that
/// converts accumulated context into understanding. Runs through the same
/// <see cref="INpcAiAdapter"/> as dialogue, so it inherits local/cloud routing.
/// (DESIGN_BRIEF.md §3, §9.)
/// </summary>
public sealed class AiReflectionSynthesizer : IReflectionSynthesizer
{
    private const int MaxBeliefs = 3;

    private static readonly char[] LineSeparators = ['\n', '\r'];

    private readonly INpcAiAdapter _ai;

    public AiReflectionSynthesizer(INpcAiAdapter ai) =>
        _ai = ai ?? throw new ArgumentNullException(nameof(ai));

    /// <inheritdoc />
    public async ValueTask<IReadOnlyList<string>> SynthesizeAsync(
        string npcId,
        IReadOnlyList<MemoryRecord> evidence,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(npcId);
        ArgumentNullException.ThrowIfNull(evidence);

        if (evidence.Count == 0)
        {
            return [];
        }

        var prompt = new StringBuilder();
        prompt.AppendLine(
            "From these recent experiences, state up to three concise, higher-level "
            + "beliefs about people, places, or yourself. One belief per line, no numbering.");
        foreach (var memory in evidence)
        {
            prompt.Append("- ").AppendLine(memory.Content);
        }

        var request = new NpcAiRequest
        {
            NpcId = npcId,
            CacheablePrefix = [AiMessage.System("You are an NPC reflecting on your experiences to form durable beliefs.")],
            Volatile = [AiMessage.User(prompt.ToString())],
            ResponseFormat = AiResponseFormat.Text,
            MaxOutputTokens = 200,
            Temperature = 0.7f,
        };

        var response = await _ai.GenerateAsync(request, cancellationToken);
        return ParseBeliefs(response.Text);
    }

    private static List<string> ParseBeliefs(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return [];
        }

        var beliefs = new List<string>();
        foreach (var rawLine in text.Split(LineSeparators, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            var cleaned = rawLine.TrimStart('-', '*', '•', ' ').Trim();
            if (cleaned.Length > 0)
            {
                beliefs.Add(cleaned);
            }

            if (beliefs.Count == MaxBeliefs)
            {
                break;
            }
        }

        return beliefs;
    }
}
