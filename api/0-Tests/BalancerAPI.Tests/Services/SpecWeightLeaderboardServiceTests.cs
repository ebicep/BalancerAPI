using BalancerAPI.Business.Services;
using BalancerAPI.Data.Data;
using BalancerAPI.Domain.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace BalancerAPI.Tests.Services;

public class SpecWeightLeaderboardServiceTests
{
    private static readonly Guid U1 = Guid.Parse("a1b2c3d4-e5f6-7890-abcd-ef1234567890");
    private static readonly Guid U2 = Guid.Parse("b2c3d4e5-f6a7-8901-bcde-f12345678901");
    private static readonly Guid U3 = Guid.Parse("c3d4e5f6-a7b8-9012-cdef-123456789012");
    private static readonly Guid U4 = Guid.Parse("d4e5f6a7-b8c9-0123-def0-234567890123");

    [Fact]
    public async Task GetLeaderboardAsync_OrdersByWeightDescPerSpec()
    {
        var (db, factory) = CreateDbContextAndFactory();
        await using (db)
        {
            db.ExperimentalBalancePlayerData.AddRange(
                Player(U1, "Alice", pyromancer: 200, cryomancer: 50),
                Player(U2, "Bob", pyromancer: 150, cryomancer: 300),
                Player(U3, "Charlie", pyromancer: 180, cryomancer: 100));
            await db.SaveChangesAsync();
        }

        var service = new SpecWeightLeaderboardService(factory);
        var result = await service.GetLeaderboardAsync(1, 10, CancellationToken.None);

        var pyro = result["pyromancer"];
        Assert.Equal(3, pyro.Count);
        Assert.Equal("Alice", pyro[0].Name);
        Assert.Equal(200, pyro[0].SpecWeight);
        Assert.Equal("Charlie", pyro[1].Name);
        Assert.Equal(180, pyro[1].SpecWeight);
        Assert.Equal("Bob", pyro[2].Name);
        Assert.Equal(150, pyro[2].SpecWeight);

        var cryo = result["cryomancer"];
        Assert.Equal("Bob", cryo[0].Name);
        Assert.Equal(300, cryo[0].SpecWeight);
    }

    [Fact]
    public async Task GetLeaderboardAsync_PaginatesPerSpec()
    {
        var (db, factory) = CreateDbContextAndFactory();
        await using (db)
        {
            db.ExperimentalBalancePlayerData.AddRange(
                Player(U1, "Alice", pyromancer: 200),
                Player(U2, "Bob", pyromancer: 150),
                Player(U3, "Charlie", pyromancer: 180));
            await db.SaveChangesAsync();
        }

        var service = new SpecWeightLeaderboardService(factory);
        var page1 = await service.GetLeaderboardAsync(1, 2, CancellationToken.None);
        var page2 = await service.GetLeaderboardAsync(2, 2, CancellationToken.None);

        Assert.Equal(2, page1["pyromancer"].Count);
        Assert.Equal("Alice", page1["pyromancer"][0].Name);
        Assert.Equal("Charlie", page1["pyromancer"][1].Name);

        Assert.Single(page2["pyromancer"]);
        Assert.Equal("Bob", page2["pyromancer"][0].Name);
    }

    [Fact]
    public async Task GetLeaderboardAsync_ExcludesBannedPlayersForThatSpec()
    {
        var (db, factory) = CreateDbContextAndFactory();
        await using (db)
        {
            db.ExperimentalBalancePlayerData.AddRange(
                Player(U1, "Alice", pyromancer: 200),
                Player(U4, "Dan", pyromancer: 250));
            db.ExperimentalSpecBans.Add(new ExperimentalSpecBan
            {
                Uuid = U4,
                Pyromancer = true
            });
            await db.SaveChangesAsync();
        }

        var service = new SpecWeightLeaderboardService(factory);
        var result = await service.GetLeaderboardAsync(1, 10, CancellationToken.None);

        Assert.Single(result["pyromancer"]);
        Assert.Equal("Alice", result["pyromancer"][0].Name);
        Assert.DoesNotContain(result["pyromancer"], e => e.Uuid == U4.ToString());
    }

    [Fact]
    public async Task GetLeaderboardAsync_DedupesByUuid()
    {
        var (db, factory) = CreateDbContextAndFactory();
        await using (db)
        {
            db.ExperimentalBalancePlayerData.AddRange(
                Player(U1, "Zara", pyromancer: 100),
                Player(U1, "Alice", pyromancer: 100));
            await db.SaveChangesAsync();
        }

        var service = new SpecWeightLeaderboardService(factory);
        var result = await service.GetLeaderboardAsync(1, 10, CancellationToken.None);

        Assert.Single(result["pyromancer"]);
        Assert.Equal("Alice", result["pyromancer"][0].Name);
    }

    [Fact]
    public async Task GetLeaderboardAsync_TieBreaksByNameThenUuid()
    {
        var (db, factory) = CreateDbContextAndFactory();
        await using (db)
        {
            db.ExperimentalBalancePlayerData.AddRange(
                Player(U2, "Bob", pyromancer: 100),
                Player(U1, "Alice", pyromancer: 100));
            await db.SaveChangesAsync();
        }

        var service = new SpecWeightLeaderboardService(factory);
        var result = await service.GetLeaderboardAsync(1, 10, CancellationToken.None);

        Assert.Equal("Alice", result["pyromancer"][0].Name);
        Assert.Equal("Bob", result["pyromancer"][1].Name);
    }

    [Fact]
    public async Task GetLeaderboardAsync_ReturnsAllSpecsWithLowercaseKeys()
    {
        var (_, factory) = CreateDbContextAndFactory();

        var service = new SpecWeightLeaderboardService(factory);
        var result = await service.GetLeaderboardAsync(1, 10, CancellationToken.None);

        Assert.Equal(18, result.Count);
        foreach (var spec in ExperimentalSpecs.AllOrdered)
        {
            Assert.True(result.ContainsKey(spec.ToLowerInvariant()));
            Assert.Empty(result[spec.ToLowerInvariant()]);
        }
    }

    private static ExperimentalBalancePlayerData Player(
        Guid uuid,
        string name,
        int pyromancer = 0,
        int cryomancer = 0) =>
        new()
        {
            Uuid = uuid,
            Name = name,
            BaseWeight = 1000,
            PyromancerWeight = pyromancer,
            CryomancerWeight = cryomancer
        };

    private static (TestBalancerDbContextForLeaderboard Db, IDbContextFactory<BalancerDbContext> Factory)
        CreateDbContextAndFactory()
    {
        var options = new DbContextOptionsBuilder<BalancerDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;
        var db = new TestBalancerDbContextForLeaderboard(options);
        var factory = new TestDbContextFactory(options);
        return (db, factory);
    }

    private sealed class TestDbContextFactory(DbContextOptions<BalancerDbContext> options) : IDbContextFactory<BalancerDbContext>
    {
        public BalancerDbContext CreateDbContext() => new TestBalancerDbContextForLeaderboard(options);

        public Task<BalancerDbContext> CreateDbContextAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(CreateDbContext());
    }

    private sealed class TestBalancerDbContextForLeaderboard(DbContextOptions<BalancerDbContext> options)
        : BalancerDbContext(options)
    {
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Ignore<ExperimentalBalancePlayerData>();
            modelBuilder.Entity<ExperimentalBalancePlayerData>(entity =>
            {
                entity.ToTable("experimental_balance_player_data_test");
                entity.HasKey(x => new { x.Uuid, x.Name });
            });
        }
    }
}
