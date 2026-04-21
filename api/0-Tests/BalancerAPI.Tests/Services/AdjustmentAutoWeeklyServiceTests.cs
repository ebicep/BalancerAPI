using BalancerAPI.Business.Services;

namespace BalancerAPI.Tests.Services;

public class AdjustmentAutoWeeklyServiceTests
{
    [Theory]
    [InlineData(0, 0, 0)]
    [InlineData(1, 0, 0)]
    [InlineData(2, 0, 0)]
    [InlineData(2, 1, 0)]
    [InlineData(3, 0, 1)]
    [InlineData(4, 0, 2)]
    [InlineData(5, 1, 2)]
    [InlineData(10, 3, 5)]
    [InlineData(0, 5, -3)]
    [InlineData(0, 2, 0)]
    [InlineData(1, 3, 0)]
    [InlineData(0, 3, -1)]
    [InlineData(0, 4, -2)]
    [InlineData(1, 5, -2)]
    [InlineData(0, 10, -8)]
    [InlineData(3, 10, -5)]
    public void ComputeWeeklySpecOffsetAdjustment_MatchesNetThresholds(int wins, int losses, int expected)
    {
        Assert.Equal(expected, AdjustmentAutoWeeklyService.ComputeWeeklySpecOffsetAdjustment(wins, losses));
    }
}
