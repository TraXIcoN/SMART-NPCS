using System.Security.Cryptography;
using VoxelAgentNexus.Core.Security;

namespace VoxelAgentNexus.Crypto;

/// <summary>
/// Development key vault. Holds AES-256 keys in process memory only.
///
/// PRODUCTION NOTE: this is the dev stand-in for the shipping vault, which will
/// custody keys in the Secure Enclave / Keychain and never materialize raw bytes
/// in managed memory. The <see cref="IKeyVault"/> surface is identical so the
/// swap is a composition-root change. (DESIGN_BRIEF.md §2.4.)
/// </summary>
public sealed class DevKeyVault : IKeyVault
{
    private const int KeySizeBytes = 32; // AES-256
    private readonly Dictionary<string, byte[]> _keys = new(StringComparer.Ordinal);
    private string? _activeKeyId;

    /// <inheritdoc />
    public string ActiveKeyId =>
        _activeKeyId ?? throw new InvalidOperationException("Key vault not initialized.");

    /// <inheritdoc />
    public ValueTask InitializeAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (_activeKeyId is null)
        {
            _activeKeyId = NewKey();
        }

        return ValueTask.CompletedTask;
    }

    /// <inheritdoc />
    public ValueTask<string> RotateAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        _activeKeyId = NewKey();
        return ValueTask.FromResult(_activeKeyId);
    }

    private string NewKey()
    {
        var id = Guid.NewGuid().ToString("N");
        var key = new byte[KeySizeBytes];
        RandomNumberGenerator.Fill(key);
        _keys[id] = key;
        return id;
    }

    /// <summary>Active key material for the crypto provider in this assembly.</summary>
    internal (string KeyId, byte[] Key) ActiveKey => (ActiveKeyId, _keys[ActiveKeyId]);

    /// <summary>Resolve a (possibly rotated-out) key by id, or null if unknown.</summary>
    internal byte[]? Resolve(string keyId) => _keys.TryGetValue(keyId, out var key) ? key : null;
}
