using VoxelAgentNexus.Core.Memory;

namespace VoxelAgentNexus.Memory;

/// <summary>
/// Drives bottom-up personality growth. When an NPC's observations since its last
/// reflection cross an importance threshold, it asks an <see cref="IReflectionSynthesizer"/>
/// to distill them into beliefs, then writes those back as <see cref="MemoryKind.Reflection"/>
/// memories (linked to their evidence). Those beliefs later enrich the NPC's prompt
/// context, so its personality grows from what it has lived. (DESIGN_BRIEF.md §3, §9.)
/// </summary>
public sealed class ReflectionSystem
{
    /// <summary>Importance assigned to a synthesized belief (kept salient for retrieval).</summary>
    public const float ReflectionImportance = 8f;

    private readonly IMemoryStore _store;
    private readonly IReflectionSynthesizer _synthesizer;
    private readonly float _importanceThreshold;

    public ReflectionSystem(IMemoryStore store, IReflectionSynthesizer synthesizer, float importanceThreshold = 150f)
    {
        _store = store ?? throw new ArgumentNullException(nameof(store));
        _synthesizer = synthesizer ?? throw new ArgumentNullException(nameof(synthesizer));
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(importanceThreshold);
        _importanceThreshold = importanceThreshold;
    }

    /// <summary>
    /// Reflect for an NPC if it is due. Returns the belief memories created (empty if
    /// the threshold was not reached).
    /// </summary>
    public async ValueTask<IReadOnlyList<MemoryRecord>> MaybeReflectAsync(
        string npcId,
        DateTimeOffset now,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(npcId);

        var workingSet = await _store.LoadWorkingSetAsync(npcId, cancellationToken);

        var lastReflectionAt = workingSet
            .Where(m => m.Kind == MemoryKind.Reflection)
            .Select(m => m.CreatedAt)
            .DefaultIfEmpty(DateTimeOffset.MinValue)
            .Max();

        var sinceLast = workingSet
            .Where(m => m.Kind == MemoryKind.Observation && m.CreatedAt > lastReflectionAt)
            .ToList();

        if (!ReflectionTrigger.ShouldReflect(sinceLast, _importanceThreshold))
        {
            return [];
        }

        var beliefs = await _synthesizer.SynthesizeAsync(npcId, sinceLast, cancellationToken);
        var evidenceIds = sinceLast.Select(m => m.Id).ToList();

        var created = new List<MemoryRecord>();
        foreach (var belief in beliefs)
        {
            if (string.IsNullOrWhiteSpace(belief))
            {
                continue;
            }

            var record = new MemoryRecord
            {
                Id = Guid.NewGuid(),
                NpcId = npcId,
                Kind = MemoryKind.Reflection,
                Content = belief,
                Importance = ReflectionImportance,
                CreatedAt = now,
                LastAccessedAt = now,
                EvidenceIds = evidenceIds,
            };

            await _store.AppendAsync(record, cancellationToken);
            created.Add(record);
        }

        return created;
    }
}
