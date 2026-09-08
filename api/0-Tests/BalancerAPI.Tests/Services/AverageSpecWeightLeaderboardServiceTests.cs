using BalancerAPI.Business.Services;
using BalancerAPI.Data.Data;
using BalancerAPI.Domain.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace BalancerAPI.Tests.Services;

public class AverageSpecWeightLeaderboardServiceTests
{
    private static readonly Guid U1 = Guid.Parse("a1b2c3d4-e5f6-7890-abcd-ef1234567890");
    private static readonly Guid U2 = Guid.Parse("b2c3d4e5-f6a7-8901-bcde-f12345678901");
    private static readonly Guid U3 = Guid.Parse("c3d4e5f6-a7b8-9012-cdef-123456789012");

    [Fact]
    public async Task GetLeaderboardAsync_OrdersByAverageWeightDesc()
    {
        var (db, factory) = CreateDbContextAndFactory();
        await using (db)
        {
            db.ExperimentalBalancePlayerData.AddRange(
                Player(U1, "Alice", flatWeight: 200),
                Player(U2, "Bob", flatWeight: 150),
                Player(U3, "Charlie", flatWeight: 180));
            SeedActiveBaseWeights(db, U1, U2, U3);
            await db.SaveChangesAsync();
        }

        var service = new AverageSpecWeightLeaderboardService(factory);
        var result = await service.GetLeaderboardAsync(1, 10, CancellationToken.None);

        Assert.Equal(3, result.Count);
        Assert.Equal("Alice", result[0].Name);
        Assert.Equal(200, result[0].AverageWeight);
        Assert.Equal("Charlie", result[1].Name);
        Assert.Equal(180, result[1].AverageWeight);
        Assert.Equal("Bob", result[2].Name);
        Assert.Equal(150, result[2].AverageWeight);
    }

    [Fact]
    public async Task GetLeaderboardAsync_AverageMatchesRoundedMeanOfAll18Specs()
    {
        var (db, factory) = CreateDbContextAndFactory();
        await using (db)
        {
            // Sum of defaults: 1+2+...+18 = 171; mean = 9.5 → rounds to 2 decimals as 9.50
            db.ExperimentalBalancePlayerData.Add(PlayerWithSequentialWeights(U1, "Alice"));
            SeedActiveBaseWeights(db, U1);
            await db.SaveChangesAsync();
        }

        var service = new AverageSpecWeightLeaderboardService(factory);
        var result = await service.GetLeaderboardAsync(1, 10, CancellationToken.None);

        Assert.Single(result);
        Assert.Equal(9.5, result[0].AverageWeight);
    }

    [Fact]
    public async Task GetLeaderboardAsync_Paginates()
    {
        var (db, factory) = CreateDbContextAndFactory();
        await using (db)
        {
            db.ExperimentalBalancePlayerData.AddRange(
                Player(U1, "Alice", flatWeight: 200),
                Player(U2, "Bob", flatWeight: 150),
                Player(U3, "Charlie", flatWeight: 180));
            SeedActiveBaseWeights(db, U1, U2, U3);
            await db.SaveChangesAsync();
        }

        var service = new AverageSpecWeightLeaderboardService(factory);
        var page1 = await service.GetLeaderboardAsync(1, 2, CancellationToken.None);
        var page2 = await service.GetLeaderboardAsync(2, 2, CancellationToken.None);

        Assert.Equal(2, page1.Count);
        Assert.Equal("Alice", page1[0].Name);
        Assert.Equal("Charlie", page1[1].Name);

        Assert.Single(page2);
        Assert.Equal("Bob", page2[0].Name);
    }

    [Fact]
    public async Task GetLeaderboardAsync_DedupesByUuid()
    {
        var (db, factory) = CreateDbContextAndFactory();
        await using (db)
        {
            db.ExperimentalBalancePlayerData.AddRange(
                Player(U1, "Zara", flatWeight: 100),
                Player(U1, "Alice", flatWeight: 100));
            SeedActiveBaseWeights(db, U1);
            await db.SaveChangesAsync();
        }

        var service = new AverageSpecWeightLeaderboardService(factory);
        var result = await service.GetLeaderboardAsync(1, 10, CancellationToken.None);

        Assert.Single(result);
        Assert.Equal("Alice", result[0].Name);
    }

    [Fact]
    public async Task GetLeaderboardAsync_TieBreaksByNameThenUuid()
    {
        var (db, factory) = CreateDbContextAndFactory();
        await using (db)
        {
            db.ExperimentalBalancePlayerData.AddRange(
                Player(U2, "Bob", flatWeight: 100),
                Player(U1, "Alice", flatWeight: 100));
            SeedActiveBaseWeights(db, U1, U2);
            await db.SaveChangesAsync();
        }

        var service = new AverageSpecWeightLeaderboardService(factory);
        var result = await service.GetLeaderboardAsync(1, 10, CancellationToken.None);

        Assert.Equal("Alice", result[0].Name);
        Assert.Equal("Bob", result[1].Name);
    }

