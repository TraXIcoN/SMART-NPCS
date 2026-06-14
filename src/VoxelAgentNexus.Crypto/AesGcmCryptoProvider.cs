using System.Security.Cryptography;
using VoxelAgentNexus.Core.Security;

namespace VoxelAgentNexus.Crypto;

/// <summary>
/// Hardware-accelerated AES-256-GCM implementation of <see cref="ICryptoProvider"/>.
/// On Apple Silicon, <see cref="AesGcm"/> uses the CPU's AES instructions.
/// A fresh 96-bit nonce is generated for every seal; a 128-bit tag authenticates
/// both ciphertext and associated data. (DESIGN_BRIEF.md §2.4, §3.1.)
/// </summary>
public sealed class AesGcmCryptoProvider : ICryptoProvider
{
    private const int NonceBytes = 12; // 96-bit GCM nonce
    private const int TagBytes = 16;   // 128-bit GCM tag

    private readonly DevKeyVault _vault;

    internal AesGcmCryptoProvider(DevKeyVault vault) => _vault = vault;

    /// <inheritdoc />
    public ValueTask<SealedBlob> SealAsync(
        ReadOnlyMemory<byte> plaintext,
        ReadOnlyMemory<byte> associatedData = default,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var (keyId, key) = _vault.ActiveKey;
        var nonce = new byte[NonceBytes];
        RandomNumberGenerator.Fill(nonce);

        var ciphertext = new byte[plaintext.Length];
        var tag = new byte[TagBytes];

        using var aes = new AesGcm(key, TagBytes);
        aes.Encrypt(nonce, plaintext.Span, ciphertext, tag, associatedData.Span);

        var blob = new SealedBlob
        {
            KeyId = keyId,
            Nonce = nonce,
            Ciphertext = ciphertext,
            Tag = tag,
            AssociatedData = associatedData,
        };
        return ValueTask.FromResult(blob);
    }

    /// <inheritdoc />
    public ValueTask<ReadOnlyMemory<byte>> UnsealAsync(
        SealedBlob blob,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var key = _vault.Resolve(blob.KeyId)
            ?? throw new CryptographicException($"Unknown key id '{blob.KeyId}'.");

        var plaintext = new byte[blob.Ciphertext.Length];

        using var aes = new AesGcm(key, TagBytes);
        // Throws AuthenticationTagMismatchException on tamper/corruption — never swallowed.
        aes.Decrypt(blob.Nonce.Span, blob.Ciphertext.Span, blob.Tag.Span, plaintext, blob.AssociatedData.Span);

        return ValueTask.FromResult<ReadOnlyMemory<byte>>(plaintext);
    }
}
