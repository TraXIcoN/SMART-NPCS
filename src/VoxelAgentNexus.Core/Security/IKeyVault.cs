namespace VoxelAgentNexus.Core.Security;

/// <summary>
/// Custody of the data-encryption keys. Backed by the Secure Enclave / Keychain
/// so key material stays in secure hardware; the rest of the system references
/// keys only by <see cref="ActiveKeyId"/>. Implemented by VoxelAgentNexus.Crypto.
/// (DESIGN_BRIEF.md §2.4.)
/// </summary>
public interface IKeyVault
{
    /// <summary>Id of the key currently used for new seals.</summary>
    string ActiveKeyId { get; }

    /// <summary>
    /// Unlock / derive keys at session start (e.g. via biometric or device key).
    /// Until this completes, no seal or unseal can occur.
    /// </summary>
    ValueTask InitializeAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Introduce a new active key for forward rotation. Existing blobs remain
    /// readable via their recorded <see cref="SealedBlob.KeyId"/>.
    /// </summary>
    ValueTask<string> RotateAsync(CancellationToken cancellationToken = default);
}
