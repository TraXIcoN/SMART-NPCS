using System.Runtime.CompilerServices;
using VoxelAgentNexus.Core.Ai;
using VoxelAgentNexus.Core.Memory;

namespace VoxelAgentNexus.IntegrationTests;

/// <summary>In-memory <see cref="IMemoryStore"/> for tests (no crypto/SQLite).</summary>
internal sealed class FakeMemoryStore : IMemoryStore
{
    public List<MemoryRecord> Records { get; } = [];

    public ValueTask AppendAsync(MemoryRecord record, CancellationToken cancellationToken = default)
    {
        Records.Add(record);
        return ValueTask.CompletedTask;
    }

    public ValueTask<MemoryRecord?> GetAsync(string npcId, Guid id, CancellationToken cancellationToken = default) =>
        ValueTask.FromResult(Records.FirstOrDefault(r => r.NpcId == npcId && r.Id == id));

    public ValueTask<IReadOnlyList<MemoryRecord>> LoadWorkingSetAsync(string npcId, CancellationToken cancellationToken = default) =>
        ValueTask.FromResult<IReadOnlyList<MemoryRecord>>(Records.Where(r => r.NpcId == npcId).ToList());

    public ValueTask CompactAsync(string npcId, CancellationToken cancellationToken = default) => ValueTask.CompletedTask;
}

/// <summary>In-memory <see cref="IRelationshipGraph"/> for tests.</summary>
internal sealed class FakeGraph : IRelationshipGraph
{
    public List<RelationshipEdge> Edges { get; } = [];

    public ValueTask<RelationshipEdge?> GetEdgeAsync(string fromId, string toId, CancellationToken cancellationToken = default) =>
        ValueTask.FromResult(Edges.FirstOrDefault(e => e.FromId == fromId && e.ToId == toId));

    public ValueTask UpsertEdgeAsync(RelationshipEdge edge, CancellationToken cancellationToken = default)
    {
        Edges.RemoveAll(e => e.FromId == edge.FromId && e.ToId == edge.ToId);
        Edges.Add(edge);
        return ValueTask.CompletedTask;
    }

    public ValueTask<IReadOnlyList<RelationshipEdge>> GetEdgesFromAsync(string fromId, CancellationToken cancellationToken = default) =>
        ValueTask.FromResult<IReadOnlyList<RelationshipEdge>>(Edges.Where(e => e.FromId == fromId).ToList());
}

/// <summary>An <see cref="INpcAiAdapter"/> that returns a fixed canned reply.</summary>
internal sealed class StubAiAdapter : INpcAiAdapter
{
    private readonly string _text;

    public StubAiAdapter(string text) => _text = text;

    public AiAdapterCapabilities Capabilities { get; } = new()
    {
        ProviderName = "stub",
        IsLocal = true,
        SupportsStreaming = false,
        SupportsPromptCaching = false,
        SupportsStructuredOutput = false,
        MaxContextTokens = 0,
    };

    public ValueTask<NpcAiResponse> GenerateAsync(NpcAiRequest request, CancellationToken cancellationToken = default) =>
        ValueTask.FromResult(new NpcAiResponse
        {
            Text = _text,
            Usage = new AiTokenUsage(0, 0, 0),
            FinishReason = AiFinishReason.Stop,
            ServedByModel = "stub",
        });

    public async IAsyncEnumerable<NpcAiResponseChunk> StreamAsync(
        NpcAiRequest request,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var response = await GenerateAsync(request, cancellationToken);
        yield return new NpcAiResponseChunk(response.Text ?? string.Empty, IsFinal: true);
    }
}
