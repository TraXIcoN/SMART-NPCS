using VoxelAgentNexus.Core.Security;

namespace VoxelAgentNexus.Core.Persistence;

/// <summary>
/// Low-level durable store that persists ONLY <see cref="SealedBlob"/>s. This
/// layer has no access to keys and never sees plaintext — the type signature is
/// the enforcement mechanism for "plaintext never touches disk".
/// Implemented by VoxelAgentNexus.Persistence over SQLite. (DESIGN_BRIEF.md §3.1.)
///
/// Records are addressed by (<c>partition</c>, <c>id</c>); a partition groups one
/// NPC's data so it can be lazily loaded as a unit at session start.
/// All operations are async to keep file I/O off the render/simulation threads.
/// </summary>
public interface IEncryptedStore
{
    /// <summary>Insert or replace a sealed record.</summary>
    ValueTask PutAsync(string partition, string id, SealedBlob blob, CancellationToken cancellationToken = default);

    /// <summary>Fetch a single sealed record, or null if absent.</summary>
    ValueTask<SealedBlob?> GetAsync(string partition, string id, CancellationToken cancellationToken = default);

    /// <summary>Stream every sealed record in a partition (e.g. to load an NPC's working set).</summary>
    IAsyncEnumerable<SealedBlob> ScanAsync(string partition, CancellationToken cancellationToken = default);

    /// <summary>Delete a record (used by memory compaction/forgetting).</summary>
    ValueTask DeleteAsync(string partition, string id, CancellationToken cancellationToken = default);
}
