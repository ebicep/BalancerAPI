using BalancerAPI.Business.Services;

namespace BalancerAPI.Tests.Services;

public class ExperimentalBalanceLineupTests
{
    [Theory]
    [InlineData(6)]
    [InlineData(7)]
    [InlineData(8)]
    [InlineData(9)]
    [InlineData(10)]
    [InlineData(11)]
    [InlineData(12)]
    [InlineData(13)]
    [InlineData(14)]
    public void GetLineupsNew_ReturnsLineupOfExpectedLength(int teamSize)
    {
        var random = new Random(teamSize * 7919);
        var lineup = ExperimentalBalanceService.GetLineupsNew(teamSize, random);
        Assert.Equal(teamSize, lineup.Length);
    }
}
