using BalancerAPI.Business.Services;
using BalancerAPI.Data.Data;
using BalancerAPI.Domain.Models;
using Microsoft.EntityFrameworkCore;

namespace BalancerAPI.Tests.Services;

public class WeeklyAdjustPatchServiceTests
{
    private static readonly Guid U1 = Guid.Parse("d4e5f6a7-b8c9-0123-def0-123456789abc");
    private static readonly DateTime FixedLastUpdated = new(2025, 3, 1, 12, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime BeforeCutoff = WeeklyAdjustPatchCutoff.Utc.AddHours(-1);
    private static readonly DateTime OnCutoff = WeeklyAdjustPatchCutoff.Utc;
    private const string Spec = "Pyromancer";

    [Theory]
    [InlineData(0, 10, true, -10, true)]
    [InlineData(5, -10, true, 15, false)]
    [InlineData(-5, 0, false, -5, true)]
    [InlineData(5, 0, false, 5, true)]
    [InlineData(4, 3, true, 1, false)]
    [InlineData(-5, -3, true, -2, true)]
    public void ComputePatch_MatchesManualAwareRules(
        int autoSpecDelta,
        int manualSpecDelta,
        bool hasManual,
        int expectedPatch,
        bool expectedExecute)
    {
        var (patch, execute, net) = WeeklyAdjustPatchService.ComputePatch(autoSpecDelta, manualSpecDelta, hasManual);
        Assert.Equal(expectedPatch, patch);
        Assert.Equal(expectedExecute, execute);
        Assert.Equal(expectedPatch, net);
    }

    [Fact]
    public async Task ApplyPatchAsync_Example1_ManualBuffAutoBuff_ExecutesNetBuff()
    {
        await using var db = await SeedPlayerAsync(offset: 10, baseWeight: 100);
        db.AdjustmentManualWeeklyLogs.Add(ManualLog(+10, offset: 20, newOffset: 10));
        db.AdjustmentWeeklyLogs.Add(AutoLog(-5, offset: 20));
        await db.SaveChangesAsync();

        var result = await RunPatchAsync(db);
        var entry = Assert.Single(result.Adjusted);
        Assert.True(entry.Executed);
        Assert.Equal(-5, entry.AutoSpecDelta);
        Assert.Equal(10, entry.ManualSpecDelta);
        Assert.Equal(-15, entry.NetSpecDelta);
        Assert.Equal(10, entry.PreviousOffset);
        Assert.Equal(25, entry.CurrentOffset);
        Assert.Equal(90, entry.PreviousWeight);
        Assert.Equal(75, entry.CurrentWeight);
        Assert.Equal(1, result.Count);
    }

    [Fact]
    public async Task ApplyPatchAsync_Example2_ManualNerfAutoNerf_Skips()
    {
        await using var db = await SeedPlayerAsync(offset: 30, baseWeight: 100);
        db.AdjustmentManualWeeklyLogs.Add(ManualLog(-10, offset: 20, newOffset: 30));
        db.AdjustmentWeeklyLogs.Add(AutoLog(5, offset: 20));
        await db.SaveChangesAsync();

        var result = await RunPatchAsync(db);
        var entry = Assert.Single(result.Adjusted);
        Assert.False(entry.Executed);
        Assert.Equal(15, entry.NetSpecDelta);
        Assert.Equal(30, entry.PreviousOffset);
        Assert.Equal(30, entry.CurrentOffset);
        Assert.Equal(70, entry.PreviousWeight);
        Assert.Equal(70, entry.CurrentWeight);
        Assert.Equal(0, result.Count);
    }

    [Fact]
    public async Task ApplyPatchAsync_Example3_AutoOnlyBuff_Executes()
    {
        await using var db = await SeedPlayerAsync(offset: 20, baseWeight: 100);
        db.AdjustmentWeeklyLogs.Add(AutoLog(-5, offset: 20));
        await db.SaveChangesAsync();

        var result = await RunPatchAsync(db);
        var entry = Assert.Single(result.Adjusted);
        Assert.True(entry.Executed);
        Assert.Equal(-5, entry.PatchAdjusted);
        Assert.Equal(25, entry.CurrentOffset);
        Assert.Equal(1, result.Count);
    }

    [Fact]
    public async Task ApplyPatchAsync_Example4_AutoOnlyNerf_Executes()
    {
        await using var db = await SeedPlayerAsync(offset: 20, baseWeight: 100);
        db.AdjustmentWeeklyLogs.Add(AutoLog(5, offset: 20));
        await db.SaveChangesAsync();

        var result = await RunPatchAsync(db);
        var entry = Assert.Single(result.Adjusted);
        Assert.True(entry.Executed);
        Assert.Equal(5, entry.PatchAdjusted);
        Assert.Equal(15, entry.CurrentOffset);
        Assert.Equal(1, result.Count);
    }

    [Fact]
    public async Task ApplyPatchAsync_ExcludesWeekKeyMinusOne_FromAutoSum()
    {
        await using var db = await SeedPlayerAsync(offset: 20, baseWeight: 100);
        db.AdjustmentWeeklyLogs.Add(AutoLog(-5, offset: 20, weekKey: -1));
        db.AdjustmentWeeklyLogs.Add(AutoLog(-3, offset: 20, weekKey: 3));
        await db.SaveChangesAsync();

        var result = await RunPatchAsync(db);
        var entry = Assert.Single(result.Adjusted);
        Assert.Equal(-3, entry.AutoSpecDelta);
        Assert.True(entry.Executed);
    }

    [Fact]
    public async Task ApplyPatchAsync_ExcludesLogsOnOrAfterCutoff()
    {
        await using var db = await SeedPlayerAsync(offset: 20, baseWeight: 100);
        db.AdjustmentWeeklyLogs.Add(AutoLog(-5, offset: 20, date: BeforeCutoff));
        db.AdjustmentWeeklyLogs.Add(AutoLog(-7, offset: 20, date: OnCutoff));
        await db.SaveChangesAsync();

        var result = await RunPatchAsync(db);
        var entry = Assert.Single(result.Adjusted);
        Assert.Equal(-5, entry.AutoSpecDelta);
    }

    [Fact]
    public async Task ApplyPatchAsync_WhenExecuted_PersistsOffsetsAndPatchLogs()
    {
        await using var db = await SeedPlayerAsync(offset: 20, baseWeight: 100);
        db.AdjustmentWeeklyLogs.Add(AutoLog(-5, offset: 20));
        await db.SaveChangesAsync();

        await RunPatchAsync(db);

        var sw = await db.ExperimentalSpecWeights.SingleAsync(x => x.Uuid == U1);
        Assert.Equal(25, sw.PyromancerOffset);
        var patchLog = Assert.Single(await db.AdjustmentWeeklyLogs.Where(x => x.WeekKey == -1).ToListAsync());
        Assert.Equal(-5, patchLog.Adjusted);
        Assert.Equal(0, patchLog.Wins);
        Assert.Equal(0, patchLog.Losses);
    }

    private static AdjustmentWeeklyLog AutoLog(
        int adjusted,
        int offset,
        int weekKey = 4,
        DateTime? date = null) =>
        new()
        {
            Id = Guid.NewGuid(),
            WeekKey = weekKey,
            Uuid = U1,
            Spec = Spec,
            Wins = 0,
            Losses = 0,
            Adjusted = adjusted,
            PreviousWeight = 100 - offset,
            PreviousOffset = offset,
            Date = date ?? BeforeCutoff
        };

    private static AdjustmentManualWeeklyLog ManualLog(
        int specWeightDelta,
        int offset,
        int newOffset,
        DateTime? date = null)
    {
        var previousSpecWeight = 100 - offset;
        return new AdjustmentManualWeeklyLog
        {
            Id = Guid.NewGuid(),
            Uuid = U1,
            Spec = Spec,
            PreviousOffset = offset,
            NewOffset = newOffset,
            BaseWeight = 100,
            PreviousSpecWeight = previousSpecWeight,
            NewSpecWeight = previousSpecWeight + specWeightDelta,
            Date = date ?? BeforeCutoff
        };
    }

    private static async Task<BalancerDbContext> SeedPlayerAsync(int offset, int baseWeight)
    {
        var db = CreateDbContext();
        db.BaseWeights.Add(new BaseWeight { Uuid = U1, Weight = baseWeight, LastUpdated = FixedLastUpdated });
        db.ExperimentalSpecWeights.Add(new ExperimentalSpecWeight
        {
            Uuid = U1,
            PyromancerOffset = offset,
            LastUpdated = FixedLastUpdated
        });
        db.Names.Add(new PlayerName { Uuid = U1, Name = "PlayerOne" });
        await db.SaveChangesAsync();
        return db;
    }

    private static async Task<WeeklyAdjustPatchResponse> RunPatchAsync(BalancerDbContext db)
    {
        var service = new WeeklyAdjustPatchService(db);
        return await service.ApplyPatchAsync(CancellationToken.None);
    }

    private static BalancerDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<BalancerDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new BalancerDbContext(options);
    }
}
