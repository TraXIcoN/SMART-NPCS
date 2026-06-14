using System.Data.Common;
using System.Runtime.CompilerServices;
using Microsoft.Data.Sqlite;
using VoxelAgentNexus.Core.Persistence;
using VoxelAgentNexus.Core.Security;

namespace VoxelAgentNexus.Persistence;

/// <summary>
/// SQLite-backed <see cref="IEncryptedStore"/>. Persists ONLY <see cref="SealedBlob"/>
/// columns — this layer has no keys and never sees plaintext. Each record is
/// addressed by (partition, id); partition groups one NPC's data for lazy load.
/// (DESIGN_BRIEF.md §3.1.)
/// </summary>
public sealed class SqliteEncryptedStore : IEncryptedStore, IAsyncDisposable
{
    private readonly SqliteConnection _connection;

    private SqliteEncryptedStore(SqliteConnection connection) => _connection = connection;

    /// <summary>Open (creating if needed) an encrypted store at the given file path.</summary>
    public static async ValueTask<SqliteEncryptedStore> OpenAsync(
        string databasePath,
        CancellationToken cancellationToken = default)
    {
        var connection = new SqliteConnection($"Data Source={databasePath}");
        await connection.OpenAsync(cancellationToken);

        var store = new SqliteEncryptedStore(connection);
        await store.InitializeSchemaAsync(cancellationToken);
        return store;
    }

    private async Task InitializeSchemaAsync(CancellationToken cancellationToken)
    {
        using var cmd = _connection.CreateCommand();
        cmd.CommandText =
            """
            CREATE TABLE IF NOT EXISTS sealed_records (
                partition  TEXT NOT NULL,
                id         TEXT NOT NULL,
                key_id     TEXT NOT NULL,
                nonce      BLOB NOT NULL,
                ciphertext BLOB NOT NULL,
                tag        BLOB NOT NULL,
                aad        BLOB NOT NULL,
                PRIMARY KEY (partition, id)
            );
            """;
        await cmd.ExecuteNonQueryAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async ValueTask PutAsync(string partition, string id, SealedBlob blob, CancellationToken cancellationToken = default)
    {
        using var cmd = _connection.CreateCommand();
        cmd.CommandText =
            """
            INSERT OR REPLACE INTO sealed_records (partition, id, key_id, nonce, ciphertext, tag, aad)
            VALUES ($partition, $id, $keyId, $nonce, $ciphertext, $tag, $aad);
            """;
        cmd.Parameters.AddWithValue("$partition", partition);
        cmd.Parameters.AddWithValue("$id", id);
        cmd.Parameters.AddWithValue("$keyId", blob.KeyId);
        cmd.Parameters.AddWithValue("$nonce", blob.Nonce.ToArray());
        cmd.Parameters.AddWithValue("$ciphertext", blob.Ciphertext.ToArray());
        cmd.Parameters.AddWithValue("$tag", blob.Tag.ToArray());
        cmd.Parameters.AddWithValue("$aad", blob.AssociatedData.ToArray());
        await cmd.ExecuteNonQueryAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async ValueTask<SealedBlob?> GetAsync(string partition, string id, CancellationToken cancellationToken = default)
    {
        using var cmd = _connection.CreateCommand();
        cmd.CommandText =
            "SELECT key_id, nonce, ciphertext, tag, aad FROM sealed_records WHERE partition = $partition AND id = $id;";
        cmd.Parameters.AddWithValue("$partition", partition);
        cmd.Parameters.AddWithValue("$id", id);

        using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken))
        {
            return null;
        }

        return Map(reader);
    }

    /// <inheritdoc />
    public async IAsyncEnumerable<SealedBlob> ScanAsync(
        string partition,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        using var cmd = _connection.CreateCommand();
        cmd.CommandText =
            "SELECT key_id, nonce, ciphertext, tag, aad FROM sealed_records WHERE partition = $partition;";
        cmd.Parameters.AddWithValue("$partition", partition);

        using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            yield return Map(reader);
        }
    }

    /// <inheritdoc />
    public async ValueTask DeleteAsync(string partition, string id, CancellationToken cancellationToken = default)
    {
        using var cmd = _connection.CreateCommand();
        cmd.CommandText = "DELETE FROM sealed_records WHERE partition = $partition AND id = $id;";
        cmd.Parameters.AddWithValue("$partition", partition);
        cmd.Parameters.AddWithValue("$id", id);
        await cmd.ExecuteNonQueryAsync(cancellationToken);
    }

    private static SealedBlob Map(DbDataReader reader) => new()
    {
        KeyId = reader.GetString(0),
        Nonce = reader.GetFieldValue<byte[]>(1),
        Ciphertext = reader.GetFieldValue<byte[]>(2),
        Tag = reader.GetFieldValue<byte[]>(3),
        AssociatedData = reader.GetFieldValue<byte[]>(4),
    };

    /// <inheritdoc />
    public async ValueTask DisposeAsync() => await _connection.DisposeAsync();
}
