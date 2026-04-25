using BalancerAPI.Business.Services;
using BalancerAPI.Data.Data;
using BalancerAPI.Domain.Models;
using Microsoft.EntityFrameworkCore;

namespace BalancerAPI.Tests.Services;

public class ManualWeightAdjustmentServiceTests
{
    private static readonly Guid U1 = Guid.Parse("a1b2c3d4-e5f6-7890-abcd-ef1234567890");
    private static readonly Guid U2 = Guid.Parse("b2c3d4e5-f6a7-8901-bcde-f12345678901");
    private static readonly DateTime FixedLastUpdated = new(2025, 3, 1, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public async Task PatchBaseAsync_ByUuid_AdjustsWeight()
    {
        await using var db = CreateDbContext();
        db.BaseWeights.Add(new BaseWeight { Uuid = U1, Weight = 100, LastUpdated = FixedLastUpdated });
        await db.SaveChangesAsync();

        var sut = new ManualWeightAdjustmentService(db);
        var result = await sut.PatchBaseAsync(U1.ToString(), new ManualAdjustBaseRequest(5), CancellationToken.None);

        Assert.True(result.Success);
        Assert.NotNull(result.Response);
        Assert.Equal(U1, result.Response.Uuid);
        Assert.Equal(string.Empty, result.Response.Name);
        Assert.Equal(100, result.Response.PreviousWeight);
        Assert.Equal(105, result.Response.NewWeight);
        Assert.Equal(105, (await db.BaseWeights.SingleAsync(x => x.Uuid == U1)).Weight);
        var log = await db.AdjustmentManualDailyLogs.SingleAsync(x => x.Uuid == U1);
        Assert.Equal(result.Response.PreviousWeight, log.PreviousWeight);
        Assert.Equal(result.Response.NewWeight, log.NewWeight);
    }

    [Fact]
    public async Task PatchBaseAsync_ByNameCaseInsensitive_AdjustsWeight()
    {
        await using var db = CreateDbContext();
        db.Names.Add(new PlayerName { Uuid = U1, Name = "TestPlayer", PreviousNames = [] });
        db.BaseWeights.Add(new BaseWeight { Uuid = U1, Weight = 50, LastUpdated = FixedLastUpdated });
        await db.SaveChangesAsync();

        var sut = new ManualWeightAdjustmentService(db);
        var result = await sut.PatchBaseAsync("testplayer", new ManualAdjustBaseRequest(-10), CancellationToken.None);

        Assert.True(result.Success);
        Assert.NotNull(result.Response);
        Assert.Equal("TestPlayer", result.Response.Name);
        Assert.Equal(40, result.Response.NewWeight);
        var log = await db.AdjustmentManualDailyLogs.SingleAsync(x => x.Uuid == U1);
        Assert.Equal(result.Response.PreviousWeight, log.PreviousWeight);
        Assert.Equal(result.Response.NewWeight, log.NewWeight);
    }

    [Fact]
    public async Task PatchBaseAsync_WhenBaseRowMissing_Returns404()
    {
        await using var db = CreateDbContext();
        var sut = new ManualWeightAdjustmentService(db);
        var result = await sut.PatchBaseAsync(U1.ToString(), new ManualAdjustBaseRequest(1), CancellationToken.None);

        Assert.False(result.Success);
        Assert.Equal(404, result.StatusCode);
        Assert.Contains("Base weight", result.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task PatchBaseAsync_WhenNameMissing_Returns404()
    {
        await using var db = CreateDbContext();
        db.BaseWeights.Add(new BaseWeight { Uuid = U1, Weight = 1, LastUpdated = FixedLastUpdated });
        await db.SaveChangesAsync();

        var sut = new ManualWeightAdjustmentService(db);
        var result = await sut.PatchBaseAsync("nobody", new ManualAdjustBaseRequest(1), CancellationToken.None);

        Assert.False(result.Success);
        Assert.Equal(404, result.StatusCode);
    }

    [Fact]
    public async Task PatchBaseAsync_WhenNameAmbiguous_Returns409()
    {
        await using var db = CreateDbContext();
        db.Names.Add(new PlayerName { Uuid = U1, Name = "dup", PreviousNames = [] });
        db.Names.Add(new PlayerName { Uuid = U2, Name = "dup", PreviousNames = [] });
        db.BaseWeights.Add(new BaseWeight { Uuid = U1, Weight = 1, LastUpdated = FixedLastUpdated });
        await db.SaveChangesAsync();

        var sut = new ManualWeightAdjustmentService(db);
        var result = await sut.PatchBaseAsync("dup", new ManualAdjustBaseRequest(1), CancellationToken.None);

        Assert.False(result.Success);
        Assert.Equal(409, result.StatusCode);
    }

    [Fact]
    public async Task PatchSpecAsync_AdjustsOffsetAndReturnsSpecWeights()
    {
        await using var db = CreateDbContext();
        db.BaseWeights.Add(new BaseWeight { Uuid = U1, Weight = 100, LastUpdated = FixedLastUpdated });
        db.ExperimentalSpecWeights.Add(new ExperimentalSpecWeight
        {
            Uuid = U1,
            PyromancerOffset = 10,
            LastUpdated = FixedLastUpdated
        });
        await db.SaveChangesAsync();

        var sut = new ManualWeightAdjustmentService(db);
        var result = await sut.PatchSpecAsync(
            U1.ToString(),
            new ManualAdjustSpecRequest(3, "pyromancer"),
            CancellationToken.None);

        Assert.True(result.Success);
        Assert.NotNull(result.Response);
        Assert.Equal("Pyromancer", result.Response.Spec);
        Assert.Equal(10, result.Response.PreviousOffset);
        Assert.Equal(13, result.Response.NewOffset);
        Assert.Equal(100, result.Response.BaseWeight);
        Assert.Equal(90, result.Response.PreviousSpecWeight);
        Assert.Equal(87, result.Response.NewSpecWeight);
        Assert.Equal(13, (await db.ExperimentalSpecWeights.SingleAsync(x => x.Uuid == U1)).PyromancerOffset);
        var log = await db.AdjustmentManualWeeklyLogs.SingleAsync(x => x.Uuid == U1 && x.Spec == "Pyromancer");
        Assert.Equal(result.Response.Spec, log.Spec);
        Assert.Equal(result.Response.PreviousOffset, log.PreviousOffset);
        Assert.Equal(result.Response.NewOffset, log.NewOffset);
        Assert.Equal(result.Response.BaseWeight, log.BaseWeight);
        Assert.Equal(result.Response.PreviousSpecWeight, log.PreviousSpecWeight);
        Assert.Equal(result.Response.NewSpecWeight, log.NewSpecWeight);
    }

    [Fact]
    public async Task PatchSpecAsync_WhenSpecUnknown_Returns400()
    {
        await using var db = CreateDbContext();
        db.BaseWeights.Add(new BaseWeight { Uuid = U1, Weight = 100, LastUpdated = FixedLastUpdated });
        db.ExperimentalSpecWeights.Add(new ExperimentalSpecWeight { Uuid = U1, LastUpdated = FixedLastUpdated });
        await db.SaveChangesAsync();

        var sut = new ManualWeightAdjustmentService(db);
        var result = await sut.PatchSpecAsync(
            U1.ToString(),
            new ManualAdjustSpecRequest(1, "NotASpec"),
            CancellationToken.None);

        Assert.False(result.Success);
        Assert.Equal(400, result.StatusCode);
    }

    [Fact]
    public async Task PatchSpecAsync_WhenSpecRowMissing_Returns404()
    {
        await using var db = CreateDbContext();
        db.BaseWeights.Add(new BaseWeight { Uuid = U1, Weight = 100, LastUpdated = FixedLastUpdated });
        await db.SaveChangesAsync();

        var sut = new ManualWeightAdjustmentService(db);
        var result = await sut.PatchSpecAsync(
            U1.ToString(),
            new ManualAdjustSpecRequest(1, "Pyromancer"),
            CancellationToken.None);

        Assert.False(result.Success);
        Assert.Equal(404, result.StatusCode);
    }

    [Theory]
    [InlineData("Pyromancer", "Pyromancer")]
    [InlineData("PYROMANCER", "Pyromancer")]
    [InlineData("  luminary  ", "Luminary")]
    public void TryNormalizeSpec_ReturnsCanonicalOrNull(string input, string? expected)
    {
        Assert.Equal(expected, ManualWeightAdjustmentService.TryNormalizeSpec(input));
    }

    [Fact]
    public void TryNormalizeSpec_WhenInvalid_ReturnsNull()
    {
        Assert.Null(ManualWeightAdjustmentService.TryNormalizeSpec("nope"));
        Assert.Null(ManualWeightAdjustmentService.TryNormalizeSpec(null));
        Assert.Null(ManualWeightAdjustmentService.TryNormalizeSpec("   "));
    }

    private static BalancerDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<BalancerDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new BalancerDbContext(options);
    }
}
