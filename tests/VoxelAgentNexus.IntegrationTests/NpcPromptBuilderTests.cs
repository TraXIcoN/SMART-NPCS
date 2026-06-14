using VoxelAgentNexus.Ai;
using VoxelAgentNexus.Core.Ai;
using VoxelAgentNexus.Core.Memory;
using Xunit;

namespace VoxelAgentNexus.IntegrationTests;

public sealed class NpcPromptBuilderTests
{
    [Fact]
    public void Build_Places_Persona_In_Prefix_And_Memories_In_Volatile()
    {
        var now = DateTimeOffset.UtcNow;
        var persona = new NpcPersona
        {
            NpcId = "npc_brom",
            Name = "Brom",
            SystemPrompt = "You are Brom, a gruff but fair blacksmith.",
        };
        var memory = new MemoryRecord
        {
            Id = Guid.NewGuid(),
            NpcId = "npc_brom",
            Kind = MemoryKind.Observation,
            Content = "The player gave me an apple at the market.",
            Importance = 6f,
            CreatedAt = now,
            LastAccessedAt = now,
        };

        var request = NpcPromptBuilder.Build(
            persona,
            [new ScoredMemory(memory, 1.2f)],
            [AiMessage.User("Do you remember me?")]);

        Assert.Equal("npc_brom", request.NpcId);
        Assert.Single(request.CacheablePrefix);
        Assert.Equal(AiRole.System, request.CacheablePrefix[0].Role);
        Assert.Contains("blacksmith", request.CacheablePrefix[0].Content, StringComparison.Ordinal);

        Assert.Contains(request.Volatile, m => m.Content.Contains("apple", StringComparison.Ordinal));
        Assert.Contains(request.Volatile, m => m.Role == AiRole.User && m.Content.Contains("remember", StringComparison.Ordinal));
    }

    [Fact]
    public void Build_Omits_Memory_Block_When_No_Memories()
    {
        var persona = new NpcPersona { NpcId = "n", Name = "N", SystemPrompt = "persona" };

        var request = NpcPromptBuilder.Build(persona, [], [AiMessage.User("Hello")]);

        Assert.Single(request.Volatile);
        Assert.Equal(AiRole.User, request.Volatile[0].Role);
    }
}
