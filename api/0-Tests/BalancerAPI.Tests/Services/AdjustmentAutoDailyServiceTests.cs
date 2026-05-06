using BalancerAPI.Business.Services;
using BalancerAPI.Data.Data;
using BalancerAPI.Domain.Models;
using Microsoft.EntityFrameworkCore;

namespace BalancerAPI.Tests.Services;

public class AdjustmentAutoDailyServiceTests
{
    private static readonly Guid U1 = Guid.Parse("a1b2c3d4-e5f6-7890-abcd-ef1234567890");
    private static readonly Guid U2 = Guid.Parse("b2c3d4e5-f6a7-8901-bcde-f12345678901");
    private static readonly Guid U3 = Guid.Parse("c3d4e5f6-a7b8-9012-cdef-123456789012");
    private static readonly DateTime FixedLastUpdated = new(2025, 3, 1, 12, 0, 0, DateTimeKind.Utc);

    [Theory]
    [InlineData(2, 0)]
    [InlineData(-2, 0)]
    [InlineData(0, 0)]
    [InlineData(3, 1)]
    [InlineData(4, 2)]
    [InlineData(5, 3)]
    [InlineData(-3, -1)]
    [InlineData(-4, -2)]
    [InlineData(-5, -3)]
    public void ComputeDelta_MatchesRules(int trajectory, int expectedDelta)
    {
        Assert.Equal(expectedDelta, AdjustmentAutoDailyService.ComputeDelta(trajectory));
    }

    [Fact]
    public async Task ApplyAutoDailyAsync_WhenTrajectoryInBand_NoChange_OmitsFromResponse()
    {
        await using var db = CreateDbContext();
        db.AdjustmentDaily.Add(new AdjustmentDaily { Uuid = U1, Trajectory = 2 });
        db.BaseWeights.Add(new BaseWeight { Uuid = U1, Weight = 100, LastUpdated = FixedLastUpdated });
        await db.SaveChangesAsync();

        var service = new AdjustmentAutoDailyService(db);
        var result = await service.ApplyAutoDailyAsync(CancellationToken.None);

        Assert.Equal(0, result.Count);
        Assert.Empty(result.Adjusted);
        Assert.Equal(2, (await db.AdjustmentDaily.SingleAsync(x => x.Uuid == U1)).Trajectory);
        Assert.Equal(100, (await db.BaseWeights.SingleAsync(x => x.Uuid == U1)).Weight);
    }

    [Fact]
    public async Task ApplyAutoDailyAsync_WhenPositiveTrajectory_AdjustsWeightAndSetsTrajectoryTo2()
    {
        await using var db = CreateDbContext();
        db.AdjustmentDaily.Add(new AdjustmentDaily { Uuid = U1, Trajectory = 3 });
        db.BaseWeights.Add(new BaseWeight { Uuid = U1, Weight = 100, LastUpdated = FixedLastUpdated });
        await db.SaveChangesAsync();

        var before = DateTime.UtcNow;
        var service = new AdjustmentAutoDailyService(db);
        var result = await service.ApplyAutoDailyAsync(CancellationToken.None);
        var after = DateTime.UtcNow;

        Assert.Equal(1, result.Count);
        var entry = Assert.Single(result.Adjusted);
        Assert.Equal(U1, entry.Uuid);
        Assert.Equal(string.Empty, entry.Name);
        Assert.Equal(100, entry.PreviousWeight);
        Assert.Equal(101, entry.CurrentWeight);
        Assert.Equal(3, entry.PreviousTrajectory);
        Assert.Equal(2, entry.NewTrajectory);
        Assert.Equal(2, (await db.AdjustmentDaily.SingleAsync(x => x.Uuid == U1)).Trajectory);
        Assert.Equal(101, (await db.BaseWeights.SingleAsync(x => x.Uuid == U1)).Weight);

        var log = Assert.Single(await db.AdjustmentDailyLogs.ToListAsync());
        Assert.Equal(U1, log.Uuid);
        Assert.Equal(100, log.PreviousWeight);
        Assert.Equal(101, log.NewWeight);
        Assert.True(log.Date >= before && log.Date <= after);
    }

    [Fact]
    public async Task ApplyAutoDailyAsync_WhenNegativeTrajectory_AdjustsWeightAndSetsTrajectoryToNegative2()
    {
        await using var db = CreateDbContext();
        db.AdjustmentDaily.Add(new AdjustmentDaily { Uuid = U1, Trajectory = -3 });
        db.BaseWeights.Add(new BaseWeight { Uuid = U1, Weight = 100, LastUpdated = FixedLastUpdated });
        await db.SaveChangesAsync();

        var service = new AdjustmentAutoDailyService(db);
        var result = await service.ApplyAutoDailyAsync(CancellationToken.None);

        Assert.Equal(1, result.Count);
        var entry = Assert.Single(result.Adjusted);
        Assert.Equal(99, entry.CurrentWeight);
        Assert.Equal(-3, entry.PreviousTrajectory);
        Assert.Equal(-2, entry.NewTrajectory);
        Assert.Equal(-2, (await db.AdjustmentDaily.SingleAsync(x => x.Uuid == U1)).Trajectory);
    }

