namespace VoxelAgentNexus.Server;

/// <summary>One agent (NPC or player) in a broadcast snapshot.</summary>
public readonly record struct AgentDto(string Id, string Name, string Kind, int X, int Y, int Z);

/// <summary>The world state broadcast to all clients each tick.</summary>
public sealed record SnapshotDto(long Tick, string TimeOfDay, IReadOnlyList<AgentDto> Agents);

/// <summary>A spoken line (from a player or an NPC) broadcast to all clients.</summary>
public sealed record DialogueDto(string SpeakerId, string SpeakerName, string Text);
