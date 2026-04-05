using System.Text.Json;
using BalancerAPI.Business.Services;
using BalancerAPI.Data.Data;
using BalancerAPI.Domain.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace BalancerAPI.Tests.Services;

public class ExperimentalBalanceConfirmServiceTests
{
    private sealed class TestDbContextFactory(DbContextOptions<BalancerDbContext> options) : IDbContextFactory<BalancerDbContext>
    {
        public BalancerDbContext CreateDbContext() => new(options);
    }

    private static DbContextOptions<BalancerDbContext> CreateOptions(string dbName) =>
        new DbContextOptionsBuilder<BalancerDbContext>()
            .UseInMemoryDatabase(dbName)
            .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;

    [Fact]
    public async Task ConfirmAsync_WhenValid_CreatesOneSpecLogPerTeamAndSetsPosted()
    {
        var dbName = Guid.NewGuid().ToString();
        var options = CreateOptions(dbName);
        var balanceId = Guid.NewGuid();
        var u1 = Guid.Parse("a1b2c3d4-e5f6-7890-abcd-ef1234567890");
        var u2 = Guid.Parse("b2c3d4e5-f6a7-8901-bcde-f12345678901");
        var u3 = Guid.Parse("c3d4e5f6-a7b8-9012-cdef-123456789012");
        var u4 = Guid.Parse("d4e5f6a7-b8c9-0123-def0-234567890123");

        var teams = new[]
        {
            new ExperimentalBalanceTeam(0, 0, 0, 0,
            [
                new ExperimentalBalancePlayerSpec(u1, "a", "Pyromancer", 0, 0, 0, 0),
                new ExperimentalBalancePlayerSpec(u2, "b", "Cryomancer", 0, 0, 0, 0)
            ]),
            new ExperimentalBalanceTeam(0, 0, 0, 0,
            [
                new ExperimentalBalancePlayerSpec(u3, "c", "Aquamancer", 0, 0, 0, 0),
                new ExperimentalBalancePlayerSpec(u4, "d", "Berserker", 0, 0, 0, 0)
            ])
        };

        var metaTime = new DateTime(2026, 4, 5, 12, 0, 0, DateTimeKind.Utc);
        var balanceJson = JsonSerializer.Serialize(teams);
        var metaJson = JsonSerializer.Serialize(new ExperimentalBalanceMeta(
            1,
            1,
            [],
            1,
            metaTime));

        await using (var db = new BalancerDbContext(options))
        {
            db.ExperimentalBalanceLogs.Add(new ExperimentalBalanceLog
            {
                BalanceId = balanceId,
                GameId = ExperimentalBalanceLogGameIds.Sentinel,
                Balance = balanceJson,
                Meta = metaJson,
                CreatedAt = metaTime,
                Posted = false
            });
            await db.SaveChangesAsync();
        }

        var sut = new ExperimentalBalanceConfirmService(new TestDbContextFactory(options));
        var result = await sut.ConfirmAsync(balanceId, CancellationToken.None);

        Assert.True(result.Success);
        Assert.Equal(200, result.StatusCode);

        await using (var verify = new BalancerDbContext(options))
        {
            var specRows = verify.ExperimentalSpecLogs.Where(x => x.BalanceId == balanceId).ToList();
            Assert.Equal(2, specRows.Count);
            Assert.Contains(specRows, r => r.Pyromancer == u1 && r.Cryomancer == u2);
            Assert.Contains(specRows, r => r.Aquamancer == u3 && r.Berserker == u4);

            var log = verify.ExperimentalBalanceLogs.Single(x => x.BalanceId == balanceId && x.GameId == ExperimentalBalanceLogGameIds.Sentinel);
            Assert.True(log.Posted);
        }
    }

    [Fact]
    public async Task ConfirmAsync_WhenAlreadyPosted_Returns409()
    {
        var dbName = Guid.NewGuid().ToString();
        var options = CreateOptions(dbName);
        var balanceId = Guid.NewGuid();

        var teams = new[]
        {
            new ExperimentalBalanceTeam(0, 0, 0, 0,
            [
                new ExperimentalBalancePlayerSpec(Guid.NewGuid(), "a", "Pyromancer", 0, 0, 0, 0)
            ])
        };

        var metaTime = new DateTime(2026, 4, 5, 14, 0, 0, DateTimeKind.Utc);
        await using (var db = new BalancerDbContext(options))
        {
            db.ExperimentalBalanceLogs.Add(new ExperimentalBalanceLog
            {
                BalanceId = balanceId,
                GameId = ExperimentalBalanceLogGameIds.Sentinel,
                Balance = JsonSerializer.Serialize(teams),
                Meta = JsonSerializer.Serialize(new ExperimentalBalanceMeta(1, 1, [], 1, metaTime)),
                CreatedAt = metaTime,
                Posted = true
            });
            await db.SaveChangesAsync();
        }

        var sut = new ExperimentalBalanceConfirmService(new TestDbContextFactory(options));
        var result = await sut.ConfirmAsync(balanceId, CancellationToken.None);

        Assert.False(result.Success);
        Assert.Equal(409, result.StatusCode);
    }

