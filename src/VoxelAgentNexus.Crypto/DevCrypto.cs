using System.Security.Cryptography;
using VoxelAgentNexus.Core.Security;

namespace VoxelAgentNexus.Crypto;

/// <summary>
/// Composition helper that wires a <see cref="DevKeyVault"/> to an
/// <see cref="AesGcmCryptoProvider"/>, keeping raw key access internal to this
/// assembly. The shipping build will offer an equivalent factory backed by the
/// Secure Enclave. (DESIGN_BRIEF.md §2.4.)
/// </summary>
public static class DevCrypto
{
    /// <summary>Create an initialized key vault and a crypto provider bound to it.</summary>
    public static async ValueTask<(IKeyVault Vault, ICryptoProvider Crypto)> CreateAsync(
        CancellationToken cancellationToken = default)
    {
        var vault = new DevKeyVault();
        await vault.InitializeAsync(cancellationToken);
        return (vault, new AesGcmCryptoProvider(vault));
    }

    /// <summary>
    /// Create a provider whose key is loaded from (or generated into) a key file,
    /// so data sealed in one run can be unsealed in the next.
    ///
    /// DEV-ONLY convenience: a plaintext key file is NOT real E2EE — it stands in
    /// for the Secure Enclave during local development and demos. Keep the file
    /// out of version control. (DESIGN_BRIEF.md §2.4.)
    /// </summary>
    public static async ValueTask<(IKeyVault Vault, ICryptoProvider Crypto)> CreatePersistentAsync(
        string keyFilePath,
        CancellationToken cancellationToken = default)
    {
        const string keyId = "dev-key-0";

        byte[] key;
        if (File.Exists(keyFilePath))
        {
            key = await File.ReadAllBytesAsync(keyFilePath, cancellationToken);
        }
        else
        {
            key = RandomNumberGenerator.GetBytes(32);
            var dir = Path.GetDirectoryName(keyFilePath);
            if (!string.IsNullOrEmpty(dir))
            {
                Directory.CreateDirectory(dir);
            }

            await File.WriteAllBytesAsync(keyFilePath, key, cancellationToken);
        }

        var vault = new DevKeyVault();
        vault.ImportActiveKey(keyId, key);
        return (vault, new AesGcmCryptoProvider(vault));
    }
}
