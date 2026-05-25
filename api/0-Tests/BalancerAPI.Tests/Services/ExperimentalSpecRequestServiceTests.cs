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
