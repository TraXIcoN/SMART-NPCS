using VoxelAgentNexus.Core.Memory;

namespace VoxelAgentNexus.Memory;

/// <summary>
/// Bootstraps NPCs at world creation by writing their preset memories and starting
/// relationships into the encrypted store — the "presets and preloaded
/// conversations" that give each NPC a distinct starting voice. After this, lived
/// experience and reflection take over. (DESIGN_BRIEF.md §9.)
/// </summary>
public sealed class WorldSeeder
{
    private readonly IMemoryStore _store;
    private readonly IRelationshipGraph _relationships;

    public WorldSeeder(IMemoryStore store, IRelationshipGraph relationships)
    {
        _store = store ?? throw new ArgumentNullException(nameof(store));
        _relationships = relationships ?? throw new ArgumentNullException(nameof(relationships));
    }

    /// <summary>Write one NPC's seed memories and relationships.</summary>
    public async ValueTask SeedAsync(NpcSeed seed, DateTimeOffset now, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(seed);

        foreach (var text in seed.SeedMemories)
        {
            await _store.AppendAsync(
                new MemoryRecord
                {
                    Id = Guid.NewGuid(),
                    NpcId = seed.NpcId,
                    Kind = MemoryKind.Observation,
                    Content = text,
                    Importance = seed.SeedMemoryImportance,
                    CreatedAt = now,
                    LastAccessedAt = now,
                },
                cancellationToken);
        }

        foreach (var edge in seed.SeedRelationships)
        {
            await _relationships.UpsertEdgeAsync(edge, cancellationToken);
        }
    }
}
