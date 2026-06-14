using System.Text;
using VoxelAgentNexus.Core.Memory;
using VoxelAgentNexus.Core.Persistence;
using VoxelAgentNexus.Core.Security;

namespace VoxelAgentNexus.Memory;

/// <summary>
/// <see cref="IMemoryStore"/> that seals every record before it reaches the
/// encrypted store and unseals on the way out. This is the only place the two
/// halves meet: <see cref="ICryptoProvider"/> turns a plaintext record into a
/// <see cref="SealedBlob"/>, and <see cref="IEncryptedStore"/> persists only that.
/// Plaintext lives in RAM and never on disk. (DESIGN_BRIEF.md §2.4, §3.1.)
///
/// The NPC id is used as both the store partition and the GCM associated data,
/// binding each blob to its owner.
/// </summary>
public sealed class EncryptedMemoryStore : IMemoryStore
{
    private readonly ICryptoProvider _crypto;
    private readonly IEncryptedStore _store;

    public EncryptedMemoryStore(ICryptoProvider crypto, IEncryptedStore store)
    {
        _crypto = crypto;
        _store = store;
    }

    /// <inheritdoc />
    public async ValueTask AppendAsync(MemoryRecord record, CancellationToken cancellationToken = default)
    {
        var plaintext = MemorySerializer.Serialize(record);
        var aad = Encoding.UTF8.GetBytes($"{record.NpcId}:{record.Id:N}");
        var blob = await _crypto.SealAsync(plaintext, aad, cancellationToken);
        await _store.PutAsync(record.NpcId, record.Id.ToString("N"), blob, cancellationToken);
    }

    /// <inheritdoc />
    public async ValueTask<MemoryRecord?> GetAsync(string npcId, Guid id, CancellationToken cancellationToken = default)
    {
        var blob = await _store.GetAsync(npcId, id.ToString("N"), cancellationToken);
        if (blob is null)
        {
            return null;
        }

        var plaintext = await _crypto.UnsealAsync(blob, cancellationToken);
        return MemorySerializer.Deserialize(plaintext.Span);
    }

    /// <inheritdoc />
    public async ValueTask<IReadOnlyList<MemoryRecord>> LoadWorkingSetAsync(string npcId, CancellationToken cancellationToken = default)
    {
        var records = new List<MemoryRecord>();
        await foreach (var blob in _store.ScanAsync(npcId, cancellationToken))
        {
            var plaintext = await _crypto.UnsealAsync(blob, cancellationToken);
            records.Add(MemorySerializer.Deserialize(plaintext.Span));
        }

        return records;
    }

    /// <inheritdoc />
    public ValueTask CompactAsync(string npcId, CancellationToken cancellationToken = default)
    {
        // TODO: decay + summarize low-importance episodic rows (the "forgetting"
        // pass). Tracked as a follow-up; no-op keeps the slice runnable today.
        cancellationToken.ThrowIfCancellationRequested();
        return ValueTask.CompletedTask;
    }
}
