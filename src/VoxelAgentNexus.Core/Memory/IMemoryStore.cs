namespace VoxelAgentNexus.Core.Memory;

/// <summary>
/// Durable, encrypted memory store for one game world. Conceptually append-only
/// for observations; reflections are appended too, and compaction prunes/merges
/// low-importance episodic rows. Implemented by VoxelAgentNexus.Memory on top of
/// <see cref="Security.ICryptoProvider"/> + <see cref="Persistence.IEncryptedStore"/>
/// — it seals records before they reach the store. (DESIGN_BRIEF.md §3, §3.1.)
/// </summary>
public interface IMemoryStore
{
    /// <summary>Seal and persist a new memory.</summary>
    ValueTask AppendAsync(MemoryRecord record, CancellationToken cancellationToken = default);

    /// <summary>Fetch and unseal a single memory by id.</summary>
    ValueTask<MemoryRecord?> GetAsync(string npcId, Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Decrypt an NPC's memories into RAM at session start. This is the bounded,
    /// plaintext working set that retrieval operates over; keep it per-NPC and
    /// lazily loaded so the decrypted footprint stays small. (DESIGN_BRIEF.md §3.1.)
    /// </summary>
    ValueTask<IReadOnlyList<MemoryRecord>> LoadWorkingSetAsync(string npcId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Decay and merge low-importance episodic memories into summaries to bound
    /// growth (the "forgetting" pass). (DESIGN_BRIEF.md §3 housekeeping.)
    /// </summary>
    ValueTask CompactAsync(string npcId, CancellationToken cancellationToken = default);
}