    [Fact]
    public async Task ApplyAutoDailyAsync_WhenNoBaseWeight_SkipsPlayer()
    {
        await using var db = CreateDbContext();
        db.AdjustmentDaily.Add(new AdjustmentDaily { Uuid = U1, Trajectory = 5 });
        await db.SaveChangesAsync();

        var service = new AdjustmentAutoDailyService(db);
        var result = await service.ApplyAutoDailyAsync(CancellationToken.None);

        Assert.Equal(0, result.Count);
        Assert.Empty(result.Adjusted);
        Assert.Equal(5, (await db.AdjustmentDaily.SingleAsync(x => x.Uuid == U1)).Trajectory);
    }

    [Fact]
    public async Task ApplyAutoDailyAsync_SecondInvocationAfterAdjust_DoesNotChangeWeightAgain()
    {
        await using var db = CreateDbContext();
        db.AdjustmentDaily.Add(new AdjustmentDaily { Uuid = U1, Trajectory = 3 });
        db.BaseWeights.Add(new BaseWeight { Uuid = U1, Weight = 100, LastUpdated = FixedLastUpdated });
        await db.SaveChangesAsync();

        var service = new AdjustmentAutoDailyService(db);
        var first = await service.ApplyAutoDailyAsync(CancellationToken.None);
        Assert.Equal(1, first.Count);
        Assert.Equal(101, (await db.BaseWeights.SingleAsync(x => x.Uuid == U1)).Weight);

        var second = await service.ApplyAutoDailyAsync(CancellationToken.None);
        Assert.Equal(0, second.Count);
        Assert.Empty(second.Adjusted);
        Assert.Equal(101, (await db.BaseWeights.SingleAsync(x => x.Uuid == U1)).Weight);
        Assert.Equal(2, (await db.AdjustmentDaily.SingleAsync(x => x.Uuid == U1)).Trajectory);
        Assert.Single(await db.AdjustmentDailyLogs.ToListAsync());
    }

    [Fact]
    public async Task ApplyAutoDailyAsync_ResolvesNameFromNamesTable()
    {
        await using var db = CreateDbContext();
        db.AdjustmentDaily.Add(new AdjustmentDaily { Uuid = U1, Trajectory = 3 });
        db.BaseWeights.Add(new BaseWeight { Uuid = U1, Weight = 50, LastUpdated = FixedLastUpdated });
        db.Names.Add(new PlayerName { Uuid = U1, Name = "PlayerOne" });
        await db.SaveChangesAsync();

        var service = new AdjustmentAutoDailyService(db);
        var result = await service.ApplyAutoDailyAsync(CancellationToken.None);

        var entry = Assert.Single(result.Adjusted);
        Assert.Equal("PlayerOne", entry.Name);
        Assert.Equal(3, entry.PreviousTrajectory);
        Assert.Equal(2, entry.NewTrajectory);
    }

    [Fact]
    public async Task ApplyAutoDailyAsync_WhenEmptyAdjustmentDaily_ReturnsZeroAmount()
    {
        await using var db = CreateDbContext();

        var service = new AdjustmentAutoDailyService(db);
        var result = await service.ApplyAutoDailyAsync(CancellationToken.None);

        Assert.Equal(0, result.Count);
        Assert.Empty(result.Adjusted);
        Assert.Empty(await db.AdjustmentDailyLogs.ToListAsync());
    }

    [Fact]
    public async Task ApplyAutoDailyAsync_MultiplePlayers_AllAdjusted()
    {
        await using var db = CreateDbContext();
        db.AdjustmentDaily.AddRange(
            new AdjustmentDaily { Uuid = U1, Trajectory = 3 },
            new AdjustmentDaily { Uuid = U2, Trajectory = -4 },
            new AdjustmentDaily { Uuid = U3, Trajectory = 1 });
        db.BaseWeights.AddRange(
            new BaseWeight { Uuid = U1, Weight = 100, LastUpdated = FixedLastUpdated },
            new BaseWeight { Uuid = U2, Weight = 200, LastUpdated = FixedLastUpdated },
            new BaseWeight { Uuid = U3, Weight = 300, LastUpdated = FixedLastUpdated });
        await db.SaveChangesAsync();

        var service = new AdjustmentAutoDailyService(db);
        var result = await service.ApplyAutoDailyAsync(CancellationToken.None);

        Assert.Equal(2, result.Count);
        Assert.Equal(2, result.Adjusted.Count);
        var u1Entry = result.Adjusted.Single(e => e.Uuid == U1);
        Assert.Equal(3, u1Entry.PreviousTrajectory);
        Assert.Equal(2, u1Entry.NewTrajectory);
        var u2Entry = result.Adjusted.Single(e => e.Uuid == U2);
        Assert.Equal(-4, u2Entry.PreviousTrajectory);
        Assert.Equal(-2, u2Entry.NewTrajectory);
        Assert.Equal(101, (await db.BaseWeights.SingleAsync(x => x.Uuid == U1)).Weight);
        Assert.Equal(198, (await db.BaseWeights.SingleAsync(x => x.Uuid == U2)).Weight);
        Assert.Equal(300, (await db.BaseWeights.SingleAsync(x => x.Uuid == U3)).Weight);
        Assert.Equal(1, (await db.AdjustmentDaily.SingleAsync(x => x.Uuid == U3)).Trajectory);
        Assert.Equal(2, await db.AdjustmentDailyLogs.CountAsync());
    }

    private static BalancerDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<BalancerDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new BalancerDbContext(options);
    }
}
