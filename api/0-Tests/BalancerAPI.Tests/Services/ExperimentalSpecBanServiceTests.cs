using BalancerAPI.Business.Services;
using BalancerAPI.Data.Data;
using BalancerAPI.Domain.Models;
using Microsoft.EntityFrameworkCore;

namespace BalancerAPI.Tests.Services;

public class ExperimentalSpecBanServiceTests
{
    private static readonly Guid TestUuid = Guid.Parse("a1b2c3d4-e5f6-7890-abcd-ef1234567890");

    [Fact]
    public async Task GetBansAsync_WhenNoRow_ReturnsEmptyList()
    {
        var (service, db) = CreateService();
        await using (db)
        {
            var result = await service.GetBansAsync(TestUuid, CancellationToken.None);

            Assert.True(result.Success);
            Assert.NotNull(result.Data);
            Assert.Empty(result.Data.Bans);
        }
    }

    [Fact]
    public async Task GetBansAsync_WhenRowHasFlags_ReturnsSpecsInAllOrderedOrder()
    {
        var (service, db) = CreateService();
        await using (db)
        {
            db.ExperimentalSpecBans.Add(new ExperimentalSpecBan
            {
                Uuid = TestUuid,
                Cryomancer = true,
                Pyromancer = true,
                Luminary = true
            });
            await db.SaveChangesAsync();

            var result = await service.GetBansAsync(TestUuid, CancellationToken.None);

            Assert.True(result.Success);
            Assert.Equal(["Pyromancer", "Cryomancer", "Luminary"], result.Data!.Bans);
        }
    }

    [Fact]
    public async Task SetBanAsync_WhenNoRow_CreatesRowAndSetsFlag()
    {
        var (service, db) = CreateService();
        await using (db)
        {
            var result = await service.SetBanAsync(TestUuid, "Pyromancer", banned: true, CancellationToken.None);

            Assert.True(result.Success);
            Assert.Equal(["Pyromancer"], result.Data!.Bans);
            db.ChangeTracker.Clear();
            var row = await db.ExperimentalSpecBans.SingleAsync(x => x.Uuid == TestUuid);
            Assert.True(row.Pyromancer);
        }
    }

    [Fact]
    public async Task SetBanAsync_WhenUnbanAndNoRow_Returns400()
    {
        var (service, db) = CreateService();
        await using (db)
        {
            var result = await service.SetBanAsync(TestUuid, "Pyromancer", banned: false, CancellationToken.None);

            Assert.False(result.Success);
            Assert.Equal(400, result.StatusCode);
            Assert.Contains("not banned", result.Message, StringComparison.OrdinalIgnoreCase);
            Assert.Empty(await db.ExperimentalSpecBans.ToListAsync());
        }
    }

    [Fact]
    public async Task SetBanAsync_WhenUnbanSpecNotBanned_Returns400()
    {
        var (service, db) = CreateService();
        await using (db)
        {
            db.ExperimentalSpecBans.Add(new ExperimentalSpecBan
            {
                Uuid = TestUuid,
                Cryomancer = true
            });
            await db.SaveChangesAsync();

            var result = await service.SetBanAsync(TestUuid, "Pyromancer", banned: false, CancellationToken.None);

            Assert.False(result.Success);
            Assert.Equal(400, result.StatusCode);
            Assert.Contains("not banned", result.Message, StringComparison.OrdinalIgnoreCase);
        }
    }

    [Fact]
    public async Task SetBanAsync_WhenBanAlreadyBanned_Returns400()
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

            var result = await service.SetBanAsync(TestUuid, "Pyromancer", banned: true, CancellationToken.None);

            Assert.False(result.Success);
            Assert.Equal(400, result.StatusCode);
            Assert.Contains("already banned", result.Message, StringComparison.OrdinalIgnoreCase);
        }
    }

    [Fact]
    public async Task SetBanAsync_WhenUnbanExisting_ClearsFlagAndReturnsRemainingBans()
    {
        var (service, db) = CreateService();
        await using (db)
        {
            db.ExperimentalSpecBans.Add(new ExperimentalSpecBan
            {
                Uuid = TestUuid,
                Pyromancer = true,
                Cryomancer = true
            });
            await db.SaveChangesAsync();

            var result = await service.SetBanAsync(TestUuid, "Pyromancer", banned: false, CancellationToken.None);

            Assert.True(result.Success);
            Assert.Equal(["Cryomancer"], result.Data!.Bans);
            db.ChangeTracker.Clear();
            var row = await db.ExperimentalSpecBans.SingleAsync(x => x.Uuid == TestUuid);
            Assert.False(row.Pyromancer);
            Assert.True(row.Cryomancer);
        }
    }

    [Fact]
    public async Task SetBanAsync_WhenBanMatchingSpecRequest_RemovesRequestRow()
    {
        var (service, db) = CreateService();
        await using (db)
        {
            db.ExperimentalSpecRequests.Add(new ExperimentalSpecRequest
            {
                Uuid = TestUuid,
                Spec = "Pyromancer",
                GameCooldown = 0,
                CreatedTime = DateTime.UtcNow
            });
            await db.SaveChangesAsync();

            var result = await service.SetBanAsync(TestUuid, "Pyromancer", banned: true, CancellationToken.None);

            Assert.True(result.Success);
            Assert.Empty(await db.ExperimentalSpecRequests.ToListAsync());
        }
    }

    [Fact]
    public async Task SetBanAsync_WhenBanDifferentSpec_KeepsRequestRow()
    {
        var (service, db) = CreateService();
        await using (db)
        {
            db.ExperimentalSpecRequests.Add(new ExperimentalSpecRequest
            {
                Uuid = TestUuid,
                Spec = "Cryomancer",
                GameCooldown = 0,
                CreatedTime = DateTime.UtcNow
            });
            await db.SaveChangesAsync();

            await service.SetBanAsync(TestUuid, "Pyromancer", banned: true, CancellationToken.None);

            var row = await db.ExperimentalSpecRequests.SingleAsync(x => x.Uuid == TestUuid);
            Assert.Equal("Cryomancer", row.Spec);
        }
    }

    [Fact]
    public async Task SetBanAsync_WhenBanAddsToExisting_ReturnsAllBannedSpecs()
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

            var result = await service.SetBanAsync(TestUuid, "Berserker", banned: true, CancellationToken.None);

            Assert.True(result.Success);
            Assert.Equal(["Pyromancer", "Berserker"], result.Data!.Bans);
        }
    }

    private static (ExperimentalSpecBanService Service, BalancerDbContext Db) CreateService()
    {
        var options = new DbContextOptionsBuilder<BalancerDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        var factory = new TestDbContextFactory(options);
        return (new ExperimentalSpecBanService(factory), factory.CreateDbContext());
    }

    private sealed class TestDbContextFactory(DbContextOptions<BalancerDbContext> options) : IDbContextFactory<BalancerDbContext>
    {
        public BalancerDbContext CreateDbContext() => new(options);
    }
}
