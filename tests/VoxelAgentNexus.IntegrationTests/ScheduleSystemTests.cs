using VoxelAgentNexus.Core.Simulation;
using VoxelAgentNexus.Simulation;
using Xunit;

namespace VoxelAgentNexus.IntegrationTests;

public sealed class ScheduleSystemTests
{
    private static DailySchedule BromsDay() => DailySchedule.Create(
    [
        new ScheduleEntry(new TimeOnly(6, 0), Activity.Commute, new VoxelPosition(10, 64, 10)),
        new ScheduleEntry(new TimeOnly(8, 0), Activity.Work, new VoxelPosition(20, 64, 20)),
        new ScheduleEntry(new TimeOnly(12, 0), Activity.Eat, new VoxelPosition(15, 64, 15)),
        new ScheduleEntry(new TimeOnly(22, 0), Activity.Sleep, new VoxelPosition(5, 64, 5)),
    ]);

    [Theory]
    [InlineData(7, 0, Activity.Commute)]
    [InlineData(9, 30, Activity.Work)]
    [InlineData(12, 0, Activity.Eat)] // boundary is inclusive of start
    [InlineData(23, 0, Activity.Sleep)]
    public void Resolve_Returns_Active_Slot(int hour, int minute, Activity expected)
    {
        var entry = ScheduleSystem.Resolve(BromsDay(), new TimeOnly(hour, minute));
        Assert.Equal(expected, entry.Activity);
    }

    [Fact]
    public void Before_First_Slot_Wraps_To_Previous_Day()
    {
        // 03:00 is before the 06:00 commute, so the 22:00 Sleep slot is still active.
        var entry = ScheduleSystem.Resolve(BromsDay(), new TimeOnly(3, 0));
        Assert.Equal(Activity.Sleep, entry.Activity);
    }

    [Fact]
    public void Create_Sorts_Unordered_Entries()
    {
        var schedule = DailySchedule.Create(
        [
            new ScheduleEntry(new TimeOnly(22, 0), Activity.Sleep, default),
            new ScheduleEntry(new TimeOnly(6, 0), Activity.Commute, default),
        ]);

        Assert.Equal(new TimeOnly(6, 0), schedule.Entries[0].StartTime);
    }
}
