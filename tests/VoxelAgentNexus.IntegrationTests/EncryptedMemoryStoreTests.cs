using System.Text;
using Microsoft.Data.Sqlite;
using VoxelAgentNexus.Core.Memory;
using VoxelAgentNexus.Crypto;
using VoxelAgentNexus.Memory;
using VoxelAgentNexus.Persistence;
using Xunit;

namespace VoxelAgentNexus.IntegrationTests;

/// <summary>
/// End-to-end proof of the §3.1 privacy claim: a memory round-trips through the
/// AES-GCM + SQLite pipeline, AND the resulting database file contains no
/// plaintext.
/// </summary>
public sealed class EncryptedMemoryStoreTests
{
    [Fact]
    public async Task Memory_RoundTrips_Through_Encrypted_Sqlite()
    {
        var dbPath = TempDbPath();
        try
        {
            var (_, crypto) = await DevCrypto.CreateAsync();
            await using var store = await SqliteEncryptedStore.OpenAsync(dbPath);
            var memory = new EncryptedMemoryStore(crypto, store);

            var original = NewRecord("npc_brom", "The player traded me a golden apple.");
            await memory.AppendAsync(original);

            var roundTripped = await memory.GetAsync("npc_brom", original.Id);

            Assert.NotNull(roundTripped);
            Assert.Equal(original.Content, roundTripped!.Content);
            Assert.Equal(original.Importance, roundTripped.Importance);
            Assert.Equal(original.Kind, roundTripped.Kind);
        }
        finally
        {
            Cleanup(dbPath);
        }
    }

    [Fact]
    public async Task Plaintext_Never_Touches_Disk()
    {
        var dbPath = TempDbPath();
        const string secret = "I hold a grudge against the player for burning my farm.";
        try
        {
            var (_, crypto) = await DevCrypto.CreateAsync();
            await using (var store = await SqliteEncryptedStore.OpenAsync(dbPath))
            {
                var memory = new EncryptedMemoryStore(crypto, store);
                await memory.AppendAsync(NewRecord("npc_mara", secret));
            }

            // Disposed: read the raw database file and scan every byte.
            SqliteConnection.ClearAllPools();
            var raw = await File.ReadAllBytesAsync(dbPath);

            Assert.False(
                ContainsUtf8(raw, secret),
                "Plaintext memory content was found on disk — encryption boundary breached.");
        }
        finally
        {
            Cleanup(dbPath);
        }
    }

    [Fact]
    public async Task WorkingSet_Loads_All_Memories_For_Npc()
    {
        var dbPath = TempDbPath();
        try
        {
            var (_, crypto) = await DevCrypto.CreateAsync();
            await using var store = await SqliteEncryptedStore.OpenAsync(dbPath);
            var memory = new EncryptedMemoryStore(crypto, store);

            await memory.AppendAsync(NewRecord("npc_brom", "Met the player at the well."));
            await memory.AppendAsync(NewRecord("npc_brom", "Argued about the harvest."));
            await memory.AppendAsync(NewRecord("npc_mara", "Someone else entirely."));

            var bromSet = await memory.LoadWorkingSetAsync("npc_brom");

            Assert.Equal(2, bromSet.Count);
            Assert.All(bromSet, m => Assert.Equal("npc_brom", m.NpcId));
        }
        finally
        {
            Cleanup(dbPath);
        }
    }

    private static MemoryRecord NewRecord(string npcId, string content) => new()
    {
        Id = Guid.NewGuid(),
        NpcId = npcId,
        Kind = MemoryKind.Observation,
        Content = content,
        Importance = 7f,
        CreatedAt = DateTimeOffset.UtcNow,
    };

    private static string TempDbPath() =>
        Path.Combine(Path.GetTempPath(), $"nexus-test-{Guid.NewGuid():N}.sqlite");

    private static void Cleanup(string dbPath)
    {
        SqliteConnection.ClearAllPools();
        if (File.Exists(dbPath))
        {
            File.Delete(dbPath);
        }
    }

    private static bool ContainsUtf8(byte[] haystack, string needleText)
    {
        var needle = Encoding.UTF8.GetBytes(needleText);
        if (needle.Length == 0 || haystack.Length < needle.Length)
        {
            return false;
        }

        for (var i = 0; i <= haystack.Length - needle.Length; i++)
        {
            var match = true;
            for (var j = 0; j < needle.Length; j++)
            {
                if (haystack[i + j] != needle[j])
                {
                    match = false;
                    break;
                }
            }

            if (match)
            {
                return true;
            }
        }

        return false;
    }
}
