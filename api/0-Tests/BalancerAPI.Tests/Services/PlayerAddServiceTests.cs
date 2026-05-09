using BalancerAPI.Business.Services;
using BalancerAPI.Data.Data;
using BalancerAPI.Domain.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Moq;

namespace BalancerAPI.Tests.Services;

public class PlayerAddServiceTests
{
    private static readonly Guid PlayerUuid = Guid.Parse("9f2b2230-3b2c-4b0f-a141-d7b598e236c7");

    [Fact]
    public async Task AddAsync_WhenNewPlayer_InsertsAllPlayerStateTablesAndReturnsMessage()
    {
        var (db, factory) = CreateDbContextAndFactory();
        db.TimeDays.Add(new TimeDay { Id = 7, Timestamp = DateTime.UtcNow });
        db.TimeWeeks.Add(new TimeWeek { Id = 3, Timestamp = DateTime.UtcNow });
        await db.SaveChangesAsync();

        var resolver = new Mock<IMinecraftPlayerResolveService>();
        resolver.Setup(x => x.ResolveAsync(PlayerUuid.ToString(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(PlayerResolveResult.Ok(PlayerUuid, "sumSmash"));
        var service = new PlayerAddService(factory, resolver.Object);

        var result = await service.AddAsync(PlayerUuid, 275, CancellationToken.None);

        Assert.True(result.Success);
        var payload = Assert.IsType<PlayerAddPayload>(result.Response);
        Assert.Equal("sumSmash", payload.Name);
        Assert.Equal(PlayerUuid, payload.Uuid);
        Assert.Contains("names", payload.TablesAdded);
        Assert.Contains("base_weights", payload.TablesAdded);
        Assert.Contains("experimental_spec_weights", payload.TablesAdded);
        Assert.Contains("experimental_specs_wl", payload.TablesAdded);
        Assert.Contains("adjustment_daily", payload.TablesAdded);
        Assert.Contains("base_weights_daily", payload.TablesAdded);
        Assert.Contains("experimental_specs_wl_daily", payload.TablesAdded);
        Assert.Contains("base_weights_weekly", payload.TablesAdded);
        Assert.Contains("experimental_spec_weights_weekly", payload.TablesAdded);
        Assert.Contains("experimental_specs_wl_weekly", payload.TablesAdded);

        Assert.NotNull(await db.Names.FindAsync(PlayerUuid));
        Assert.NotNull(await db.BaseWeights.FindAsync(PlayerUuid));
        Assert.NotNull(await db.ExperimentalSpecWeights.FindAsync(PlayerUuid));
        Assert.NotNull(await db.ExperimentalSpecsWl.FindAsync(PlayerUuid));
        Assert.NotNull(await db.AdjustmentDaily.FindAsync(PlayerUuid));
        Assert.NotNull(await db.BaseWeightsDaily.FindAsync(PlayerUuid, 7));
        Assert.NotNull(await db.ExperimentalSpecsWlDaily.FindAsync(PlayerUuid, 7));
        Assert.NotNull(await db.BaseWeightsWeekly.FindAsync(PlayerUuid, 3));
        Assert.NotNull(await db.ExperimentalSpecWeightsWeekly.FindAsync(PlayerUuid, 3));
        Assert.NotNull(await db.ExperimentalSpecsWlWeekly.FindAsync(PlayerUuid, 3));
    }

    [Fact]
    public async Task AddAsync_WhenBaseWeightAlreadyExists_ReturnsConflict()
    {
        var (db, factory) = CreateDbContextAndFactory();
        db.BaseWeights.Add(new BaseWeight { Uuid = PlayerUuid, Weight = 100 });
        await db.SaveChangesAsync();

        var resolver = new Mock<IMinecraftPlayerResolveService>();
        resolver.Setup(x => x.ResolveAsync(PlayerUuid.ToString(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(PlayerResolveResult.Ok(PlayerUuid, "sumSmash"));
        var service = new PlayerAddService(factory, resolver.Object);

        var result = await service.AddAsync(PlayerUuid, 275, CancellationToken.None);

        Assert.False(result.Success);
        Assert.Equal(409, result.StatusCode);
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
