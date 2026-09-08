using BalancerAPI.Business.Services;
using BalancerAPI.Data.Data;
using BalancerAPI.Domain.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace BalancerAPI.Tests.Services;

public class BaseWeightLeaderboardServiceTests
{
    private static readonly Guid U1 = Guid.Parse("a1b2c3d4-e5f6-7890-abcd-ef1234567890");
    private static readonly Guid U2 = Guid.Parse("b2c3d4e5-f6a7-8901-bcde-f12345678901");
    private static readonly Guid U3 = Guid.Parse("c3d4e5f6-a7b8-9012-cdef-123456789012");

    [Fact]
    public async Task GetLeaderboardAsync_OrdersByWeightDesc()
    {
        var (db, factory) = CreateDbContextAndFactory();
        await using (db)
        {
            SeedPlayer(db, U1, "Alice", weight: 200, lastPlayed: DateTime.UtcNow);
            SeedPlayer(db, U2, "Bob", weight: 150, lastPlayed: DateTime.UtcNow);
            SeedPlayer(db, U3, "Charlie", weight: 180, lastPlayed: DateTime.UtcNow);
            await db.SaveChangesAsync();
        }

        var service = new BaseWeightLeaderboardService(factory);
        var result = await service.GetLeaderboardAsync(1, 10, CancellationToken.None);

        Assert.Equal(3, result.Count);
        Assert.Equal("Alice", result[0].Name);
        Assert.Equal(200, result[0].BaseWeight);
        Assert.Equal("Charlie", result[1].Name);
        Assert.Equal("Bob", result[2].Name);
    }

    [Fact]
    public async Task GetLeaderboardAsync_Paginates()
    {
        var (db, factory) = CreateDbContextAndFactory();
        await using (db)
        {
            SeedPlayer(db, U1, "Alice", weight: 200, lastPlayed: DateTime.UtcNow);
            SeedPlayer(db, U2, "Bob", weight: 150, lastPlayed: DateTime.UtcNow);
            SeedPlayer(db, U3, "Charlie", weight: 180, lastPlayed: DateTime.UtcNow);
            await db.SaveChangesAsync();
        }

        var service = new BaseWeightLeaderboardService(factory);
        var page1 = await service.GetLeaderboardAsync(1, 2, CancellationToken.None);
        var page2 = await service.GetLeaderboardAsync(2, 2, CancellationToken.None);

        Assert.Equal(2, page1.Count);
        Assert.Equal("Alice", page1[0].Name);
        Assert.Equal("Charlie", page1[1].Name);
        Assert.Single(page2);
        Assert.Equal("Bob", page2[0].Name);
    }

    [Fact]
    public async Task GetLeaderboardAsync_ExcludesStaleAndNullLastPlayed()
    {
        var (db, factory) = CreateDbContextAndFactory();
        await using (db)
        {
            SeedPlayer(db, U1, "Alice", weight: 100, lastPlayed: DateTime.UtcNow);
            SeedPlayer(db, U2, "Bob", weight: 300, lastPlayed: DateTime.UtcNow.AddMonths(-1).AddDays(-1));
            SeedPlayer(db, U3, "Charlie", weight: 250, lastPlayed: null);
            await db.SaveChangesAsync();
        }

        var service = new BaseWeightLeaderboardService(factory);
        var result = await service.GetLeaderboardAsync(1, 10, CancellationToken.None);

        Assert.Single(result);
        Assert.Equal("Alice", result[0].Name);
    }

    private static void SeedPlayer(
        BalancerDbContext db,
        Guid uuid,
        string name,
        int weight,
        DateTime? lastPlayed)
    {
        db.Names.Add(new PlayerName { Uuid = uuid, Name = name, PreviousNames = [] });
        db.BaseWeights.Add(new BaseWeight
        {
            Uuid = uuid,
            Weight = weight,
            LastUpdated = DateTime.UtcNow,
            LastPlayed = lastPlayed
        });
    }

    private static (BalancerDbContext Db, IDbContextFactory<BalancerDbContext> Factory)
        CreateDbContextAndFactory()
    {
        var options = new DbContextOptionsBuilder<BalancerDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;
        var db = new BalancerDbContext(options);
        var factory = new TestDbContextFactory(options);
        return (db, factory);
    }

    private sealed class TestDbContextFactory(DbContextOptions<BalancerDbContext> options)
        : IDbContextFactory<BalancerDbContext>
    {
        public BalancerDbContext CreateDbContext() => new(options);

        public Task<BalancerDbContext> CreateDbContextAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(CreateDbContext());
    }
}
