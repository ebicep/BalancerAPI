using BalancerAPI.Business.Services;

namespace BalancerAPI.Tests.Services;

public class ExperimentalBalanceSpecGroupWeightTests
{
    private const int MaxDiff = 20;
    private const int DefaultMaxFailures = 2;

    private static ExperimentalBalanceService.PlayerAssignment A(string spec, int evalWeight) =>
        new() { Spec = spec, EvalWeight = evalWeight };

    [Fact]
    public void ViolatesSpecGroupWeightBalance_when_all_groups_within_diff_returns_false()
    {
        var blue = new[] { A("Avenger", 10), A("Berserker", 10), A("Cryomancer", 10), A("Aquamancer", 10), A("Revenant", 10) };
        var red = new[] { A("Thunderlord", 10), A("Pyromancer", 10), A("Defender", 10), A("Luminary", 10), A("Protector", 10) };

        Assert.False(ExperimentalBalanceService.ViolatesSpecGroupWeightBalance(
            blue, red, MaxDiff, DefaultMaxFailures));
    }

    [Fact]
    public void ViolatesSpecGroupWeightBalance_when_one_group_over_diff_returns_false_four_of_five_rule()
    {
        var blue = new[] { A("Avenger", 50) };
        var red = Array.Empty<ExperimentalBalanceService.PlayerAssignment>();

        Assert.False(ExperimentalBalanceService.ViolatesSpecGroupWeightBalance(
            blue, red, MaxDiff, DefaultMaxFailures));
    }

    [Fact]
    public void ViolatesSpecGroupWeightBalance_when_two_groups_over_diff_returns_true()
    {
        var blue = new[] { A("Avenger", 50), A("Berserker", 50) };
        var red = Array.Empty<ExperimentalBalanceService.PlayerAssignment>();

        Assert.True(ExperimentalBalanceService.ViolatesSpecGroupWeightBalance(
            blue, red, MaxDiff, DefaultMaxFailures));
    }

    [Fact]
    public void ViolatesSpecGroupWeightBalance_when_max_failures_is_one_single_exceed_violates()
    {
        var blue = new[] { A("Avenger", 50) };
        var red = Array.Empty<ExperimentalBalanceService.PlayerAssignment>();

        Assert.True(ExperimentalBalanceService.ViolatesSpecGroupWeightBalance(blue, red, MaxDiff, maxFailures: 1));
    }

    [Fact]
    public void ViolatesSpecGroupWeightBalance_spec_not_in_any_group_does_not_affect_group_sums()
    {
        var blue = new[] { A("Spiritguard", 10_000) };
        var red = new[] { A("Spiritguard", 0) };

        Assert.False(ExperimentalBalanceService.ViolatesSpecGroupWeightBalance(
            blue, red, MaxDiff, DefaultMaxFailures));
    }
}
