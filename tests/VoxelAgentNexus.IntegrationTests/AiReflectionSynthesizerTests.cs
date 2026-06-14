using VoxelAgentNexus.Ai;
using VoxelAgentNexus.Core.Memory;
using Xunit;

namespace VoxelAgentNexus.IntegrationTests;

public sealed class AiReflectionSynthesizerTests
{
    private static MemoryRecord Observation(string content) => new()
    {
        Id = Guid.NewGuid(),
        NpcId = "brom",
        Kind = MemoryKind.Observation,
        Content = content,
        Importance = 5f,
        CreatedAt = DateTimeOffset.UtcNow,
    };

    [Fact]
    public async Task Parses_Beliefs_Strips_Bullets_And_Caps_At_Three()
    {
        var adapter = new StubAiAdapter(
            "- The player is a thief.\n* They burned my farm.\nI should stay wary.\n- one belief too many");
        var synthesizer = new AiReflectionSynthesizer(adapter);

        var beliefs = await synthesizer.SynthesizeAsync("brom", [Observation("player stole bread")]);

        Assert.Equal(3, beliefs.Count);
        Assert.Equal("The player is a thief.", beliefs[0]);
        Assert.Equal("They burned my farm.", beliefs[1]);
        Assert.Equal("I should stay wary.", beliefs[2]);
    }

    [Fact]
    public async Task Empty_Evidence_Skips_The_Model()
    {
        var synthesizer = new AiReflectionSynthesizer(new StubAiAdapter("ignored"));

        var beliefs = await synthesizer.SynthesizeAsync("brom", []);

        Assert.Empty(beliefs);
    }
}