    [Fact]
    public async Task ConfirmAsync_WhenDuplicateSpecInTeam_Returns400()
    {
        var dbName = Guid.NewGuid().ToString();
        var options = CreateOptions(dbName);
        var balanceId = Guid.NewGuid();

        var teams = new[]
        {
            new ExperimentalBalanceTeam(0, 0, 0, 0,
            [
                new ExperimentalBalancePlayerSpec(Guid.NewGuid(), "a", "Pyromancer", 0, 0, 0, 0),
                new ExperimentalBalancePlayerSpec(Guid.NewGuid(), "b", "Pyromancer", 0, 0, 0, 0)
            ])
        };

        var metaTime = new DateTime(2026, 4, 5, 16, 0, 0, DateTimeKind.Utc);
        await using (var db = new BalancerDbContext(options))
        {
            db.ExperimentalBalanceLogs.Add(new ExperimentalBalanceLog
            {
                BalanceId = balanceId,
                GameId = ExperimentalBalanceLogGameIds.Sentinel,
                Balance = JsonSerializer.Serialize(teams),
                Meta = JsonSerializer.Serialize(new ExperimentalBalanceMeta(1, 1, [], 1, metaTime)),
                CreatedAt = metaTime,
                Posted = false
            });
            await db.SaveChangesAsync();
        }

        var sut = new ExperimentalBalanceConfirmService(new TestDbContextFactory(options));
        var result = await sut.ConfirmAsync(balanceId, CancellationToken.None);

        Assert.False(result.Success);
        Assert.Equal(400, result.StatusCode);
    }

    [Fact]
    public async Task ConfirmAsync_WhenMultipleRowsSameBalanceId_UsesLatestCreatedAt()
    {
        var dbName = Guid.NewGuid().ToString();
        var options = CreateOptions(dbName);
        var balanceId = Guid.NewGuid();
        var u1 = Guid.Parse("a1b2c3d4-e5f6-7890-abcd-ef1234567890");
        var u2 = Guid.Parse("b2c3d4e5-f6a7-8901-bcde-f12345678901");

        var olderTeams = new[]
        {
            new ExperimentalBalanceTeam(0, 0, 0, 0,
            [
                new ExperimentalBalancePlayerSpec(u1, "a", "Pyromancer", 0, 0, 0, 0)
            ])
        };
        var newerTeams = new[]
        {
            new ExperimentalBalanceTeam(0, 0, 0, 0,
            [
                new ExperimentalBalancePlayerSpec(u1, "a", "Defender", 0, 0, 0, 0),
                new ExperimentalBalancePlayerSpec(u2, "b", "Cryomancer", 0, 0, 0, 0)
            ])
        };

        var tOld = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var tNew = new DateTime(2026, 6, 1, 0, 0, 0, DateTimeKind.Utc);

        await using (var db = new BalancerDbContext(options))
        {
            db.ExperimentalBalanceLogs.Add(new ExperimentalBalanceLog
            {
                BalanceId = balanceId,
                GameId = ExperimentalBalanceLogGameIds.Sentinel,
                Balance = JsonSerializer.Serialize(olderTeams),
                Meta = JsonSerializer.Serialize(new ExperimentalBalanceMeta(1, 1, [], 1, tOld)),
                CreatedAt = tOld,
                Posted = false
            });
            db.ExperimentalBalanceLogs.Add(new ExperimentalBalanceLog
            {
                BalanceId = balanceId,
                GameId = "111111111111111111111111",
                Balance = JsonSerializer.Serialize(newerTeams),
                Meta = JsonSerializer.Serialize(new ExperimentalBalanceMeta(1, 1, [], 1, tNew)),
                CreatedAt = tNew,
                Posted = false
            });
            await db.SaveChangesAsync();
        }

        var sut = new ExperimentalBalanceConfirmService(new TestDbContextFactory(options));
        var result = await sut.ConfirmAsync(balanceId, CancellationToken.None);

        Assert.True(result.Success);

        await using (var verify = new BalancerDbContext(options))
        {
            var older = verify.ExperimentalBalanceLogs.Single(x =>
                x.BalanceId == balanceId && x.GameId == ExperimentalBalanceLogGameIds.Sentinel);
            var newer = verify.ExperimentalBalanceLogs.Single(x =>
                x.BalanceId == balanceId && x.GameId == "111111111111111111111111");
            Assert.False(older.Posted);
            Assert.True(newer.Posted);

            var specRows = verify.ExperimentalSpecLogs.Where(x => x.BalanceId == balanceId).ToList();
            Assert.Single(specRows);
            Assert.Equal(u1, specRows[0].Defender);
            Assert.Equal(u2, specRows[0].Cryomancer);
        }
    }
}
