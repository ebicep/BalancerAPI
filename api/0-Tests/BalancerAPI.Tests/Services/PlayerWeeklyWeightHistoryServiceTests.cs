using BalancerAPI.Business.Services;
using BalancerAPI.Data.Data;
using BalancerAPI.Domain.Models;
using Microsoft.EntityFrameworkCore;

namespace BalancerAPI.Tests.Services;

public class PlayerWeeklyWeightHistoryServiceTests
{
    private static readonly Guid TestUuid = Guid.Parse("a1b2c3d4-e5f6-7890-abcd-ef1234567890");
    private static readonly DateTime Week0Time = new(2026, 1, 5, 0, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime Week1Time = new(2026, 1, 12, 0, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime FixedLastUpdated = new(2026, 1, 10, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public async Task GetAsync_ForwardFillsMiddleWeek_AndLiveOverridesCurrentWeek()
    {
        await using var db = CreateDbContext();

        db.TimeWeeks.AddRange(
            new TimeWeek { Id = 0, Timestamp = Week0Time },
            new TimeWeek { Id = 1, Timestamp = Week1Time });

        db.Names.Add(new PlayerName { Uuid = TestUuid, Name = "TestPlayer", PreviousNames = [] });

        db.BaseWeights.Add(new BaseWeight
        {
            Uuid = TestUuid,
            Weight = 950,
            LastUpdated = FixedLastUpdated,
        });

        db.BaseWeightsWeekly.Add(new BaseWeightWeekly
        {
            Uuid = TestUuid,
            WeekStartDate = 0,
            Weight = 900,
        });

        db.ExperimentalSpecWeights.Add(new ExperimentalSpecWeight
        {
            Uuid = TestUuid,
            LastUpdated = FixedLastUpdated,
            PyromancerOffset = 5,
        });

        db.ExperimentalSpecWeightsWeekly.Add(new ExperimentalSpecWeightWeekly
        {
            Uuid = TestUuid,
            WeekStartDate = 0,
            PyromancerOffset = 10,
        });

        await db.SaveChangesAsync();

        var service = new PlayerWeeklyWeightHistoryService(db);
        var result = await service.GetAsync(TestUuid, CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal("TestPlayer", result.Name);
        Assert.Equal(2, result.Weeks.Count);

        var week0 = result.Weeks[0];
        Assert.Equal(0, week0.WeekId);
        Assert.False(week0.IsCurrentWeek);
        Assert.Equal(900, week0.BaseWeight);
        Assert.Equal(10, week0.SpecOffsets.Pyromancer);

        var week1 = result.Weeks[1];
        Assert.Equal(1, week1.WeekId);
        Assert.True(week1.IsCurrentWeek);
        Assert.Equal(950, week1.BaseWeight);
        Assert.Equal(5, week1.SpecOffsets.Pyromancer);
    }

    private static BalancerDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<BalancerDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new BalancerDbContext(options);
    }
}
