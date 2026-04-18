using BalancerAPI.Business.Services;
using BalancerAPI.Data.Data;
using BalancerAPI.Domain.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace BalancerAPI.Tests.Services;

public class TimeServiceSnapshotTests
{
    private sealed class TestDbContextFactory(DbContextOptions<BalancerDbContext> options) : IDbContextFactory<BalancerDbContext>
    {
        public BalancerDbContext CreateDbContext() => new(options);

        public Task<BalancerDbContext> CreateDbContextAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(CreateDbContext());
    }

    private static DbContextOptions<BalancerDbContext> CreateOptions(string dbName) =>
        new DbContextOptionsBuilder<BalancerDbContext>()
            .UseInMemoryDatabase(dbName)
            .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;

    [Fact]
    public async Task CreateNewDayAsync_SnapshotsOnlyChangedPlayers()
    {
        var options = CreateOptions(Guid.NewGuid().ToString());
        var boundary = new DateTime(2026, 4, 1, 0, 0, 0, DateTimeKind.Utc);

        await using (var seed = new BalancerDbContext(options))
        {
            seed.TimeDays.Add(new TimeDay { Id = 0, Timestamp = boundary });

            seed.BaseWeights.AddRange(
                new BaseWeight { Uuid = Guid.NewGuid(), Weight = 100, LastUpdated = boundary.AddMinutes(-1) },
                new BaseWeight { Uuid = Guid.NewGuid(), Weight = 200, LastUpdated = boundary.AddMinutes(1) });

            seed.ExperimentalSpecsWl.AddRange(
                new ExperimentalSpecsWl { Uuid = Guid.NewGuid(), LastUpdated = boundary.AddMinutes(-1) },
                new ExperimentalSpecsWl { Uuid = Guid.NewGuid(), LastUpdated = boundary.AddMinutes(1) });

            await seed.SaveChangesAsync();
        }

        await using (var db = new BalancerDbContext(options))
        {
            var service = new TimeService(db, new TestDbContextFactory(options));
            var newDayId = await service.CreateNewDayAsync(CancellationToken.None);

            Assert.Equal(1, newDayId);

            var baseDaily = await db.BaseWeightsDaily.Where(x => x.DayStartDate == newDayId).ToListAsync();
            Assert.Single(baseDaily);
            Assert.Equal(200, baseDaily[0].Weight);

            var wlDaily = await db.ExperimentalSpecsWlDaily.Where(x => x.DayStartDate == newDayId).ToListAsync();
            Assert.Single(wlDaily);
        }
    }

    [Fact]
    public async Task CreateNewWeekAsync_SnapshotsOnlyChangedPlayers()
    {
        var options = CreateOptions(Guid.NewGuid().ToString());
        var boundary = new DateTime(2026, 4, 1, 0, 0, 0, DateTimeKind.Utc);

        var bwUnchanged = Guid.NewGuid();
        var bwChanged = Guid.NewGuid();
        var swUnchanged = Guid.NewGuid();
        var swChanged = Guid.NewGuid();
        var wlUnchanged = Guid.NewGuid();
        var wlChanged = Guid.NewGuid();

        await using (var seed = new BalancerDbContext(options))
        {
            seed.TimeWeeks.Add(new TimeWeek { Id = 0, Timestamp = boundary });

            seed.BaseWeights.AddRange(
                new BaseWeight { Uuid = bwUnchanged, Weight = 100, LastUpdated = boundary.AddMinutes(-1) },
                new BaseWeight { Uuid = bwChanged, Weight = 200, LastUpdated = boundary.AddMinutes(1) });

            seed.ExperimentalSpecWeights.AddRange(
                new ExperimentalSpecWeight { Uuid = swUnchanged, LastUpdated = boundary.AddMinutes(-1) },
                new ExperimentalSpecWeight { Uuid = swChanged, PyromancerOffset = 5, LastUpdated = boundary.AddMinutes(1) });

            seed.ExperimentalSpecsWl.AddRange(
                new ExperimentalSpecsWl { Uuid = wlUnchanged, LastUpdated = boundary.AddMinutes(-1) },
                new ExperimentalSpecsWl { Uuid = wlChanged, PyromancerWins = 3, LastUpdated = boundary.AddMinutes(1) });

            await seed.SaveChangesAsync();
        }

        await using (var db = new BalancerDbContext(options))
        {
            var service = new TimeService(db, new TestDbContextFactory(options));
            var newWeekId = await service.CreateNewWeekAsync(CancellationToken.None);

            Assert.Equal(1, newWeekId);

            var baseWeekly = await db.BaseWeightsWeekly.Where(x => x.WeekStartDate == newWeekId).ToListAsync();
            Assert.Single(baseWeekly);
            Assert.Equal(bwChanged, baseWeekly[0].Uuid);

            var specWeekly = await db.ExperimentalSpecWeightsWeekly.Where(x => x.WeekStartDate == newWeekId).ToListAsync();
            Assert.Single(specWeekly);
            Assert.Equal(swChanged, specWeekly[0].Uuid);
            Assert.Equal(5, specWeekly[0].PyromancerOffset);

            var wlWeekly = await db.ExperimentalSpecsWlWeekly.Where(x => x.WeekStartDate == newWeekId).ToListAsync();
            Assert.Single(wlWeekly);
            Assert.Equal(wlChanged, wlWeekly[0].Uuid);
            Assert.Equal(3, wlWeekly[0].PyromancerWins);
        }
    }

    [Fact]
    public async Task CreateNewDayAsync_WhenRunTwiceWithoutChanges_DoesNotCreateSecondSnapshot()
    {
        var options = CreateOptions(Guid.NewGuid().ToString());
        var boundary = new DateTime(2026, 4, 1, 0, 0, 0, DateTimeKind.Utc);

        var changedUuid = Guid.NewGuid();

        await using (var seed = new BalancerDbContext(options))
        {
            seed.TimeDays.Add(new TimeDay { Id = 0, Timestamp = boundary });
            seed.BaseWeights.Add(new BaseWeight { Uuid = changedUuid, Weight = 200, LastUpdated = boundary.AddMinutes(1) });
            await seed.SaveChangesAsync();
        }

        await using (var db = new BalancerDbContext(options))
        {
            var service = new TimeService(db, new TestDbContextFactory(options));
            var day1 = await service.CreateNewDayAsync(CancellationToken.None);
            var day2 = await service.CreateNewDayAsync(CancellationToken.None);

            Assert.Equal(1, day1);
            Assert.Equal(2, day2);

            var allDaily = await db.BaseWeightsDaily.ToListAsync();
            Assert.Single(allDaily);
            Assert.Equal(1, allDaily[0].DayStartDate);
        }
    }
}
