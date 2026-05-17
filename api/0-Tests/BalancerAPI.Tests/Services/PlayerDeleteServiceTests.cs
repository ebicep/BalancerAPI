using BalancerAPI.Business.Services;
using BalancerAPI.Data.Data;
using BalancerAPI.Domain.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace BalancerAPI.Tests.Services;

public class PlayerDeleteServiceTests
{
    private static readonly Guid PlayerUuid = Guid.Parse("9f2b2230-3b2c-4b0f-a141-d7b598e236c7");

    [Fact]
    public async Task DeleteAsync_WhenPlayerExists_RemovesAllPlayerStateTablesAndReturnsDeletedData()
    {
        var (db, factory) = CreateDbContextAndFactory();
        SeedPlayer(db);
        await db.SaveChangesAsync();

        var service = new PlayerDeleteService(factory);
        var result = await service.DeleteAsync(PlayerUuid, CancellationToken.None);

        Assert.True(result.Success);
        var payload = Assert.IsType<PlayerDeletePayload>(result.Response);
        Assert.Equal("sumSmash", payload.Name);
        Assert.Equal(PlayerUuid, payload.Uuid);
        Assert.Contains("names", payload.TablesRemoved);
        Assert.Contains("base_weights", payload.TablesRemoved);
        Assert.Contains("experimental_spec_weights", payload.TablesRemoved);
        Assert.Contains("experimental_specs_wl", payload.TablesRemoved);
        Assert.Contains("adjustment_daily", payload.TablesRemoved);
        Assert.Contains("base_weights_daily", payload.TablesRemoved);
        Assert.Contains("experimental_specs_wl_daily", payload.TablesRemoved);
        Assert.Contains("base_weights_weekly", payload.TablesRemoved);
        Assert.Contains("experimental_spec_weights_weekly", payload.TablesRemoved);
        Assert.Contains("experimental_specs_wl_weekly", payload.TablesRemoved);

        Assert.True(payload.Data.ContainsKey("base_weights"));
        var baseWeights = Assert.IsAssignableFrom<IEnumerable<BaseWeight>>(payload.Data["base_weights"]);
        Assert.Single(baseWeights);
        Assert.Equal(275, baseWeights.First().Weight);

        db.ChangeTracker.Clear();
        Assert.Null(await db.Names.FindAsync(PlayerUuid));
        Assert.Null(await db.BaseWeights.FindAsync(PlayerUuid));
        Assert.Null(await db.ExperimentalSpecWeights.FindAsync(PlayerUuid));
        Assert.Null(await db.ExperimentalSpecsWl.FindAsync(PlayerUuid));
        Assert.Null(await db.AdjustmentDaily.FindAsync(PlayerUuid));
        Assert.Null(await db.BaseWeightsDaily.FindAsync(PlayerUuid, 7));
        Assert.Null(await db.ExperimentalSpecsWlDaily.FindAsync(PlayerUuid, 7));
        Assert.Null(await db.BaseWeightsWeekly.FindAsync(PlayerUuid, 3));
        Assert.Null(await db.ExperimentalSpecWeightsWeekly.FindAsync(PlayerUuid, 3));
        Assert.Null(await db.ExperimentalSpecsWlWeekly.FindAsync(PlayerUuid, 3));
    }

    [Fact]
    public async Task DeleteAsync_WhenPlayerNotFound_ReturnsNotFound()
    {
        var (_, factory) = CreateDbContextAndFactory();
        var service = new PlayerDeleteService(factory);

        var result = await service.DeleteAsync(PlayerUuid, CancellationToken.None);

        Assert.False(result.Success);
        Assert.Equal(404, result.StatusCode);
    }

    [Fact]
    public async Task DeleteAsync_DoesNotTouchLogTables()
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

        var service = new PlayerDeleteService(factory);
        var result = await service.DeleteAsync(PlayerUuid, CancellationToken.None);

        Assert.True(result.Success);
        Assert.DoesNotContain("adjustment_daily_log", result.Response!.TablesRemoved);
        Assert.Single(await db.AdjustmentDailyLogs.Where(x => x.Uuid == PlayerUuid).ToListAsync());
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
