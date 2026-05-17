using BalancerAPI.Business.Services;
using BalancerAPI.Data.Data;
using BalancerAPI.Domain.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Moq;

namespace BalancerAPI.Tests.Services;

public class PlayerUuidUpdateServiceTests
{
    private static readonly Guid OldUuid = Guid.Parse("9f2b2230-3b2c-4b0f-a141-d7b598e236c7");
    private static readonly Guid NewUuid = Guid.Parse("a1b2c3d4-e5f6-7890-abcd-ef1234567890");

    [Fact]
    public async Task UpdateAsync_WhenPlayerExists_UpdatesAllTablesAndReturnsPayload()
    {
        var (db, factory) = CreateDbContextAndFactory();
        SeedPlayer(db);
        var logId = Guid.NewGuid();
        db.AdjustmentDailyLogs.Add(new AdjustmentDailyLog
        {
            Id = logId,
            Uuid = OldUuid,
            PreviousWeight = 100,
            NewWeight = 275,
            Date = DateTime.UtcNow
        });
        db.ExperimentalSpecLogs.Add(new ExperimentalSpecLog
        {
            Id = Guid.NewGuid(),
            Pyromancer = OldUuid
        });
        await db.SaveChangesAsync();

        var resolver = CreateResolverMock(NewUuid, "newPlayer");
        var service = new PlayerUuidUpdateService(factory, resolver.Object);

        var result = await service.UpdateAsync(OldUuid, NewUuid, CancellationToken.None);

        Assert.True(result.Success);
        var payload = Assert.IsType<PlayerUuidUpdatePayload>(result.Response);
        Assert.Equal("newPlayer", payload.Name);
        Assert.Equal(OldUuid, payload.OldUuid);
        Assert.Equal(NewUuid, payload.NewUuid);
        Assert.Contains("names", payload.TablesUpdated);
        Assert.Contains("base_weights", payload.TablesUpdated);
        Assert.Contains("adjustment_daily_log", payload.TablesUpdated);
        Assert.Contains("experimental_spec_logs", payload.TablesUpdated);

        db.ChangeTracker.Clear();
        Assert.Null(await db.BaseWeights.FindAsync(OldUuid));
        Assert.NotNull(await db.BaseWeights.FindAsync(NewUuid));
        Assert.Null(await db.Names.FindAsync(OldUuid));
        var nameRow = await db.Names.FindAsync(NewUuid);
        Assert.NotNull(nameRow);
        Assert.Equal("newPlayer", nameRow!.Name);
        Assert.Contains("sumSmash", nameRow.PreviousNames);

        Assert.Equal(NewUuid, (await db.AdjustmentDailyLogs.FindAsync(logId))!.Uuid);
        var specLog = await db.ExperimentalSpecLogs.SingleAsync();
        Assert.Equal(NewUuid, specLog.Pyromancer);
    }

    [Fact]
    public async Task UpdateAsync_WhenOldPlayerNotFound_ReturnsNotFound()
    {
        var (_, factory) = CreateDbContextAndFactory();
        var resolver = CreateResolverMock(NewUuid, "newPlayer");
        var service = new PlayerUuidUpdateService(factory, resolver.Object);

        var result = await service.UpdateAsync(OldUuid, NewUuid, CancellationToken.None);

        Assert.False(result.Success);
        Assert.Equal(404, result.StatusCode);
    }

    [Fact]
    public async Task UpdateAsync_WhenNewPlayerAlreadyInBaseWeights_ReturnsConflictWithoutChanges()
    {
        var (db, factory) = CreateDbContextAndFactory();
        SeedPlayer(db);
        db.BaseWeights.Add(new BaseWeight { Uuid = NewUuid, Weight = 100 });
        await db.SaveChangesAsync();

        var resolver = CreateResolverMock(NewUuid, "newPlayer");
        var service = new PlayerUuidUpdateService(factory, resolver.Object);

        var result = await service.UpdateAsync(OldUuid, NewUuid, CancellationToken.None);

        Assert.False(result.Success);
        Assert.Equal(409, result.StatusCode);
        Assert.NotNull(await db.BaseWeights.FindAsync(OldUuid));
    }

    [Fact]
    public async Task UpdateAsync_WhenSameUuid_ReturnsBadRequest()
    {
        var (_, factory) = CreateDbContextAndFactory();
        var resolver = CreateResolverMock(OldUuid, "sumSmash");
        var service = new PlayerUuidUpdateService(factory, resolver.Object);

        var result = await service.UpdateAsync(OldUuid, OldUuid, CancellationToken.None);

        Assert.False(result.Success);
        Assert.Equal(400, result.StatusCode);
    }

