using BalancerAPI.Business.Services;
using BalancerAPI.Data.Data;
using BalancerAPI.Domain.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace BalancerAPI.Tests.Services;

public class PlayerGetServiceTests
{
    private static readonly Guid PlayerUuid = Guid.Parse("9f2b2230-3b2c-4b0f-a141-d7b598e236c7");

    private static readonly string[] AllPhysicalTableKeys =
    [
        "names",
        "base_weights",
        "base_weights_daily",
        "base_weights_weekly",
        "experimental_spec_weights",
        "experimental_spec_weights_weekly",
        "experimental_spec_bans",
        "experimental_specs_wl",
        "experimental_specs_wl_daily",
        "experimental_specs_wl_weekly",
        "adjustment_daily"
    ];

    [Fact]
    public async Task GetAsync_WhenPlayerExists_ReturnsAllPhysicalTables()
    {
        var (db, factory) = CreateDbContextAndFactory();
        SeedPlayer(db);
        await db.SaveChangesAsync();

        var service = new PlayerGetService(factory);
        var result = await service.GetAsync(PlayerUuid, CancellationToken.None);

        Assert.True(result.Success);
        var payload = Assert.IsType<PlayerGetPayload>(result.Response);
        Assert.Equal("sumSmash", payload.Name);
        Assert.Equal(PlayerUuid, payload.Uuid);

        foreach (var table in AllPhysicalTableKeys)
        {
            Assert.True(payload.Data.ContainsKey(table), $"Expected table key '{table}' in data.");
        }

        var baseWeights = Assert.IsAssignableFrom<IEnumerable<BaseWeight>>(payload.Data["base_weights"]);
        Assert.Single(baseWeights);
        Assert.Equal(275, baseWeights.First().Weight);

        var names = Assert.IsAssignableFrom<IEnumerable<PlayerName>>(payload.Data["names"]);
        Assert.Single(names);
        Assert.Equal("sumSmash", names.First().Name);
    }

    [Fact]
    public async Task GetAsync_WhenPlayerNotFound_ReturnsNotFound()
    {
        var (_, factory) = CreateDbContextAndFactory();
        var service = new PlayerGetService(factory);

        var result = await service.GetAsync(PlayerUuid, CancellationToken.None);

        Assert.False(result.Success);
        Assert.Equal(404, result.StatusCode);
        Assert.Equal("Player not found in base_weights.", result.Message);
    }

    [Fact]
    public async Task GetAsync_DoesNotIncludeLogTables()
    {
        var (db, factory) = CreateDbContextAndFactory();
        SeedPlayer(db);
        db.AdjustmentDailyLogs.Add(new AdjustmentDailyLog
        {
            Id = Guid.NewGuid(),
            Uuid = PlayerUuid,
            PreviousWeight = 100,
            NewWeight = 275,
            Date = DateTime.UtcNow
        });
        await db.SaveChangesAsync();

        var service = new PlayerGetService(factory);
        var result = await service.GetAsync(PlayerUuid, CancellationToken.None);

        Assert.True(result.Success);
        Assert.DoesNotContain("adjustment_daily_log", result.Response!.Data.Keys);
    }

    private static void SeedPlayer(BalancerDbContext db)
    {
        db.TimeDays.Add(new TimeDay { Id = 7, Timestamp = DateTime.UtcNow });
        db.TimeWeeks.Add(new TimeWeek { Id = 3, Timestamp = DateTime.UtcNow });
        db.Names.Add(new PlayerName { Uuid = PlayerUuid, Name = "sumSmash", PreviousNames = [] });
        db.BaseWeights.Add(new BaseWeight
        {
            Uuid = PlayerUuid,
            Weight = 275,
            LastUpdated = DateTime.UtcNow
        });
        db.ExperimentalSpecWeights.Add(new ExperimentalSpecWeight { Uuid = PlayerUuid });
        db.ExperimentalSpecBans.Add(new ExperimentalSpecBan { Uuid = PlayerUuid });
        db.ExperimentalSpecsWl.Add(new ExperimentalSpecsWl { Uuid = PlayerUuid });
        db.AdjustmentDaily.Add(new AdjustmentDaily { Uuid = PlayerUuid, Trajectory = 0 });
        db.BaseWeightsDaily.Add(new BaseWeightDaily { Uuid = PlayerUuid, DayStartDate = 7, Weight = 275 });
        db.ExperimentalSpecsWlDaily.Add(new ExperimentalSpecsWlDaily { Uuid = PlayerUuid, DayStartDate = 7 });
        db.BaseWeightsWeekly.Add(new BaseWeightWeekly { Uuid = PlayerUuid, WeekStartDate = 3, Weight = 275 });
        db.ExperimentalSpecWeightsWeekly.Add(new ExperimentalSpecWeightWeekly { Uuid = PlayerUuid, WeekStartDate = 3 });
        db.ExperimentalSpecsWlWeekly.Add(new ExperimentalSpecsWlWeekly { Uuid = PlayerUuid, WeekStartDate = 3 });
    }

    private sealed class TestDbContextFactory(DbContextOptions<BalancerDbContext> options) : IDbContextFactory<BalancerDbContext>
    {
        public BalancerDbContext CreateDbContext() => new(options);
        public Task<BalancerDbContext> CreateDbContextAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(new BalancerDbContext(options));
    }

    private static (BalancerDbContext Db, IDbContextFactory<BalancerDbContext> Factory) CreateDbContextAndFactory()
    {
        var options = new DbContextOptionsBuilder<BalancerDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;
        var db = new BalancerDbContext(options);
        IDbContextFactory<BalancerDbContext> factory = new TestDbContextFactory(options);
        return (db, factory);
    }
}