    [Fact]
    public async Task GetLeaderboardAsync_IncludesBannedPlayers()
    {
        var (db, factory) = CreateDbContextAndFactory();
        await using (db)
        {
            db.ExperimentalBalancePlayerData.Add(Player(U1, "Alice", flatWeight: 200));
            SeedActiveBaseWeights(db, U1);
            db.ExperimentalSpecBans.Add(new ExperimentalSpecBan
            {
                Uuid = U1,
                Pyromancer = true
            });
            await db.SaveChangesAsync();
        }

        var service = new AverageSpecWeightLeaderboardService(factory);
        var result = await service.GetLeaderboardAsync(1, 10, CancellationToken.None);

        Assert.Single(result);
        Assert.Equal("Alice", result[0].Name);
        Assert.Equal(200, result[0].AverageWeight);
    }

    [Fact]
    public async Task GetLeaderboardAsync_ExcludesStaleAndNullLastPlayed()
    {
        var (db, factory) = CreateDbContextAndFactory();
        await using (db)
        {
            db.ExperimentalBalancePlayerData.AddRange(
                Player(U1, "Alice", flatWeight: 100),
                Player(U2, "Bob", flatWeight: 300),
                Player(U3, "Charlie", flatWeight: 250));
            db.BaseWeights.AddRange(
                new BaseWeight { Uuid = U1, Weight = 1000, LastUpdated = DateTime.UtcNow, LastPlayed = DateTime.UtcNow },
                new BaseWeight
                {
                    Uuid = U2,
                    Weight = 1000,
                    LastUpdated = DateTime.UtcNow,
                    LastPlayed = DateTime.UtcNow.AddMonths(-1).AddDays(-1)
                },
                new BaseWeight { Uuid = U3, Weight = 1000, LastUpdated = DateTime.UtcNow, LastPlayed = null });
            await db.SaveChangesAsync();
        }

        var service = new AverageSpecWeightLeaderboardService(factory);
        var result = await service.GetLeaderboardAsync(1, 10, CancellationToken.None);

        Assert.Single(result);
        Assert.Equal("Alice", result[0].Name);
    }

    private static void SeedActiveBaseWeights(BalancerDbContext db, params Guid[] uuids)
    {
        foreach (var uuid in uuids)
        {
            db.BaseWeights.Add(new BaseWeight
            {
                Uuid = uuid,
                Weight = 1000,
                LastUpdated = DateTime.UtcNow,
                LastPlayed = DateTime.UtcNow
            });
        }
    }

    private static ExperimentalBalancePlayerData Player(
        Guid uuid,
        string name,
        int flatWeight) =>
        new()
        {
            Uuid = uuid,
            Name = name,
            BaseWeight = 1000,
            PyromancerWeight = flatWeight,
            CryomancerWeight = flatWeight,
            AquamancerWeight = flatWeight,
            BerserkerWeight = flatWeight,
            DefenderWeight = flatWeight,
            RevenantWeight = flatWeight,
            AvengerWeight = flatWeight,
            CrusaderWeight = flatWeight,
            ProtectorWeight = flatWeight,
            ThunderlordWeight = flatWeight,
            SpiritguardWeight = flatWeight,
            EarthwardenWeight = flatWeight,
            AssassinWeight = flatWeight,
            VindicatorWeight = flatWeight,
            ApothecaryWeight = flatWeight,
            ConjurerWeight = flatWeight,
            SentinelWeight = flatWeight,
            LuminaryWeight = flatWeight
        };

    private static ExperimentalBalancePlayerData PlayerWithSequentialWeights(Guid uuid, string name) =>
        new()
        {
            Uuid = uuid,
            Name = name,
            BaseWeight = 1000,
            PyromancerWeight = 1,
            CryomancerWeight = 2,
            AquamancerWeight = 3,
            BerserkerWeight = 4,
            DefenderWeight = 5,
            RevenantWeight = 6,
            AvengerWeight = 7,
            CrusaderWeight = 8,
            ProtectorWeight = 9,
            ThunderlordWeight = 10,
            SpiritguardWeight = 11,
            EarthwardenWeight = 12,
            AssassinWeight = 13,
            VindicatorWeight = 14,
            ApothecaryWeight = 15,
            ConjurerWeight = 16,
            SentinelWeight = 17,
            LuminaryWeight = 18
        };

    private static (TestBalancerDbContextForAvgLeaderboard Db, IDbContextFactory<BalancerDbContext> Factory)
        CreateDbContextAndFactory()
    {
        var options = new DbContextOptionsBuilder<BalancerDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;
        var db = new TestBalancerDbContextForAvgLeaderboard(options);
        var factory = new TestDbContextFactory(options);
        return (db, factory);
    }

    private sealed class TestDbContextFactory(DbContextOptions<BalancerDbContext> options) : IDbContextFactory<BalancerDbContext>
    {
        public BalancerDbContext CreateDbContext() => new TestBalancerDbContextForAvgLeaderboard(options);

        public Task<BalancerDbContext> CreateDbContextAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(CreateDbContext());
    }

    private sealed class TestBalancerDbContextForAvgLeaderboard(DbContextOptions<BalancerDbContext> options)
        : BalancerDbContext(options)
    {
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Ignore<ExperimentalBalancePlayerData>();
            modelBuilder.Entity<ExperimentalBalancePlayerData>(entity =>
            {
                entity.ToTable("experimental_balance_player_data_avg_lb_test");
                entity.HasKey(x => new { x.Uuid, x.Name });
            });
        }
    }
}
