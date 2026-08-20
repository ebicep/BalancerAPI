using BalancerAPI.Business.Services;

namespace BalancerAPI.Tests.Services;

public class ExperimentalSpecsTests
{
    [Fact]
    public void ClassAndSpecTypeMaps_CoverAllEighteenSpecsWithoutOverlap()
    {
        var fromClasses = ExperimentalClasses.AllOrdered
            .SelectMany(ExperimentalSpecs.SpecsForClass)
            .ToList();
        var fromSpecTypes = ExperimentalSpecTypes.AllOrdered
            .SelectMany(ExperimentalSpecs.SpecsForSpecType)
            .ToList();

        Assert.Equal(ExperimentalSpecs.AllOrdered, fromClasses);
        Assert.Equal(ExperimentalSpecs.AllOrdered.Length, fromClasses.Distinct(StringComparer.Ordinal).Count());
        Assert.Equal(
            ExperimentalSpecs.AllOrdered.OrderBy(s => s, StringComparer.Ordinal),
            fromSpecTypes.OrderBy(s => s, StringComparer.Ordinal));
        Assert.Equal(ExperimentalSpecs.AllOrdered.Length, fromSpecTypes.Distinct(StringComparer.Ordinal).Count());
    }

    [Fact]
    public void LineupTankPool_ExcludesSpiritguard_SpecTypeTankIncludesIt()
    {
        Assert.DoesNotContain("Spiritguard", ExperimentalSpecs.Tank);
        Assert.DoesNotContain("Spiritguard", ExperimentalSpecs.TankSet);
        Assert.Contains("Spiritguard", ExperimentalSpecs.SpecsForSpecType(ExperimentalSpecTypes.Tank));
        Assert.Equal(["Cryomancer", "Vindicator", "Crusader"], ExperimentalSpecs.TankPicks);
    }

    [Theory]
    [InlineData("heal", "Healer")]
    [InlineData("Heal", "Healer")]
    [InlineData("Healer", "Healer")]
    [InlineData("tank", "Tank")]
    public void TryNormalizeSpecType_AcceptsHealAlias(string input, string expected)
    {
        Assert.Equal(expected, ExperimentalSpecs.TryNormalizeSpecType(input));
    }
}
