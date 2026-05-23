using BalancerAPI.Business.Services;
using BalancerAPI.Data.Data;
using BalancerAPI.Domain.Models;
using Microsoft.EntityFrameworkCore;

namespace BalancerAPI.Tests.Services;

public class TrajectoryServiceTests
{
    private static readonly Guid U1 = Guid.Parse("a1b2c3d4-e5f6-7890-abcd-ef1234567890");
    private static readonly Guid U2 = Guid.Parse("b2c3d4e5-f6a7-8901-bcde-f12345678901");
    private static readonly Guid U3 = Guid.Parse("c3d4e5f6-a7b8-9012-cdef-123456789012");
    private static readonly DateTime FixedLastUpdated = new(2025, 3, 1, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public async Task ListAsync_OrdersByTrajectoryDescThenNameThenUuid()
    {
        await using var db = CreateDbContext();
        db.Names.AddRange(
            new PlayerName { Uuid = U1, Name = "Zed", PreviousNames = [] },
            new PlayerName { Uuid = U2, Name = "Amy", PreviousNames = [] },
            new PlayerName { Uuid = U3, Name = "Bob", PreviousNames = [] });
        db.AdjustmentDaily.AddRange(
            new AdjustmentDaily { Uuid = U1, Trajectory = 1 },
            new AdjustmentDaily { Uuid = U2, Trajectory = 3 },
            new AdjustmentDaily { Uuid = U3, Trajectory = 3 });
        await db.SaveChangesAsync();

        var sut = new TrajectoryService(db);
        var list = await sut.ListAsync(CancellationToken.None);

        Assert.Equal(3, list.Count);
        Assert.Equal(U2, list[0].Uuid);
        Assert.Equal("Amy", list[0].Name);
        Assert.Equal(3, list[0].Trajectory);
        Assert.Equal(U3, list[1].Uuid);
        Assert.Equal("Bob", list[1].Name);
        Assert.Equal(U1, list[2].Uuid);
        Assert.Equal("Zed", list[2].Name);
    }

    [Fact]
    public async Task ListAsync_IncludesJoinedName()
    {
        await using var db = CreateDbContext();
        db.Names.Add(new PlayerName { Uuid = U1, Name = "Alpha", PreviousNames = [] });
        db.AdjustmentDaily.Add(new AdjustmentDaily { Uuid = U1, Trajectory = 2 });
        await db.SaveChangesAsync();

        var sut = new TrajectoryService(db);
        var list = await sut.ListAsync(CancellationToken.None);

        Assert.Single(list);
        Assert.Equal("Alpha", list[0].Name);
    }

    [Fact]
    public async Task ListAsync_ExcludesPlayersWithoutAdjustmentDaily()
    {
        await using var db = CreateDbContext();
        db.BaseWeights.Add(new BaseWeight { Uuid = U1, Weight = 100, LastUpdated = FixedLastUpdated });
        db.Names.Add(new PlayerName { Uuid = U1, Name = "OnlyBase", PreviousNames = [] });
        db.AdjustmentDaily.Add(new AdjustmentDaily { Uuid = U2, Trajectory = 1 });
        await db.SaveChangesAsync();

        var sut = new TrajectoryService(db);
        var list = await sut.ListAsync(CancellationToken.None);

        Assert.Single(list);
        Assert.Equal(U2, list[0].Uuid);
    }

    [Fact]
    public async Task ListAsync_WhenNoName_ReturnsEmptyName()
    {
        await using var db = CreateDbContext();
        db.AdjustmentDaily.Add(new AdjustmentDaily { Uuid = U1, Trajectory = -2 });
        await db.SaveChangesAsync();

        var sut = new TrajectoryService(db);
        var list = await sut.ListAsync(CancellationToken.None);

        Assert.Single(list);
        Assert.Equal(string.Empty, list[0].Name);
    }

    [Fact]
    public async Task SetAsync_CreatesAdjustmentDailyRow()
    {
        await using var db = CreateDbContext();
        db.BaseWeights.Add(new BaseWeight { Uuid = U1, Weight = 100, LastUpdated = FixedLastUpdated });
        await db.SaveChangesAsync();

        var sut = new TrajectoryService(db);
        var result = await sut.SetAsync(U1.ToString(), new SetTrajectoryRequest(4), CancellationToken.None);

        Assert.True(result.Success);
        Assert.NotNull(result.Response);
        Assert.Equal(U1, result.Response.Uuid);
        Assert.Equal(4, result.Response.Trajectory);
        Assert.Equal(4, (await db.AdjustmentDaily.SingleAsync(x => x.Uuid == U1)).Trajectory);
    }

    [Fact]
    public async Task SetAsync_UpdatesExistingAdjustmentDailyRow()
    {
        await using var db = CreateDbContext();
        db.BaseWeights.Add(new BaseWeight { Uuid = U1, Weight = 100, LastUpdated = FixedLastUpdated });
        db.AdjustmentDaily.Add(new AdjustmentDaily { Uuid = U1, Trajectory = 2 });
        await db.SaveChangesAsync();

        var sut = new TrajectoryService(db);
        var result = await sut.SetAsync(U1.ToString(), new SetTrajectoryRequest(-1), CancellationToken.None);

        Assert.True(result.Success);
        Assert.Equal(-1, result.Response!.Trajectory);
        Assert.Equal(-1, (await db.AdjustmentDaily.SingleAsync(x => x.Uuid == U1)).Trajectory);
    }

    [Fact]
    public async Task SetAsync_ByNameCaseInsensitive_ReturnsDisplayName()
    {
        await using var db = CreateDbContext();
        db.Names.Add(new PlayerName { Uuid = U1, Name = "TestPlayer", PreviousNames = [] });
        db.BaseWeights.Add(new BaseWeight { Uuid = U1, Weight = 50, LastUpdated = FixedLastUpdated });
        await db.SaveChangesAsync();

        var sut = new TrajectoryService(db);
        var result = await sut.SetAsync("testplayer", new SetTrajectoryRequest(1), CancellationToken.None);

        Assert.True(result.Success);
        Assert.Equal("TestPlayer", result.Response!.Name);
        Assert.Equal(1, (await db.AdjustmentDaily.SingleAsync(x => x.Uuid == U1)).Trajectory);
    }

    [Fact]
    public async Task SetAsync_WhenBaseRowMissing_Returns404()
    {
        await using var db = CreateDbContext();
        var sut = new TrajectoryService(db);
        var result = await sut.SetAsync(U1.ToString(), new SetTrajectoryRequest(1), CancellationToken.None);

        Assert.False(result.Success);
        Assert.Equal(404, result.StatusCode);
        Assert.Contains("Base weight", result.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task SetAsync_WhenNameMissing_Returns404()
    {
        await using var db = CreateDbContext();
        db.BaseWeights.Add(new BaseWeight { Uuid = U1, Weight = 1, LastUpdated = FixedLastUpdated });
        await db.SaveChangesAsync();

        var sut = new TrajectoryService(db);
        var result = await sut.SetAsync("nobody", new SetTrajectoryRequest(1), CancellationToken.None);

        Assert.False(result.Success);
        Assert.Equal(404, result.StatusCode);
    }

    [Fact]
    public async Task SetAsync_WhenNameAmbiguous_Returns409()
    {
        await using var db = CreateDbContext();
        db.Names.Add(new PlayerName { Uuid = U1, Name = "dup", PreviousNames = [] });
        db.Names.Add(new PlayerName { Uuid = U2, Name = "dup", PreviousNames = [] });
        db.BaseWeights.Add(new BaseWeight { Uuid = U1, Weight = 1, LastUpdated = FixedLastUpdated });
        await db.SaveChangesAsync();

        var sut = new TrajectoryService(db);
        var result = await sut.SetAsync("dup", new SetTrajectoryRequest(1), CancellationToken.None);

        Assert.False(result.Success);
        Assert.Equal(409, result.StatusCode);
    }

    [Fact]
    public async Task SetAsync_WhenPlayerKeyEmpty_Returns400()
    {
        await using var db = CreateDbContext();
        var sut = new TrajectoryService(db);
        var result = await sut.SetAsync("   ", new SetTrajectoryRequest(1), CancellationToken.None);

        Assert.False(result.Success);
        Assert.Equal(400, result.StatusCode);
    }

    private static BalancerDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<BalancerDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new BalancerDbContext(options);
    }
}
