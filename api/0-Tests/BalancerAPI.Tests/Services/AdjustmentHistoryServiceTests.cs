using BalancerAPI.Business.Services;
using BalancerAPI.Data.Data;
using BalancerAPI.Domain.Models;
using Microsoft.EntityFrameworkCore;

namespace BalancerAPI.Tests.Services;

public class AdjustmentHistoryServiceTests
{
    private static readonly Guid TestUuid = Guid.Parse("a1b2c3d4-e5f6-7890-abcd-ef1234567890");
    private static readonly DateTime SharedDate = new(2026, 5, 18, 12, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime EarlierDate = new(2026, 5, 17, 12, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime FixedLastUpdated = new(2026, 1, 10, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public async Task GetBaseHistoryAsync_WhenNoBaseWeight_ReturnsNull()
    {
        await using var db = CreateDbContext();
        var service = new AdjustmentHistoryService(db);

        var result = await service.GetBaseHistoryAsync(TestUuid, CancellationToken.None);

        Assert.Null(result);
    }

    [Fact]
    public async Task GetBaseHistoryAsync_WhenNoLogs_ReturnsEmptyEntries()
    {
        await using var db = CreateDbContext();
        db.BaseWeights.Add(new BaseWeight
        {
            Uuid = TestUuid,
            Weight = 100,
            LastUpdated = FixedLastUpdated,
        });
        db.Names.Add(new PlayerName { Uuid = TestUuid, Name = "Player", PreviousNames = [] });
        await db.SaveChangesAsync();

        var service = new AdjustmentHistoryService(db);
        var result = await service.GetBaseHistoryAsync(TestUuid, CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal("Player", result.Name);
        Assert.Empty(result.Entries);
    }

    [Fact]
    public async Task GetBaseHistoryAsync_MergesAndSortsByDateThenId()
    {
        var autoId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var manualId = Guid.Parse("22222222-2222-2222-2222-222222222222");
        var olderManualId = Guid.Parse("33333333-3333-3333-3333-333333333333");

        await using var db = CreateDbContext();
        db.BaseWeights.Add(new BaseWeight
        {
            Uuid = TestUuid,
            Weight = 100,
            LastUpdated = FixedLastUpdated,
        });
        db.Names.Add(new PlayerName { Uuid = TestUuid, Name = "Player", PreviousNames = [] });
        db.AdjustmentDailyLogs.Add(new AdjustmentDailyLog
        {
            Id = autoId,
            Uuid = TestUuid,
            PreviousWeight = 10,
            NewWeight = 11,
            Date = SharedDate,
        });
        db.AdjustmentManualDailyLogs.Add(new AdjustmentManualDailyLog
        {
            Id = manualId,
            Uuid = TestUuid,
            PreviousWeight = 20,
            NewWeight = 25,
            Date = SharedDate,
        });
        db.AdjustmentManualDailyLogs.Add(new AdjustmentManualDailyLog
        {
            Id = olderManualId,
            Uuid = TestUuid,
            PreviousWeight = 5,
            NewWeight = 6,
            Date = EarlierDate,
        });
        await db.SaveChangesAsync();

        var service = new AdjustmentHistoryService(db);
        var result = await service.GetBaseHistoryAsync(TestUuid, CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(3, result.Entries.Count);
        Assert.Equal(manualId, result.Entries[0].Id);
        Assert.Equal("manual", result.Entries[0].Source);
        Assert.Equal(autoId, result.Entries[1].Id);
        Assert.Equal("auto", result.Entries[1].Source);
        Assert.Equal(olderManualId, result.Entries[2].Id);
    }

    [Fact]
    public async Task GetSpecHistoryAsync_WhenNoSpecRow_ReturnsNull()
    {
        await using var db = CreateDbContext();
        var service = new AdjustmentHistoryService(db);

        var result = await service.GetSpecHistoryAsync(TestUuid, CancellationToken.None);

        Assert.Null(result);
    }

    [Fact]
    public async Task GetSpecHistoryAsync_DerivesAutoWeeklyWeightsAndOffsets()
    {
        var autoId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");

        await using var db = CreateDbContext();
        db.ExperimentalSpecWeights.Add(new ExperimentalSpecWeight
        {
            Uuid = TestUuid,
            LastUpdated = FixedLastUpdated,
            PyromancerOffset = 7,
        });
        db.Names.Add(new PlayerName { Uuid = TestUuid, Name = "SpecPlayer", PreviousNames = [] });
        db.AdjustmentWeeklyLogs.Add(new AdjustmentWeeklyLog
        {
            Id = autoId,
            WeekKey = 3,
            Uuid = TestUuid,
            Spec = "Pyromancer",
            Wins = 5,
            Losses = 1,
            Adjusted = 3,
            PreviousWeight = 90,
            PreviousOffset = 10,
            Date = SharedDate,
        });
        await db.SaveChangesAsync();

        var service = new AdjustmentHistoryService(db);
        var result = await service.GetSpecHistoryAsync(TestUuid, CancellationToken.None);

        Assert.NotNull(result);
        var entry = Assert.Single(result.Entries);
        Assert.Equal("auto", entry.Source);
        Assert.Equal(3, entry.WeekKey);
        Assert.Equal(10, entry.PreviousOffset);
        Assert.Equal(7, entry.NewOffset);
        Assert.Equal(90, entry.PreviousSpecWeight);
        Assert.Equal(93, entry.NewSpecWeight);
        Assert.Equal(5, entry.Wins);
        Assert.Equal(1, entry.Losses);
        Assert.Equal(3, entry.Adjusted);
    }

    private static BalancerDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<BalancerDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new BalancerDbContext(options);
    }
}
