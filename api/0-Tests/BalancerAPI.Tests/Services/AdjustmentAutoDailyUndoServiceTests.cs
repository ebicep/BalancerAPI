using BalancerAPI.Business.Services;
using BalancerAPI.Data.Data;
using BalancerAPI.Domain.Models;
using Microsoft.EntityFrameworkCore;

namespace BalancerAPI.Tests.Services;

public class AdjustmentAutoDailyUndoServiceTests
{
    private static readonly Guid U1 = Guid.Parse("a1b2c3d4-e5f6-7890-abcd-ef1234567890");
    private static readonly Guid U2 = Guid.Parse("b2c3d4e5-f6a7-8901-bcde-f12345678901");
    private static readonly DateTime FixedLastUpdated = new(2025, 3, 1, 12, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime OlderBatchDate = new(2025, 3, 1, 10, 0, 0, DateTimeKind.Utc);

    [Fact]
    public async Task UndoAutoDailyAsync_ApplyThenUndo_RestoresWeightsAndTrajectories_DeletesLogs()
    {
        await using var db = CreateDbContext();
        db.AdjustmentDaily.AddRange(
            new AdjustmentDaily { Uuid = U1, Trajectory = 3 },
            new AdjustmentDaily { Uuid = U2, Trajectory = -4 });
        db.BaseWeights.AddRange(
            new BaseWeight { Uuid = U1, Weight = 100, LastUpdated = FixedLastUpdated },
            new BaseWeight { Uuid = U2, Weight = 200, LastUpdated = FixedLastUpdated });
        await db.SaveChangesAsync();

        var service = new AdjustmentAutoDailyService(db);
        var applied = await service.ApplyAutoDailyAsync(CancellationToken.None);

        var undo = await service.UndoAutoDailyAsync(applied, CancellationToken.None);

        Assert.True(undo.Success);
        Assert.Equal(200, undo.StatusCode);
        Assert.NotNull(undo.Response);
        Assert.Equal(applied.Count, undo.Response!.Count);
        Assert.Equal(applied.Date, undo.Response.Date);

        var u1Applied = applied.Adjusted.Single(e => e.Uuid == U1);
        var u1Undone = undo.Response.Adjusted.Single(e => e.Uuid == U1);
        Assert.Equal(u1Applied.CurrentWeight, u1Undone.PreviousWeight);
        Assert.Equal(u1Applied.PreviousWeight, u1Undone.CurrentWeight);
        Assert.Equal(u1Applied.NewTrajectory, u1Undone.PreviousTrajectory);
        Assert.Equal(u1Applied.PreviousTrajectory, u1Undone.NewTrajectory);

        var u2Applied = applied.Adjusted.Single(e => e.Uuid == U2);
        var u2Undone = undo.Response.Adjusted.Single(e => e.Uuid == U2);
        Assert.Equal(u2Applied.CurrentWeight, u2Undone.PreviousWeight);
        Assert.Equal(u2Applied.PreviousWeight, u2Undone.CurrentWeight);
        Assert.Equal(u2Applied.NewTrajectory, u2Undone.PreviousTrajectory);
        Assert.Equal(u2Applied.PreviousTrajectory, u2Undone.NewTrajectory);

        Assert.Equal(100, (await db.BaseWeights.SingleAsync(x => x.Uuid == U1)).Weight);
        Assert.Equal(200, (await db.BaseWeights.SingleAsync(x => x.Uuid == U2)).Weight);
        Assert.Equal(3, (await db.AdjustmentDaily.SingleAsync(x => x.Uuid == U1)).Trajectory);
        Assert.Equal(-4, (await db.AdjustmentDaily.SingleAsync(x => x.Uuid == U2)).Trajectory);
        Assert.Empty(await db.AdjustmentDailyLogs.ToListAsync());
    }

    [Fact]
    public async Task UndoAutoDailyAsync_WhenRequestDateWithinTolerance_Succeeds_UsesDatabaseBatchDate()
    {
        await using var db = CreateDbContext();
        db.AdjustmentDaily.Add(new AdjustmentDaily { Uuid = U1, Trajectory = 3 });
        db.BaseWeights.Add(new BaseWeight { Uuid = U1, Weight = 100, LastUpdated = FixedLastUpdated });
        await db.SaveChangesAsync();

        var service = new AdjustmentAutoDailyService(db);
        var applied = await service.ApplyAutoDailyAsync(CancellationToken.None);
        var batchDate = (await db.AdjustmentDailyLogs.SingleAsync()).Date;

        var payloadWithSkewedDate = applied with
        {
            Date = applied.Date!.Value.AddTicks(5)
        };

        var undo = await service.UndoAutoDailyAsync(payloadWithSkewedDate, CancellationToken.None);

        Assert.True(undo.Success);
        Assert.Equal(batchDate, undo.Response!.Date);
        Assert.Equal(100, (await db.BaseWeights.SingleAsync(x => x.Uuid == U1)).Weight);
        Assert.Empty(await db.AdjustmentDailyLogs.ToListAsync());
    }

    [Fact]
    public async Task UndoAutoDailyAsync_WhenDateIsNotLatest_Returns409()
    {
        await using var db = CreateDbContext();
        db.AdjustmentDaily.Add(new AdjustmentDaily { Uuid = U1, Trajectory = 3 });
        db.BaseWeights.Add(new BaseWeight { Uuid = U1, Weight = 100, LastUpdated = FixedLastUpdated });
        db.AdjustmentDailyLogs.Add(new AdjustmentDailyLog
        {
            Id = Guid.NewGuid(),
            Uuid = U1,
            PreviousWeight = 90,
            NewWeight = 91,
            Date = OlderBatchDate
        });
        await db.SaveChangesAsync();

        var service = new AdjustmentAutoDailyService(db);
        var applied = await service.ApplyAutoDailyAsync(CancellationToken.None);

        var stalePayload = new AdjustmentAutoDailyResponse(
            1,
            [
                new AdjustmentAutoDailyAdjustedEntry(
                    U1,
                    string.Empty,
                    90,
                    91,
                    3,
                    2)
            ],
            OlderBatchDate);

        var undo = await service.UndoAutoDailyAsync(stalePayload, CancellationToken.None);

        Assert.False(undo.Success);
        Assert.Equal(409, undo.StatusCode);
        Assert.Equal(101, (await db.BaseWeights.SingleAsync(x => x.Uuid == U1)).Weight);
        Assert.Equal(2, await db.AdjustmentDailyLogs.CountAsync());
    }

    [Fact]
    public async Task UndoAutoDailyAsync_WhenWeightChangedSinceApply_Returns409()
    {
        await using var db = CreateDbContext();
        db.AdjustmentDaily.Add(new AdjustmentDaily { Uuid = U1, Trajectory = 3 });
        db.BaseWeights.Add(new BaseWeight { Uuid = U1, Weight = 100, LastUpdated = FixedLastUpdated });
        await db.SaveChangesAsync();

        var service = new AdjustmentAutoDailyService(db);
        var applied = await service.ApplyAutoDailyAsync(CancellationToken.None);

        var baseWeight = await db.BaseWeights.SingleAsync(x => x.Uuid == U1);
        baseWeight.Weight = 999;
        await db.SaveChangesAsync();

        var undo = await service.UndoAutoDailyAsync(applied, CancellationToken.None);

        Assert.False(undo.Success);
        Assert.Equal(409, undo.StatusCode);
        Assert.Single(await db.AdjustmentDailyLogs.ToListAsync());
    }

    [Fact]
    public async Task UndoAutoDailyAsync_WhenLogCountMismatch_Returns409()
    {
        await using var db = CreateDbContext();
        db.AdjustmentDaily.Add(new AdjustmentDaily { Uuid = U1, Trajectory = 3 });
        db.BaseWeights.Add(new BaseWeight { Uuid = U1, Weight = 100, LastUpdated = FixedLastUpdated });
        await db.SaveChangesAsync();

        var service = new AdjustmentAutoDailyService(db);
        var applied = await service.ApplyAutoDailyAsync(CancellationToken.None);

        var badPayload = applied with { Count = 2 };

        var undo = await service.UndoAutoDailyAsync(badPayload, CancellationToken.None);

        Assert.False(undo.Success);
        Assert.Equal(409, undo.StatusCode);
    }

    [Fact]
    public async Task UndoAutoDailyAsync_WhenCountZero_Returns400()
    {
        await using var db = CreateDbContext();
        var service = new AdjustmentAutoDailyService(db);

        var undo = await service.UndoAutoDailyAsync(
            new AdjustmentAutoDailyResponse(0, [], null),
            CancellationToken.None);

        Assert.False(undo.Success);
        Assert.Equal(400, undo.StatusCode);
    }

    [Fact]
    public async Task UndoAutoDailyAsync_WhenDateMissing_Returns400()
    {
        await using var db = CreateDbContext();
        var service = new AdjustmentAutoDailyService(db);

        var undo = await service.UndoAutoDailyAsync(
            new AdjustmentAutoDailyResponse(1, [], null),
            CancellationToken.None);

        Assert.False(undo.Success);
        Assert.Equal(400, undo.StatusCode);
    }

    private static BalancerDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<BalancerDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new BalancerDbContext(options);
    }
}
