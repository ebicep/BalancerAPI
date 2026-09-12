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

    [Fact]
    public void GetLineupsNew_TeamSize12_IsFiveDamageFourTankThreeHeal()
    {
        var random = new Random(12345);
        var lineup = ExperimentalBalanceService.GetLineupsNew(12, random);
        Assert.Equal(12, lineup.Length);
        Assert.Equal(5, lineup.Count(s => ExperimentalSpecs.DamageSet.Contains(s)));
        Assert.Equal(4, lineup.Count(s => ExperimentalSpecs.TankSet.Contains(s)));
        Assert.Equal(3, lineup.Count(s => ExperimentalSpecs.HealSet.Contains(s)));
    }
}
