using VoxelAgentNexus.Core.Simulation;
using VoxelAgentNexus.Simulation;
using Xunit;

namespace VoxelAgentNexus.IntegrationTests;

public sealed class SpatialHashGridTests
{
    [Fact]
    public void QueryNeighbors_Returns_Only_Within_Radius()
    {
        var grid = new SpatialHashGrid(cellSize: 16);
        grid.Insert("near", new VoxelPosition(3, 0, 0));
        grid.Insert("edge", new VoxelPosition(10, 0, 0));
        grid.Insert("far", new VoxelPosition(40, 0, 0));

        var neighbors = grid.QueryNeighbors(new VoxelPosition(0, 0, 0), radius: 10);

        Assert.Contains("near", neighbors);
        Assert.Contains("edge", neighbors);
        Assert.DoesNotContain("far", neighbors);
    }

    [Fact]
    public void FindProximityPairs_Finds_Close_Pair_Once()
    {
        var grid = new SpatialHashGrid(cellSize: 8);
        grid.Insert("alice", new VoxelPosition(0, 0, 0));
        grid.Insert("bob", new VoxelPosition(2, 0, 0));
        grid.Insert("loner", new VoxelPosition(100, 0, 0));

        var pairs = grid.FindProximityPairs(radius: 5);

        Assert.Single(pairs);
        var pair = pairs[0];
        Assert.Equal("alice", pair.AgentA); // normalized ordinal order
        Assert.Equal("bob", pair.AgentB);
    }

    [Fact]
    public void Buckets_Negative_Coordinates_Consistently()
    {
        var grid = new SpatialHashGrid(cellSize: 16);
        grid.Insert("a", new VoxelPosition(-3, 0, -3));
        grid.Insert("b", new VoxelPosition(-1, 0, -1));

        var pairs = grid.FindProximityPairs(radius: 5);

        Assert.Single(pairs);
    }

    [Fact]
    public void Clear_Empties_The_Grid()
    {
        var grid = new SpatialHashGrid(cellSize: 16);
        grid.Insert("a", new VoxelPosition(0, 0, 0));
        grid.Clear();

        Assert.Empty(grid.QueryNeighbors(new VoxelPosition(0, 0, 0), radius: 100));
    }
}
