namespace VoxelAgentNexus.Core.Security;

/// <summary>
/// An AES-256-GCM encryption envelope. This is the ONLY representation of agent
/// data that crosses the crypto boundary to disk — plaintext never persists.
/// (DESIGN_BRIEF.md §2.4, §3.1.)
///
/// The fields map directly to AES-GCM: a unique <see cref="Nonce"/> per seal, the
/// <see cref="Ciphertext"/>, and the authentication <see cref="Tag"/>.
/// <see cref="AssociatedData"/> is authenticated-but-not-encrypted (AAD) and binds
/// the blob to its context (e.g. record id / partition) so it cannot be silently
/// relocated. <see cref="KeyId"/> records which key sealed it, enabling rotation.
/// </summary>
public sealed record SealedBlob
{
    /// <summary>Identifier of the key that sealed this blob (supports key rotation).</summary>
    public required string KeyId { get; init; }

    /// <summary>96-bit GCM nonce, unique for every seal under a given key.</summary>
    public required ReadOnlyMemory<byte> Nonce { get; init; }

    /// <summary>The encrypted payload.</summary>
    public required ReadOnlyMemory<byte> Ciphertext { get; init; }

    /// <summary>128-bit GCM authentication tag.</summary>
    public required ReadOnlyMemory<byte> Tag { get; init; }

    /// <summary>Optional associated data, authenticated but not encrypted.</summary>
    public ReadOnlyMemory<byte> AssociatedData { get; init; }
}
