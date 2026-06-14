using System.Collections.Concurrent;
using VoxelAgentNexus.Core.Memory;

namespace VoxelAgentNexus.Server;

/// <summary>
/// Thread-safe in-memory relationship graph for the server.
/// TODO: persist + encrypt these alongside memories (currently lost on restart).
/// </summary>
public sealed class InMemoryRelationshipGraph : IRelationshipGraph
{
    private readonly ConcurrentDictionary<(string From, string To), RelationshipEdge> _edges = new();

    public ValueTask<RelationshipEdge?> GetEdgeAsync(string fromId, string toId, CancellationToken cancellationToken = default) =>
        ValueTask.FromResult(_edges.TryGetValue((fromId, toId), out var edge) ? edge : null);

    public ValueTask UpsertEdgeAsync(RelationshipEdge edge, CancellationToken cancellationToken = default)
    {
        _edges[(edge.FromId, edge.ToId)] = edge;
        return ValueTask.CompletedTask;
    }

    public ValueTask<IReadOnlyList<RelationshipEdge>> GetEdgesFromAsync(string fromId, CancellationToken cancellationToken = default) =>
        ValueTask.FromResult<IReadOnlyList<RelationshipEdge>>(
            _edges.Values.Where(e => string.Equals(e.FromId, fromId, StringComparison.Ordinal)).ToList());
}
