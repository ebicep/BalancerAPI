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

            seed.AdjustmentDaily.AddRange(
                new AdjustmentDaily { Uuid = Guid.NewGuid(), Trajectory = 2 },
                new AdjustmentDaily { Uuid = Guid.NewGuid(), Trajectory = -1 });

            await seed.SaveChangesAsync();
        }

        await using (var db = new BalancerDbContext(options))
        {
            var service = new TimeService(db, new TestDbContextFactory(options));
            var newDayId = await service.CreateNewDayAsync(CancellationToken.None);

            Assert.Equal(1, newDayId);
            Assert.Empty(await db.AdjustmentDaily.ToListAsync());

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

    [Fact]
    public async Task UndoDayAsync_RemovesSnapshotsAndTimeDay()
    {
        var options = CreateOptions(Guid.NewGuid().ToString());
        var keepDayId = 10;
        var removeDayId = 11;

        await using (var seed = new BalancerDbContext(options))
        {
            seed.TimeDays.AddRange(
                new TimeDay { Id = keepDayId, Timestamp = DateTime.UtcNow.AddDays(-1) },
                new TimeDay { Id = removeDayId, Timestamp = DateTime.UtcNow });

            seed.BaseWeightsDaily.AddRange(
                new BaseWeightDaily { Uuid = Guid.NewGuid(), DayStartDate = keepDayId, Weight = 100 },
                new BaseWeightDaily { Uuid = Guid.NewGuid(), DayStartDate = removeDayId, Weight = 200 });

            seed.ExperimentalSpecsWlDaily.AddRange(
                new ExperimentalSpecsWlDaily { Uuid = Guid.NewGuid(), DayStartDate = keepDayId, PyromancerWins = 1 },
                new ExperimentalSpecsWlDaily { Uuid = Guid.NewGuid(), DayStartDate = removeDayId, PyromancerWins = 2 });

            await seed.SaveChangesAsync();
        }

        await using (var db = new BalancerDbContext(options))
        {
            var service = new TimeService(db, new TestDbContextFactory(options));
            var wasUndone = await service.UndoDayAsync(removeDayId, CancellationToken.None);

            Assert.True(wasUndone);
            Assert.False(await db.TimeDays.AnyAsync(x => x.Id == removeDayId));
            Assert.Empty(await db.BaseWeightsDaily.Where(x => x.DayStartDate == removeDayId).ToListAsync());
            Assert.Empty(await db.ExperimentalSpecsWlDaily.Where(x => x.DayStartDate == removeDayId).ToListAsync());

            Assert.True(await db.TimeDays.AnyAsync(x => x.Id == keepDayId));
            Assert.Single(await db.BaseWeightsDaily.Where(x => x.DayStartDate == keepDayId).ToListAsync());
            Assert.Single(await db.ExperimentalSpecsWlDaily.Where(x => x.DayStartDate == keepDayId).ToListAsync());
        }
    }

    [Fact]
    public async Task UndoWeekAsync_RemovesSnapshotsAndTimeWeek()
    {
        var options = CreateOptions(Guid.NewGuid().ToString());
        var keepWeekId = 20;
        var removeWeekId = 21;

        await using (var seed = new BalancerDbContext(options))
        {
            seed.TimeWeeks.AddRange(
                new TimeWeek { Id = keepWeekId, Timestamp = DateTime.UtcNow.AddDays(-7) },
                new TimeWeek { Id = removeWeekId, Timestamp = DateTime.UtcNow });

            seed.BaseWeightsWeekly.AddRange(
                new BaseWeightWeekly { Uuid = Guid.NewGuid(), WeekStartDate = keepWeekId, Weight = 100 },
                new BaseWeightWeekly { Uuid = Guid.NewGuid(), WeekStartDate = removeWeekId, Weight = 200 });

            seed.ExperimentalSpecWeightsWeekly.AddRange(
                new ExperimentalSpecWeightWeekly { Uuid = Guid.NewGuid(), WeekStartDate = keepWeekId, PyromancerOffset = 1 },
                new ExperimentalSpecWeightWeekly { Uuid = Guid.NewGuid(), WeekStartDate = removeWeekId, PyromancerOffset = 2 });

            seed.ExperimentalSpecsWlWeekly.AddRange(
                new ExperimentalSpecsWlWeekly { Uuid = Guid.NewGuid(), WeekStartDate = keepWeekId, PyromancerWins = 1 },
                new ExperimentalSpecsWlWeekly { Uuid = Guid.NewGuid(), WeekStartDate = removeWeekId, PyromancerWins = 2 });

            await seed.SaveChangesAsync();
        }

        await using (var db = new BalancerDbContext(options))
        {
            var service = new TimeService(db, new TestDbContextFactory(options));
            var wasUndone = await service.UndoWeekAsync(removeWeekId, CancellationToken.None);

            Assert.True(wasUndone);
            Assert.False(await db.TimeWeeks.AnyAsync(x => x.Id == removeWeekId));
            Assert.Empty(await db.BaseWeightsWeekly.Where(x => x.WeekStartDate == removeWeekId).ToListAsync());
            Assert.Empty(await db.ExperimentalSpecWeightsWeekly.Where(x => x.WeekStartDate == removeWeekId).ToListAsync());
            Assert.Empty(await db.ExperimentalSpecsWlWeekly.Where(x => x.WeekStartDate == removeWeekId).ToListAsync());

            Assert.True(await db.TimeWeeks.AnyAsync(x => x.Id == keepWeekId));
            Assert.Single(await db.BaseWeightsWeekly.Where(x => x.WeekStartDate == keepWeekId).ToListAsync());
            Assert.Single(await db.ExperimentalSpecWeightsWeekly.Where(x => x.WeekStartDate == keepWeekId).ToListAsync());
            Assert.Single(await db.ExperimentalSpecsWlWeekly.Where(x => x.WeekStartDate == keepWeekId).ToListAsync());
        }
    }

    [Fact]
    public async Task UndoSeasonAsync_RemovesTimeSeason()
    {
        var options = CreateOptions(Guid.NewGuid().ToString());
        var keepSeasonId = 30;
        var removeSeasonId = 31;

        await using (var seed = new BalancerDbContext(options))
        {
            seed.TimeSeasons.AddRange(
                new TimeSeason { Id = keepSeasonId, Timestamp = DateTime.UtcNow.AddDays(-30) },
                new TimeSeason { Id = removeSeasonId, Timestamp = DateTime.UtcNow });
            await seed.SaveChangesAsync();
        }

        await using (var db = new BalancerDbContext(options))
        {
            var service = new TimeService(db, new TestDbContextFactory(options));
            var wasUndone = await service.UndoSeasonAsync(removeSeasonId, CancellationToken.None);

            Assert.True(wasUndone);
            Assert.False(await db.TimeSeasons.AnyAsync(x => x.Id == removeSeasonId));
            Assert.True(await db.TimeSeasons.AnyAsync(x => x.Id == keepSeasonId));
        }
    }

    [Fact]
    public async Task UndoMethods_WhenIdDoesNotExist_ReturnFalseAndDoNotChangeData()
    {
        var options = CreateOptions(Guid.NewGuid().ToString());

        await using (var seed = new BalancerDbContext(options))
        {
            seed.TimeDays.Add(new TimeDay { Id = 1, Timestamp = DateTime.UtcNow });
            seed.TimeWeeks.Add(new TimeWeek { Id = 1, Timestamp = DateTime.UtcNow });
            seed.TimeSeasons.Add(new TimeSeason { Id = 1, Timestamp = DateTime.UtcNow });
            seed.BaseWeightsDaily.Add(new BaseWeightDaily { Uuid = Guid.NewGuid(), DayStartDate = 1, Weight = 100 });
            seed.BaseWeightsWeekly.Add(new BaseWeightWeekly { Uuid = Guid.NewGuid(), WeekStartDate = 1, Weight = 100 });
            seed.ExperimentalSpecWeightsWeekly.Add(new ExperimentalSpecWeightWeekly
                { Uuid = Guid.NewGuid(), WeekStartDate = 1, PyromancerOffset = 1 });
            seed.ExperimentalSpecsWlDaily.Add(new ExperimentalSpecsWlDaily
                { Uuid = Guid.NewGuid(), DayStartDate = 1, PyromancerWins = 1 });
            seed.ExperimentalSpecsWlWeekly.Add(new ExperimentalSpecsWlWeekly
                { Uuid = Guid.NewGuid(), WeekStartDate = 1, PyromancerWins = 1 });
            await seed.SaveChangesAsync();
        }

        await using (var db = new BalancerDbContext(options))
        {
            var service = new TimeService(db, new TestDbContextFactory(options));

            Assert.False(await service.UndoDayAsync(999, CancellationToken.None));
            Assert.False(await service.UndoWeekAsync(999, CancellationToken.None));
            Assert.False(await service.UndoSeasonAsync(999, CancellationToken.None));

            Assert.Equal(1, await db.TimeDays.CountAsync());
            Assert.Equal(1, await db.TimeWeeks.CountAsync());
            Assert.Equal(1, await db.TimeSeasons.CountAsync());
            Assert.Equal(1, await db.BaseWeightsDaily.CountAsync());
            Assert.Equal(1, await db.BaseWeightsWeekly.CountAsync());
            Assert.Equal(1, await db.ExperimentalSpecWeightsWeekly.CountAsync());
            Assert.Equal(1, await db.ExperimentalSpecsWlDaily.CountAsync());
            Assert.Equal(1, await db.ExperimentalSpecsWlWeekly.CountAsync());
        }
    }
}
