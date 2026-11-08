using BalancerAPI.Business.Services;

namespace BalancerAPI.Tests.Services;

public class ExperimentalBalanceSpecRequestTeamSpreadTests
{
    private static readonly Guid R1 = Guid.Parse("11111111-1111-1111-1111-111111111101");
    private static readonly Guid R2 = Guid.Parse("11111111-1111-1111-1111-111111111102");
    private static readonly Guid R3 = Guid.Parse("11111111-1111-1111-1111-111111111103");
    private static readonly Guid Other = Guid.Parse("22222222-2222-2222-2222-222222222202");

    [Fact]
    public void ViolatesActiveSpecRequestTeamClustering_when_fewer_than_two_active_requesters_returns_false()
    {
        var blue = new[] { R1, Other };
        var red = new[] { Other, Other };
        var active = new HashSet<Guid> { R1 };

        Assert.False(ExperimentalBalanceService.ViolatesActiveSpecRequestTeamClustering(blue, red, active));

        active = [];
        Assert.False(ExperimentalBalanceService.ViolatesActiveSpecRequestTeamClustering(blue, red, active));
    }

    [Fact]
    public void ViolatesActiveSpecRequestTeamClustering_when_both_requesters_on_blue()
    {
        var blue = new[] { R1, R2, Other };
        var red = new[] { Other, Other, Other };
        var active = new HashSet<Guid> { R1, R2 };

        Assert.True(ExperimentalBalanceService.ViolatesActiveSpecRequestTeamClustering(blue, red, active));
    }

    [Fact]
    public void ViolatesActiveSpecRequestTeamClustering_when_both_requesters_on_red()
    {
        var blue = new[] { Other, Other, Other };
        var red = new[] { R1, R2, Other };
        var active = new HashSet<Guid> { R1, R2 };

        Assert.True(ExperimentalBalanceService.ViolatesActiveSpecRequestTeamClustering(blue, red, active));
    }

    [Fact]
    public void ViolatesActiveSpecRequestTeamClustering_when_requesters_split_returns_false()
    {
        var blue = new[] { R1, Other, Other };
        var red = new[] { R2, Other, Other };
        var active = new HashSet<Guid> { R1, R2 };

        Assert.False(ExperimentalBalanceService.ViolatesActiveSpecRequestTeamClustering(blue, red, active));
    }

    [Fact]
    public void ViolatesActiveSpecRequestTeamClustering_when_three_requesters_2v1_returns_false()
    {
        var blue = new[] { R1, R2, Other };
        var red = new[] { R3, Other, Other };
        var active = new HashSet<Guid> { R1, R2, R3 };

        Assert.False(ExperimentalBalanceService.ViolatesActiveSpecRequestTeamClustering(blue, red, active));
    }

    [Fact]
    public void ViolatesActiveSpecRequestTeamClustering_when_three_requesters_all_on_one_team_returns_true()
    {
        var blue = new[] { R1, R2, R3 };
        var red = new[] { Other, Other, Other };
        var active = new HashSet<Guid> { R1, R2, R3 };

        Assert.True(ExperimentalBalanceService.ViolatesActiveSpecRequestTeamClustering(blue, red, active));
    }
}
