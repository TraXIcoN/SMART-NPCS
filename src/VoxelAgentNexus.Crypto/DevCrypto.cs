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
}