    [Fact]
    public async Task UpdateAsync_WhenCompositeKeyOverlap_ReturnsConflictWithoutChanges()
    {
        var (db, factory) = CreateDbContextAndFactory();
        SeedPlayer(db);
        db.BaseWeightsDaily.Add(new BaseWeightDaily { Uuid = NewUuid, DayStartDate = 7, Weight = 100 });
        await db.SaveChangesAsync();

        var resolver = CreateResolverMock(NewUuid, "newPlayer");
        var service = new PlayerUuidUpdateService(factory, resolver.Object);

        var result = await service.UpdateAsync(OldUuid, NewUuid, CancellationToken.None);

        Assert.False(result.Success);
        Assert.Equal(409, result.StatusCode);
        Assert.NotNull(await db.BaseWeights.FindAsync(OldUuid));
        Assert.NotNull(await db.BaseWeightsDaily.FindAsync(OldUuid, 7));
    }

    [Fact]
    public async Task UpdateAsync_WhenSpecLogReferencesOldUuid_UpdatesSpecColumnToNewUuid()
    {
        var (db, factory) = CreateDbContextAndFactory();
        db.Names.Add(new PlayerName { Uuid = OldUuid, Name = "sumSmash", PreviousNames = [] });
        db.BaseWeights.Add(new BaseWeight { Uuid = OldUuid, Weight = 275 });
        var logId = Guid.NewGuid();
        db.ExperimentalSpecLogs.Add(new ExperimentalSpecLog { Id = logId, Luminary = OldUuid });
        await db.SaveChangesAsync();

        var resolver = CreateResolverMock(NewUuid, "newPlayer");
        var service = new PlayerUuidUpdateService(factory, resolver.Object);

        var result = await service.UpdateAsync(OldUuid, NewUuid, CancellationToken.None);

        Assert.True(result.Success);
        db.ChangeTracker.Clear();
        var specLog = await db.ExperimentalSpecLogs.FindAsync(logId);
        Assert.NotNull(specLog);
        Assert.Equal(NewUuid, specLog!.Luminary);
        Assert.Null(specLog.Pyromancer);
    }

    private static Mock<IMinecraftPlayerResolveService> CreateResolverMock(Guid uuid, string name)
    {
        var resolver = new Mock<IMinecraftPlayerResolveService>();
        resolver.Setup(x => x.ResolveAsync(uuid.ToString(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(PlayerResolveResult.Ok(uuid, name));
        return resolver;
    }

    private static void SeedPlayer(BalancerDbContext db)
    {
        db.TimeDays.Add(new TimeDay { Id = 7, Timestamp = DateTime.UtcNow });
        db.TimeWeeks.Add(new TimeWeek { Id = 3, Timestamp = DateTime.UtcNow });
        db.Names.Add(new PlayerName { Uuid = OldUuid, Name = "sumSmash", PreviousNames = [] });
        db.BaseWeights.Add(new BaseWeight
        {
            Uuid = OldUuid,
            Weight = 275,
            LastUpdated = DateTime.UtcNow
        });
        db.ExperimentalSpecWeights.Add(new ExperimentalSpecWeight { Uuid = OldUuid });
        db.ExperimentalSpecsWl.Add(new ExperimentalSpecsWl { Uuid = OldUuid });
        db.AdjustmentDaily.Add(new AdjustmentDaily { Uuid = OldUuid, Trajectory = 0 });
        db.BaseWeightsDaily.Add(new BaseWeightDaily { Uuid = OldUuid, DayStartDate = 7, Weight = 275 });
        db.ExperimentalSpecsWlDaily.Add(new ExperimentalSpecsWlDaily { Uuid = OldUuid, DayStartDate = 7 });
        db.BaseWeightsWeekly.Add(new BaseWeightWeekly { Uuid = OldUuid, WeekStartDate = 3, Weight = 275 });
        db.ExperimentalSpecWeightsWeekly.Add(new ExperimentalSpecWeightWeekly { Uuid = OldUuid, WeekStartDate = 3 });
        db.ExperimentalSpecsWlWeekly.Add(new ExperimentalSpecsWlWeekly { Uuid = OldUuid, WeekStartDate = 3 });
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
