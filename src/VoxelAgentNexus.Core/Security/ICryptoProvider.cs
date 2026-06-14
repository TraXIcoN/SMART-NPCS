namespace VoxelAgentNexus.Core.Security;

/// <summary>
/// Seals and unseals data with hardware-accelerated AES-256-GCM. The active key
/// is supplied by an <see cref="IKeyVault"/>; raw key material is never exposed
/// through this interface. Implemented by VoxelAgentNexus.Crypto.
/// (DESIGN_BRIEF.md §2.4, §3.1.)
///
/// Contract:
///  - <see cref="SealAsync"/> MUST generate a fresh, unique nonce per call.
///  - <see cref="UnsealAsync"/> MUST fail (throw) on tag mismatch — a failed
///    authentication is a tamper/corruption signal, never silently ignored.
///  - Both run on the async task pool, off the render/simulation threads.
/// </summary>
public interface ICryptoProvider
{
    /// <summary>Encrypt and authenticate <paramref name="plaintext"/> under the active key.</summary>
    /// <param name="plaintext">Bytes to protect.</param>
    /// <param name="associatedData">Optional AAD to bind (e.g. record id); not encrypted.</param>
    ValueTask<SealedBlob> SealAsync(
        ReadOnlyMemory<byte> plaintext,
        ReadOnlyMemory<byte> associatedData = default,
        CancellationToken cancellationToken = default);

    /// <summary>Verify and decrypt a <see cref="SealedBlob"/>. Throws on authentication failure.</summary>
    ValueTask<ReadOnlyMemory<byte>> UnsealAsync(
        SealedBlob blob,
        CancellationToken cancellationToken = default);
}
