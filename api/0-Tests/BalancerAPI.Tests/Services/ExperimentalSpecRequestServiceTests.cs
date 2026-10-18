using System.Text.Json;
using BalancerAPI.Business.Services;
using BalancerAPI.Data.Data;
using BalancerAPI.Domain.Models;
using Microsoft.EntityFrameworkCore;

namespace BalancerAPI.Tests.Services;

public class ExperimentalSpecRequestServiceTests
{
    private static readonly Guid TestUuid = Guid.Parse("a1b2c3d4-e5f6-7890-abcd-ef1234567890");

    [Fact]
    public async Task UpsertAsync_WhenNoRow_InsertsWithCooldown5()
    {
        var (service, db) = CreateService();
        await using (db)
        {
            var result = await service.UpsertAsync(TestUuid, "Pyromancer", CancellationToken.None);

            Assert.True(result.Success);
            Assert.Equal("Pyromancer", result.Data!.Spec);
            Assert.Equal(5, result.Data.GameCooldown);
            db.ChangeTracker.Clear();
            var row = await db.ExperimentalSpecRequests.SingleAsync(x => x.Uuid == TestUuid);
            Assert.Equal("Pyromancer", row.Spec);
            Assert.Equal(5, row.GameCooldown);
        }
    }

    [Fact]
    public async Task UpsertAsync_WhenRowExists_UpdatesSpecOnly()
    {
        var (service, db) = CreateService();
        await using (db)
        {
            var created = new DateTime(2020, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            db.ExperimentalSpecRequests.Add(new ExperimentalSpecRequest
            {
                Uuid = TestUuid,
                Spec = "Pyromancer",
                GameCooldown = 2,
                CreatedTime = created
            });
            await db.SaveChangesAsync();

            var result = await service.UpsertAsync(TestUuid, "Cryomancer", CancellationToken.None);

            Assert.True(result.Success);
            Assert.Equal("Cryomancer", result.Data!.Spec);
            Assert.Equal(2, result.Data.GameCooldown);
            db.ChangeTracker.Clear();
            var row = await db.ExperimentalSpecRequests.SingleAsync(x => x.Uuid == TestUuid);
            Assert.Equal("Cryomancer", row.Spec);
            Assert.Equal(2, row.GameCooldown);
            Assert.Equal(created, row.CreatedTime);
        }
    }

    private static readonly Guid OtherUuid = Guid.Parse("b2c3d4e5-f6a7-8901-bcde-f12345678901");

    private static readonly DateTime TestGameCreatedAt = new(2026, 5, 24, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public async Task UnrunFromBalanceAsync_RestoresOnlyRequestedPlayers()
    {
        var balance = BuildBalance(
            (TestUuid, "Pyromancer", true),
            (OtherUuid, "Cryomancer", false));

        var (service, db) = CreateService();
        await using (db)
        {
            var balanceId = await SeedBalanceLogAsync(db, balance);
            var result = await service.UnrunFromBalanceIdAsync(balanceId, CancellationToken.None);

            Assert.True(result.Success);
            Assert.Single(result.Data!.Restored);
            Assert.Equal(TestUuid, result.Data.Restored[0].Uuid);
            Assert.Equal("player", result.Data.Restored[0].Name);
            Assert.Equal("Pyromancer", result.Data.Restored[0].Spec);
            db.ChangeTracker.Clear();
            Assert.Single(await db.ExperimentalSpecRequests.ToListAsync());
        }
    }

    [Fact]
    public async Task UnrunFromBalanceAsync_SetsCooldownAndCreatedTime()
    {
        var balance = BuildBalance(TestGameCreatedAt, (TestUuid, "pyromancer", true));
        var expectedCreated = TestGameCreatedAt.AddYears(-20);
        var (service, db) = CreateService();
        await using (db)
        {
            var balanceId = await SeedBalanceLogAsync(db, balance);
            var result = await service.UnrunFromBalanceIdAsync(balanceId, CancellationToken.None);

            Assert.True(result.Success);
            Assert.Equal(-10, result.Data!.Restored[0].GameCooldown);
            Assert.Equal(expectedCreated, result.Data.Restored[0].CreatedTime);
            db.ChangeTracker.Clear();
            var row = await db.ExperimentalSpecRequests.SingleAsync(x => x.Uuid == TestUuid);
            Assert.Equal(-10, row.GameCooldown);
            Assert.Equal(expectedCreated, row.CreatedTime);
            Assert.Equal("Pyromancer", row.Spec);
        }
    }

    [Fact]
    public async Task UnrunFromBalanceAsync_WhenRowExists_OverwritesSpecCooldownAndCreatedTime()
    {
        var balance = BuildBalance(TestGameCreatedAt, (TestUuid, "Cryomancer", true));
        var expectedCreated = TestGameCreatedAt.AddYears(-20);
        var (service, db) = CreateService();
        await using (db)
        {
            db.ExperimentalSpecRequests.Add(new ExperimentalSpecRequest
            {
                Uuid = TestUuid,
                Spec = "Pyromancer",
                GameCooldown = 2,
                CreatedTime = new DateTime(2020, 5, 1, 0, 0, 0, DateTimeKind.Utc)
            });
            await db.SaveChangesAsync();

            var balanceId = await SeedBalanceLogAsync(db, balance);
            var result = await service.UnrunFromBalanceIdAsync(balanceId, CancellationToken.None);

            Assert.True(result.Success);
            db.ChangeTracker.Clear();
            var row = await db.ExperimentalSpecRequests.SingleAsync(x => x.Uuid == TestUuid);
            Assert.Equal("Cryomancer", row.Spec);
            Assert.Equal(-10, row.GameCooldown);
            Assert.Equal(expectedCreated, row.CreatedTime);
        }
    }

    [Fact]
    public async Task UnrunFromBalanceIdAsync_WhenLogMissing_Returns404()
    {
        var (service, db) = CreateService();
        await using (db)
        {
            var result = await service.UnrunFromBalanceIdAsync(Guid.NewGuid(), CancellationToken.None);
            Assert.False(result.Success);
            Assert.Equal(404, result.StatusCode);
        }
    }

    [Fact]
    public async Task UnrunFromBalanceIdAsync_WhenStoredBalanceEmpty_Returns400()
    {
        var balanceId = Guid.NewGuid();
        var (service, db) = CreateService();
        await using (db)
        {
            db.ExperimentalBalanceLogs.Add(new ExperimentalBalanceLog
            {
                BalanceId = balanceId,
                Balance = "[]",
                Meta = "{}",
                CreatedAt = DateTime.UtcNow
            });
            await db.SaveChangesAsync();

            var result = await service.UnrunFromBalanceIdAsync(balanceId, CancellationToken.None);
            Assert.False(result.Success);
            Assert.Equal(400, result.StatusCode);
        }
    }

    [Fact]
    public async Task UnrunFromBalanceAsync_WhenInvalidSpec_Returns400()
    {
        var balance = BuildBalance((TestUuid, "not-a-spec", true));
        var (service, db) = CreateService();
        await using (db)
        {
            var balanceId = await SeedBalanceLogAsync(db, balance);
            var result = await service.UnrunFromBalanceIdAsync(balanceId, CancellationToken.None);

            Assert.False(result.Success);
            Assert.Equal(400, result.StatusCode);
            Assert.Empty(await db.ExperimentalSpecRequests.ToListAsync());
        }
    }

    [Fact]
    public async Task UpsertAsync_WhenBannedOnSpec_Returns400()
    {
        var (service, db) = CreateService();
        await using (db)
        {
            db.ExperimentalSpecBans.Add(new ExperimentalSpecBan
            {
                Uuid = TestUuid,
                Pyromancer = true
            });
            await db.SaveChangesAsync();

            var result = await service.UpsertAsync(TestUuid, "Pyromancer", CancellationToken.None);

            Assert.False(result.Success);
            Assert.Equal(400, result.StatusCode);
            Assert.Contains("banned", result.Message, StringComparison.OrdinalIgnoreCase);
            Assert.Empty(await db.ExperimentalSpecRequests.ToListAsync());
        }
    }

    private static ExperimentalBalanceResponse BuildBalance(
        params (Guid uuid, string spec, bool requested)[] players) =>
        BuildBalance(null, players);

    private static ExperimentalBalanceResponse BuildBalance(
        DateTime? gameCreatedAt,
        params (Guid uuid, string spec, bool requested)[] players)
    {
        var createdAt = gameCreatedAt ?? DateTime.UtcNow;
        var specs = players.Select(p => new ExperimentalBalancePlayerSpec(
            p.uuid,
            "player",
            p.spec,
            0,
            0,
            0,
            0,
            false,
            p.requested)).ToList();
        var team = new ExperimentalBalanceTeam(0, 0, 0, 0, specs);
        return new ExperimentalBalanceResponse(
            Guid.NewGuid(),
            [team],
            0,
            new ExperimentalBalanceMeta(0, 0, [], 0, createdAt));
    }

    private static async Task<Guid> SeedBalanceLogAsync(
        BalancerDbContext db,
        ExperimentalBalanceResponse balance)
    {
        db.ExperimentalBalanceLogs.Add(new ExperimentalBalanceLog
        {
            BalanceId = balance.BalanceId,
            Balance = JsonSerializer.Serialize(balance.Balance),
            Meta = JsonSerializer.Serialize(balance.Meta),
            CreatedAt = balance.Meta.Time
        });
        await db.SaveChangesAsync();
        return balance.BalanceId;
    }

    private static (ExperimentalSpecRequestService Service, BalancerDbContext Db) CreateService()
    {
        var options = new DbContextOptionsBuilder<BalancerDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        var factory = new TestDbContextFactory(options);
        return (new ExperimentalSpecRequestService(factory), factory.CreateDbContext());
    }

    private sealed class TestDbContextFactory(DbContextOptions<BalancerDbContext> options) : IDbContextFactory<BalancerDbContext>
    {
        public BalancerDbContext CreateDbContext() => new(options);
    }
}
