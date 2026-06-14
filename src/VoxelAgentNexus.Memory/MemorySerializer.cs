using System.Text.Json;
using VoxelAgentNexus.Core.Memory;

namespace VoxelAgentNexus.Memory;

/// <summary>
/// Serializes <see cref="MemoryRecord"/> to/from UTF-8 JSON bytes for sealing.
/// A flat DTO with concrete arrays is used so the embedding and evidence links
/// round-trip deterministically (rather than serializing <c>ReadOnlyMemory&lt;T&gt;</c>).
/// </summary>
internal static class MemorySerializer
{
    private sealed record Dto(
        Guid Id,
        string NpcId,
        MemoryKind Kind,
        string Content,
        float Importance,
        DateTimeOffset CreatedAt,
        DateTimeOffset LastAccessedAt,
        float[] Embedding,
        Guid[] EvidenceIds);

    public static byte[] Serialize(MemoryRecord record)
    {
        var dto = new Dto(
            record.Id,
            record.NpcId,
            record.Kind,
            record.Content,
            record.Importance,
            record.CreatedAt,
            record.LastAccessedAt,
            record.Embedding.ToArray(),
            [.. record.EvidenceIds]);

        return JsonSerializer.SerializeToUtf8Bytes(dto);
    }

    public static MemoryRecord Deserialize(ReadOnlySpan<byte> utf8)
    {
        var dto = JsonSerializer.Deserialize<Dto>(utf8)
            ?? throw new InvalidDataException("Could not deserialize MemoryRecord.");

        return new MemoryRecord
        {
            Id = dto.Id,
            NpcId = dto.NpcId,
            Kind = dto.Kind,
            Content = dto.Content,
            Importance = dto.Importance,
            CreatedAt = dto.CreatedAt,
            LastAccessedAt = dto.LastAccessedAt,
            Embedding = dto.Embedding,
            EvidenceIds = dto.EvidenceIds,
        };
    }
}
