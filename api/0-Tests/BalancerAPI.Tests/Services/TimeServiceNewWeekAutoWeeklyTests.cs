using BalancerAPI.Business.Services;
using BalancerAPI.Data.Data;
using BalancerAPI.Domain.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace BalancerAPI.Tests.Services;

public class TimeServiceNewWeekAutoWeeklyTests
{
    private static readonly Guid PlayerUuid = Guid.Parse("a1b2c3d4-e5f6-7890-abcd-ef1234567890");
    private static readonly DateTime Boundary = new(2026, 4, 1, 0, 0, 0, DateTimeKind.Utc);

    private sealed class TestDbContextFactory(DbContextOptions<BalancerDbContext> options) : IDbContextFactory<BalancerDbContext>
    {
        public BalancerDbContext CreateDbContext() => new TestBalancerDbContext(options);

        public Task<BalancerDbContext> CreateDbContextAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(CreateDbContext());
    }

    private sealed class TestBalancerDbContext(DbContextOptions<BalancerDbContext> options) : BalancerDbContext(options)
    {
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Ignore<ExperimentalSpecsWlCurrentWeek>();
            modelBuilder.Entity<ExperimentalSpecsWlCurrentWeek>(entity =>
            {
                entity.ToTable("experimental_specs_wl_current_week_test");
                entity.HasKey(x => x.Uuid);
            });
        }
    }

    private static DbContextOptions<BalancerDbContext> CreateOptions(string dbName) =>
        new DbContextOptionsBuilder<BalancerDbContext>()
            .UseInMemoryDatabase(dbName)
            .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;

    [Fact]
    public async Task CreateNewWeekAsync_AfterAutoWeeklyOnlyChange_SnapshotsSpecWeightsAtNewWeek()
    {
        var options = CreateOptions(Guid.NewGuid().ToString());
        await SeedPlayerAsync(options, specLastUpdated: Boundary.AddMinutes(-1));

        await using var db = new TestBalancerDbContext(options);
        var timeService = CreateTimeService(db, options);
        var response = await timeService.CreateNewWeekAsync(CancellationToken.None);

        Assert.Equal(1, response.NewWeek);
        Assert.Equal(1, response.AutoWeekly.Count);
        Assert.Equal(8, Assert.Single(response.AutoWeekly.Adjusted).Specs.Single(s => s.Spec == "Pyromancer").CurrentOffset);

        await using var verify = new TestBalancerDbContext(options);
        var specWeight = await verify.ExperimentalSpecWeights.AsNoTracking().SingleAsync(x => x.Uuid == PlayerUuid);
        Assert.Equal(8, specWeight.PyromancerOffset);

        var weekly = await verify.ExperimentalSpecWeightsWeekly
            .Where(x => x.Uuid == PlayerUuid && x.WeekStartDate == 1)
            .ToListAsync();
        var snapshot = Assert.Single(weekly);
        Assert.Equal(8, snapshot.PyromancerOffset);
    }

    [Fact]
    public async Task ApplyAutoWeeklyAsync_PersistsSpecWeightOffsetChanges()
    {
        var options = CreateOptions(Guid.NewGuid().ToString());
        await SeedPlayerAsync(options, specLastUpdated: Boundary.AddMinutes(-1));

        await using var db = new TestBalancerDbContext(options);
        var service = new AdjustmentAutoWeeklyService(db);
        var result = await service.ApplyAutoWeeklyAsync(CancellationToken.None);

        Assert.Equal(1, result.Count);

        await using var verify = new TestBalancerDbContext(options);
        var stored = await verify.ExperimentalSpecWeights.SingleAsync(x => x.Uuid == PlayerUuid);
        Assert.Equal(8, stored.PyromancerOffset);
    }

    [Fact]
    public async Task CreateNewWeekAsync_WhenMidWeekTouchAndAutoWeekly_SnapshotsPostAutoWeeklyOffsets()
    {
        var options = CreateOptions(Guid.NewGuid().ToString());
        await SeedPlayerAsync(options, specLastUpdated: Boundary.AddMinutes(1));

        await using var db = new TestBalancerDbContext(options);
        var timeService = CreateTimeService(db, options);
        await timeService.CreateNewWeekAsync(CancellationToken.None);

        await using var verify = new TestBalancerDbContext(options);
        var specWeight = await verify.ExperimentalSpecWeights.AsNoTracking().SingleAsync(x => x.Uuid == PlayerUuid);
        Assert.Equal(8, specWeight.PyromancerOffset);

        var snapshot = await verify.ExperimentalSpecWeightsWeekly.SingleAsync(
            x => x.Uuid == PlayerUuid && x.WeekStartDate == 1);
        Assert.Equal(8, snapshot.PyromancerOffset);
    }

    private static TimeService CreateTimeService(BalancerDbContext db, DbContextOptions<BalancerDbContext> options)
    {
        var autoWeekly = new AdjustmentAutoWeeklyService(db);
        return new TimeService(db, new TestDbContextFactory(options), autoWeekly);
    }

    private static async Task SeedPlayerAsync(DbContextOptions<BalancerDbContext> options, DateTime specLastUpdated)
    {
        await using var seed = new TestBalancerDbContext(options);
        seed.TimeWeeks.Add(new TimeWeek { Id = 0, Timestamp = Boundary });

        seed.BaseWeights.Add(new BaseWeight
        {
            Uuid = PlayerUuid,
            Weight = 100,
            LastUpdated = Boundary.AddMinutes(-1)
        });

        seed.ExperimentalSpecWeights.Add(new ExperimentalSpecWeight
        {
            Uuid = PlayerUuid,
            PyromancerOffset = 10,
            LastUpdated = specLastUpdated
        });

        seed.ExperimentalSpecsWl.Add(new ExperimentalSpecsWl
        {
            Uuid = PlayerUuid,
            LastUpdated = Boundary.AddMinutes(-1)
        });

        seed.ExperimentalSpecsWlCurrentWeek.Add(new ExperimentalSpecsWlCurrentWeek
        {
            Uuid = PlayerUuid,
            PyromancerWins = 4,
            PyromancerLosses = 0
        });

        await seed.SaveChangesAsync();
    }
}
