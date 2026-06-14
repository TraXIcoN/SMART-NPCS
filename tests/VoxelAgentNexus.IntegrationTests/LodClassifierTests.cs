using VoxelAgentNexus.Core.Simulation;
using VoxelAgentNexus.Simulation;
using Xunit;

namespace VoxelAgentNexus.IntegrationTests;

public sealed class LodClassifierTests
{
    private static readonly LodClassifier Classifier =
        new(new LodThresholds { NearRadius = 10, MidRadius = 50 });

    private static readonly VoxelPosition Player = new(0, 0, 0);

    [Theory]
    [InlineData(5, SimulationLod.Near)]
    [InlineData(10, SimulationLod.Near)]  // inclusive boundary
    [InlineData(30, SimulationLod.Mid)]
    [InlineData(50, SimulationLod.Mid)]   // inclusive boundary
    [InlineData(80, SimulationLod.Far)]
    public void Classify_Bands_By_Distance(int x, SimulationLod expected)
    {
        var lod = Classifier.Classify(Player, new VoxelPosition(x, 0, 0));
        Assert.Equal(expected, lod);
    }

    [Fact]
    public void Invalid_Thresholds_Throw()
    {
        Assert.Throws<ArgumentException>(() =>
            new LodClassifier(new LodThresholds { NearRadius = 50, MidRadius = 10 }));
    }
}
