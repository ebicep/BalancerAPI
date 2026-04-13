using System.Text.Json;
using BalancerAPI.Business.Services;
using BalancerAPI.Data.Data;
using BalancerAPI.Domain.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace BalancerAPI.Tests.Services;

public class ExperimentalBalanceInputServiceTests
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

    private static readonly Guid U1 = Guid.Parse("a1b2c3d4-e5f6-7890-abcd-ef1234567890");
    private static readonly Guid U2 = Guid.Parse("b2c3d4e5-f6a7-8901-bcde-f12345678901");
    private static readonly Guid U3 = Guid.Parse("c3d4e5f6-a7b8-9012-cdef-123456789012");
    private static readonly Guid U4 = Guid.Parse("d4e5f6a7-b8c9-0123-def0-234567890123");

    private const string PayloadMongoGameId = "aaaaaaaaaaaaaaaaaaaaaaaa";

    private static readonly JsonSerializerOptions InputJsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private static string SerializeInputBody(ExperimentalBalanceInputBody body) =>
        JsonSerializer.Serialize(body, InputJsonOptions);

    private static ExperimentalBalanceInputBody BuildValidZeroStatsBody() =>
        new(
            Winners: [new ExperimentalBalanceInputPlayerLine(U1, 0, 0), new ExperimentalBalanceInputPlayerLine(U2, 0, 0)],
            Losers: [new ExperimentalBalanceInputPlayerLine(U3, 0, 0), new ExperimentalBalanceInputPlayerLine(U4, 0, 0)],
            GameId: PayloadMongoGameId);

    private static IReadOnlyList<ExperimentalBalanceTeam> BuildTwoTeamBalance() =>
    [
        new ExperimentalBalanceTeam(0, 0, 0, 0,
        [
            new ExperimentalBalancePlayerSpec(U1, "a", "Pyromancer", 0, 0, 0, 0),
            new ExperimentalBalancePlayerSpec(U2, "b", "Cryomancer", 0, 0, 0, 0)
        ]),
        new ExperimentalBalanceTeam(0, 0, 0, 0,
        [
            new ExperimentalBalancePlayerSpec(U3, "c", "Aquamancer", 0, 0, 0, 0),
            new ExperimentalBalancePlayerSpec(U4, "d", "Berserker", 0, 0, 0, 0)
        ])
    ];

    private static ExperimentalBalanceLog BuildLog(
        Guid balanceId,
        DateTime metaTime,
        bool posted = true,
        string? inputJson = null,
        bool counted = false,
        string? gameId = null) =>
        new()
        {
            BalanceId = balanceId,
            Balance = JsonSerializer.Serialize(BuildTwoTeamBalance()),
            Meta = JsonSerializer.Serialize(new ExperimentalBalanceMeta(1, 1, [], 1, metaTime)),
            CreatedAt = metaTime,
            Posted = posted,
            Input = inputJson,
            Counted = counted,
            GameId = gameId
        };

    [Fact]
    public async Task InputAsync_WhenValid_UpdatesWlAndSetsCountedAndAudit()
    {
        var dbName = Guid.NewGuid().ToString();
        var options = CreateOptions(dbName);
        var balanceId = Guid.NewGuid();
        var metaTime = new DateTime(2026, 4, 5, 12, 0, 0, DateTimeKind.Utc);

        await using (var db = new BalancerDbContext(options))
        {
            db.ExperimentalBalanceLogs.Add(BuildLog(balanceId, metaTime));
            db.ExperimentalSpecLogs.AddRange(
                new ExperimentalSpecLog { BalanceId = balanceId, Pyromancer = U1, Cryomancer = U2 },
                new ExperimentalSpecLog { BalanceId = balanceId, Aquamancer = U3, Berserker = U4 });
            foreach (var u in new[] { U1, U2, U3, U4 })
            {
                db.ExperimentalSpecsWl.Add(new ExperimentalSpecsWl { Uuid = u });
            }

            await db.SaveChangesAsync();
        }

        var sut = new ExperimentalBalanceInputService(new TestDbContextFactory(options));
        var body = new ExperimentalBalanceInputBody(
            Winners:
            [
                new ExperimentalBalanceInputPlayerLine(U1, 5, 2),
                new ExperimentalBalanceInputPlayerLine(U2, 3, 1)
            ],
            Losers:
            [
                new ExperimentalBalanceInputPlayerLine(U3, 2, 5),
                new ExperimentalBalanceInputPlayerLine(U4, 1, 3)
            ],
            GameId: PayloadMongoGameId);

        var result = await sut.InputAsync(balanceId, body, CancellationToken.None);

        Assert.True(result.Success);
        Assert.Equal(200, result.StatusCode);

        await using (var verify = new BalancerDbContext(options))
        {
            var log = verify.ExperimentalBalanceLogs.Single(x => x.BalanceId == balanceId);
            Assert.True(log.Counted);
            Assert.False(string.IsNullOrEmpty(log.Input));
            Assert.Equal(PayloadMongoGameId, log.GameId);

            var audits = verify.ExperimentalInputLogs.Where(x => x.BalanceId == balanceId).ToList();
            Assert.Single(audits);
            Assert.Equal("input", audits[0].Action);
            Assert.Equal(PayloadMongoGameId, audits[0].GameId);

            var w1 = verify.ExperimentalSpecsWl.Single(x => x.Uuid == U1);
            Assert.Equal(1, w1.PyromancerWins);
            Assert.Equal(5, w1.PyromancerKills);
            Assert.Equal(2, w1.PyromancerDeaths);

            var w2 = verify.ExperimentalSpecsWl.Single(x => x.Uuid == U2);
            Assert.Equal(1, w2.CryomancerWins);
            Assert.Equal(3, w2.CryomancerKills);
            Assert.Equal(1, w2.CryomancerDeaths);

            var w3 = verify.ExperimentalSpecsWl.Single(x => x.Uuid == U3);
            Assert.Equal(1, w3.AquamancerLosses);
            Assert.Equal(2, w3.AquamancerKills);
            Assert.Equal(5, w3.AquamancerDeaths);

            var w4 = verify.ExperimentalSpecsWl.Single(x => x.Uuid == U4);
            Assert.Equal(1, w4.BerserkerLosses);
            Assert.Equal(1, w4.BerserkerKills);
            Assert.Equal(3, w4.BerserkerDeaths);
        }
    }

    [Fact]
    public async Task InputAsync_WhenWinnersSplitAcrossTeams_Returns400()
    {
        var dbName = Guid.NewGuid().ToString();
        var options = CreateOptions(dbName);
        var balanceId = Guid.NewGuid();
        var metaTime = new DateTime(2026, 4, 5, 12, 0, 0, DateTimeKind.Utc);

        await using (var db = new BalancerDbContext(options))
        {
            db.ExperimentalBalanceLogs.Add(BuildLog(balanceId, metaTime));
            db.ExperimentalSpecLogs.AddRange(
                new ExperimentalSpecLog { BalanceId = balanceId, Pyromancer = U1, Cryomancer = U2 },
                new ExperimentalSpecLog { BalanceId = balanceId, Aquamancer = U3, Berserker = U4 });
            foreach (var u in new[] { U1, U2, U3, U4 })
            {
                db.ExperimentalSpecsWl.Add(new ExperimentalSpecsWl { Uuid = u });
            }

            await db.SaveChangesAsync();
        }

        var sut = new ExperimentalBalanceInputService(new TestDbContextFactory(options));
        var body = new ExperimentalBalanceInputBody(
            Winners: [new ExperimentalBalanceInputPlayerLine(U1, 0, 0), new ExperimentalBalanceInputPlayerLine(U3, 0, 0)],
            Losers: [new ExperimentalBalanceInputPlayerLine(U2, 0, 0), new ExperimentalBalanceInputPlayerLine(U4, 0, 0)],
            GameId: PayloadMongoGameId);

        var result = await sut.InputAsync(balanceId, body, CancellationToken.None);

        Assert.False(result.Success);
        Assert.Equal(400, result.StatusCode);
    }

    [Fact]
    public async Task InputAsync_WhenNotPosted_Returns409()
    {
        var dbName = Guid.NewGuid().ToString();
        var options = CreateOptions(dbName);
        var balanceId = Guid.NewGuid();
        var metaTime = new DateTime(2026, 4, 5, 12, 0, 0, DateTimeKind.Utc);

        await using (var db = new BalancerDbContext(options))
        {
            db.ExperimentalBalanceLogs.Add(BuildLog(balanceId, metaTime, posted: false));
            await db.SaveChangesAsync();
        }

        var sut = new ExperimentalBalanceInputService(new TestDbContextFactory(options));
        var body = new ExperimentalBalanceInputBody(
            Winners: [new ExperimentalBalanceInputPlayerLine(U1, 0, 0), new ExperimentalBalanceInputPlayerLine(U2, 0, 0)],
            Losers: [new ExperimentalBalanceInputPlayerLine(U3, 0, 0), new ExperimentalBalanceInputPlayerLine(U4, 0, 0)],
            GameId: PayloadMongoGameId);

        var result = await sut.InputAsync(balanceId, body, CancellationToken.None);

        Assert.False(result.Success);
        Assert.Equal(409, result.StatusCode);
    }

    [Fact]
    public async Task InputAsync_WhenAlreadyCounted_Returns409()
    {
        var dbName = Guid.NewGuid().ToString();
        var options = CreateOptions(dbName);
        var balanceId = Guid.NewGuid();
        var metaTime = new DateTime(2026, 4, 5, 12, 0, 0, DateTimeKind.Utc);

        await using (var db = new BalancerDbContext(options))
        {
            db.ExperimentalBalanceLogs.Add(BuildLog(balanceId, metaTime, counted: true, gameId: PayloadMongoGameId));
            db.ExperimentalSpecLogs.AddRange(
                new ExperimentalSpecLog { BalanceId = balanceId, Pyromancer = U1, Cryomancer = U2 },
                new ExperimentalSpecLog { BalanceId = balanceId, Aquamancer = U3, Berserker = U4 });
            foreach (var u in new[] { U1, U2, U3, U4 })
            {
                db.ExperimentalSpecsWl.Add(new ExperimentalSpecsWl { Uuid = u });
            }

            await db.SaveChangesAsync();
        }

        var sut = new ExperimentalBalanceInputService(new TestDbContextFactory(options));
        var body = new ExperimentalBalanceInputBody(
            Winners: [new ExperimentalBalanceInputPlayerLine(U1, 0, 0), new ExperimentalBalanceInputPlayerLine(U2, 0, 0)],
            Losers: [new ExperimentalBalanceInputPlayerLine(U3, 0, 0), new ExperimentalBalanceInputPlayerLine(U4, 0, 0)],
            GameId: PayloadMongoGameId);

        var result = await sut.InputAsync(balanceId, body, CancellationToken.None);

        Assert.False(result.Success);
        Assert.Equal(409, result.StatusCode);
    }

    [Fact]
    public async Task InputAsync_WhenCountedFalseAfterPriorApply_ReappliesAndAddsSecondAudit()
    {
        var dbName = Guid.NewGuid().ToString();
        var options = CreateOptions(dbName);
        var balanceId = Guid.NewGuid();
        var metaTime = new DateTime(2026, 4, 5, 12, 0, 0, DateTimeKind.Utc);
        var zeroBody = BuildValidZeroStatsBody();

        await using (var db = new BalancerDbContext(options))
        {
            db.ExperimentalBalanceLogs.Add(BuildLog(balanceId, metaTime));
            db.ExperimentalSpecLogs.AddRange(
                new ExperimentalSpecLog { BalanceId = balanceId, Pyromancer = U1, Cryomancer = U2 },
                new ExperimentalSpecLog { BalanceId = balanceId, Aquamancer = U3, Berserker = U4 });
            foreach (var u in new[] { U1, U2, U3, U4 })
            {
                db.ExperimentalSpecsWl.Add(new ExperimentalSpecsWl { Uuid = u });
            }

            await db.SaveChangesAsync();
        }

        var sut = new ExperimentalBalanceInputService(new TestDbContextFactory(options));
        Assert.True((await sut.InputAsync(balanceId, zeroBody, CancellationToken.None)).Success);

        await using (var mid = new BalancerDbContext(options))
        {
            var log = mid.ExperimentalBalanceLogs.Single(x => x.BalanceId == balanceId);
            log.Counted = false;
            await mid.SaveChangesAsync();
        }

        Assert.True((await sut.InputAsync(balanceId, zeroBody, CancellationToken.None)).Success);

        await using (var verify = new BalancerDbContext(options))
        {
            Assert.True(verify.ExperimentalBalanceLogs.Single(x => x.BalanceId == balanceId).Counted);
            Assert.Equal(2, verify.ExperimentalInputLogs.Count(x => x.BalanceId == balanceId));
            var w1 = verify.ExperimentalSpecsWl.Single(x => x.Uuid == U1);
            Assert.Equal(2, w1.PyromancerWins);
        }
    }

    [Fact]
    public async Task InputAsync_WhenStoredInputCountedFalseAndBodyMismatch_Returns400()
    {
        var dbName = Guid.NewGuid().ToString();
        var options = CreateOptions(dbName);
        var balanceId = Guid.NewGuid();
        var metaTime = new DateTime(2026, 4, 5, 12, 0, 0, DateTimeKind.Utc);
        var storedJson = SerializeInputBody(BuildValidZeroStatsBody());

        await using (var db = new BalancerDbContext(options))
        {
            db.ExperimentalBalanceLogs.Add(BuildLog(balanceId, metaTime, inputJson: storedJson, counted: false, gameId: PayloadMongoGameId));
            db.ExperimentalSpecLogs.AddRange(
                new ExperimentalSpecLog { BalanceId = balanceId, Pyromancer = U1, Cryomancer = U2 },
                new ExperimentalSpecLog { BalanceId = balanceId, Aquamancer = U3, Berserker = U4 });
            foreach (var u in new[] { U1, U2, U3, U4 })
            {
                db.ExperimentalSpecsWl.Add(new ExperimentalSpecsWl { Uuid = u });
            }

            await db.SaveChangesAsync();
        }

        var sut = new ExperimentalBalanceInputService(new TestDbContextFactory(options));
        var mismatched = new ExperimentalBalanceInputBody(
            Winners: [new ExperimentalBalanceInputPlayerLine(U1, 99, 0), new ExperimentalBalanceInputPlayerLine(U2, 0, 0)],
            Losers: [new ExperimentalBalanceInputPlayerLine(U3, 0, 0), new ExperimentalBalanceInputPlayerLine(U4, 0, 0)],
            GameId: PayloadMongoGameId);

        var result = await sut.InputAsync(balanceId, mismatched, CancellationToken.None);

        Assert.False(result.Success);
        Assert.Equal(400, result.StatusCode);

        await using (var verify = new BalancerDbContext(options))
        {
            var w1 = verify.ExperimentalSpecsWl.Single(x => x.Uuid == U1);
            Assert.Equal(0, w1.PyromancerWins);
            Assert.Empty(verify.ExperimentalInputLogs.Where(x => x.BalanceId == balanceId));
        }
    }

    [Fact]
    public async Task InputAsync_WhenGameIdMismatchesStored_Returns400()
    {
        var dbName = Guid.NewGuid().ToString();
        var options = CreateOptions(dbName);
        var balanceId = Guid.NewGuid();
        var metaTime = new DateTime(2026, 4, 5, 12, 0, 0, DateTimeKind.Utc);
        const string otherGameId = "bbbbbbbbbbbbbbbbbbbbbbbb";

        await using (var db = new BalancerDbContext(options))
        {
            db.ExperimentalBalanceLogs.Add(BuildLog(balanceId, metaTime, counted: false, gameId: PayloadMongoGameId));
            db.ExperimentalSpecLogs.AddRange(
                new ExperimentalSpecLog { BalanceId = balanceId, Pyromancer = U1, Cryomancer = U2 },
                new ExperimentalSpecLog { BalanceId = balanceId, Aquamancer = U3, Berserker = U4 });
            foreach (var u in new[] { U1, U2, U3, U4 })
            {
                db.ExperimentalSpecsWl.Add(new ExperimentalSpecsWl { Uuid = u });
            }

            await db.SaveChangesAsync();
        }

        var sut = new ExperimentalBalanceInputService(new TestDbContextFactory(options));
        var body = new ExperimentalBalanceInputBody(
            Winners: [new ExperimentalBalanceInputPlayerLine(U1, 0, 0), new ExperimentalBalanceInputPlayerLine(U2, 0, 0)],
            Losers: [new ExperimentalBalanceInputPlayerLine(U3, 0, 0), new ExperimentalBalanceInputPlayerLine(U4, 0, 0)],
            GameId: otherGameId);

        var result = await sut.InputAsync(balanceId, body, CancellationToken.None);

        Assert.False(result.Success);
        Assert.Equal(400, result.StatusCode);
    }

    [Fact]
    public async Task InputAsync_WhenMissingSpecAssignment_Returns400()
    {
        var dbName = Guid.NewGuid().ToString();
        var options = CreateOptions(dbName);
        var balanceId = Guid.NewGuid();
        var metaTime = new DateTime(2026, 4, 5, 12, 0, 0, DateTimeKind.Utc);

        await using (var db = new BalancerDbContext(options))
        {
            db.ExperimentalBalanceLogs.Add(BuildLog(balanceId, metaTime));
            db.ExperimentalSpecLogs.Add(new ExperimentalSpecLog { BalanceId = balanceId, Pyromancer = U1, Cryomancer = U2 });
            foreach (var u in new[] { U1, U2, U3, U4 })
            {
                db.ExperimentalSpecsWl.Add(new ExperimentalSpecsWl { Uuid = u });
            }

            await db.SaveChangesAsync();
        }

        var sut = new ExperimentalBalanceInputService(new TestDbContextFactory(options));
        var body = new ExperimentalBalanceInputBody(
            Winners: [new ExperimentalBalanceInputPlayerLine(U1, 0, 0), new ExperimentalBalanceInputPlayerLine(U2, 0, 0)],
            Losers: [new ExperimentalBalanceInputPlayerLine(U3, 0, 0), new ExperimentalBalanceInputPlayerLine(U4, 0, 0)],
            GameId: PayloadMongoGameId);

        var result = await sut.InputAsync(balanceId, body, CancellationToken.None);

        Assert.False(result.Success);
        Assert.Equal(400, result.StatusCode);
    }

    [Fact]
    public async Task InputAsync_WhenMissingSpecsWlRow_Returns404()
    {
        var dbName = Guid.NewGuid().ToString();
        var options = CreateOptions(dbName);
        var balanceId = Guid.NewGuid();
        var metaTime = new DateTime(2026, 4, 5, 12, 0, 0, DateTimeKind.Utc);

        await using (var db = new BalancerDbContext(options))
        {
            db.ExperimentalBalanceLogs.Add(BuildLog(balanceId, metaTime));
            db.ExperimentalSpecLogs.AddRange(
                new ExperimentalSpecLog { BalanceId = balanceId, Pyromancer = U1, Cryomancer = U2 },
                new ExperimentalSpecLog { BalanceId = balanceId, Aquamancer = U3, Berserker = U4 });
            db.ExperimentalSpecsWl.Add(new ExperimentalSpecsWl { Uuid = U1 });
            db.ExperimentalSpecsWl.Add(new ExperimentalSpecsWl { Uuid = U2 });
            db.ExperimentalSpecsWl.Add(new ExperimentalSpecsWl { Uuid = U3 });

            await db.SaveChangesAsync();
        }

        var sut = new ExperimentalBalanceInputService(new TestDbContextFactory(options));
        var body = new ExperimentalBalanceInputBody(
            Winners: [new ExperimentalBalanceInputPlayerLine(U1, 0, 0), new ExperimentalBalanceInputPlayerLine(U2, 0, 0)],
            Losers: [new ExperimentalBalanceInputPlayerLine(U3, 0, 0), new ExperimentalBalanceInputPlayerLine(U4, 0, 0)],
            GameId: PayloadMongoGameId);

        var result = await sut.InputAsync(balanceId, body, CancellationToken.None);

        Assert.False(result.Success);
        Assert.Equal(404, result.StatusCode);
    }

    [Fact]
    public async Task InputAsync_WhenInvalidGameId_Returns400()
    {
        var sut = new ExperimentalBalanceInputService(new TestDbContextFactory(CreateOptions(Guid.NewGuid().ToString())));
        var body = new ExperimentalBalanceInputBody(
            Winners: [new ExperimentalBalanceInputPlayerLine(U1, 0, 0)],
            Losers: [],
            GameId: "not-a-valid-object-id");

        var result = await sut.InputAsync(Guid.NewGuid(), body, CancellationToken.None);

        Assert.False(result.Success);
        Assert.Equal(400, result.StatusCode);
    }

    [Fact]
    public async Task UninputAsync_AfterInput_RestoresWlSetsCountedFalsePreservesInput_AddsUninputAudit()
    {
        var dbName = Guid.NewGuid().ToString();
        var options = CreateOptions(dbName);
        var balanceId = Guid.NewGuid();
        var metaTime = new DateTime(2026, 4, 5, 12, 0, 0, DateTimeKind.Utc);

        await using (var db = new BalancerDbContext(options))
        {
            db.ExperimentalBalanceLogs.Add(BuildLog(balanceId, metaTime));
            db.ExperimentalSpecLogs.AddRange(
                new ExperimentalSpecLog { BalanceId = balanceId, Pyromancer = U1, Cryomancer = U2 },
                new ExperimentalSpecLog { BalanceId = balanceId, Aquamancer = U3, Berserker = U4 });
            foreach (var u in new[] { U1, U2, U3, U4 })
            {
                db.ExperimentalSpecsWl.Add(new ExperimentalSpecsWl { Uuid = u });
            }

            await db.SaveChangesAsync();
        }

        var sut = new ExperimentalBalanceInputService(new TestDbContextFactory(options));
        var body = new ExperimentalBalanceInputBody(
            Winners:
            [
                new ExperimentalBalanceInputPlayerLine(U1, 5, 2),
                new ExperimentalBalanceInputPlayerLine(U2, 3, 1)
            ],
            Losers:
            [
                new ExperimentalBalanceInputPlayerLine(U3, 2, 5),
                new ExperimentalBalanceInputPlayerLine(U4, 1, 3)
            ],
            GameId: PayloadMongoGameId);

        Assert.True((await sut.InputAsync(balanceId, body, CancellationToken.None)).Success);

        string storedInputJson;
        await using (var beforeUninput = new BalancerDbContext(options))
        {
            storedInputJson = beforeUninput.ExperimentalBalanceLogs.Single(x => x.BalanceId == balanceId).Input!;
        }

        var uninput = await sut.UninputAsync(balanceId, CancellationToken.None);
        Assert.True(uninput.Success);
        Assert.Equal(200, uninput.StatusCode);

        await using (var verify = new BalancerDbContext(options))
        {
            var log = verify.ExperimentalBalanceLogs.Single(x => x.BalanceId == balanceId);
            Assert.False(log.Counted);
            Assert.Equal(storedInputJson, log.Input);
            Assert.Equal(PayloadMongoGameId, log.GameId);

            var audits = verify.ExperimentalInputLogs.Where(x => x.BalanceId == balanceId).OrderBy(x => x.OccurredAt).ToList();
            Assert.Equal(2, audits.Count);
            Assert.Equal("input", audits[0].Action);
            Assert.Equal("uninput", audits[1].Action);
            Assert.Equal(PayloadMongoGameId, audits[1].GameId);

            var w1 = verify.ExperimentalSpecsWl.Single(x => x.Uuid == U1);
            Assert.Equal(0, w1.PyromancerWins);
            Assert.Equal(0, w1.PyromancerKills);
            Assert.Equal(0, w1.PyromancerDeaths);
        }
    }

    [Fact]
    public async Task UninputAsync_WhenNotCounted_Returns409()
    {
        var dbName = Guid.NewGuid().ToString();
        var options = CreateOptions(dbName);
        var balanceId = Guid.NewGuid();
        var metaTime = new DateTime(2026, 4, 5, 12, 0, 0, DateTimeKind.Utc);

        await using (var db = new BalancerDbContext(options))
        {
            db.ExperimentalBalanceLogs.Add(BuildLog(balanceId, metaTime, inputJson: SerializeInputBody(BuildValidZeroStatsBody()), counted: false));
            db.ExperimentalSpecLogs.AddRange(
                new ExperimentalSpecLog { BalanceId = balanceId, Pyromancer = U1, Cryomancer = U2 },
                new ExperimentalSpecLog { BalanceId = balanceId, Aquamancer = U3, Berserker = U4 });
            foreach (var u in new[] { U1, U2, U3, U4 })
            {
                db.ExperimentalSpecsWl.Add(new ExperimentalSpecsWl { Uuid = u });
            }

            await db.SaveChangesAsync();
        }

        var sut = new ExperimentalBalanceInputService(new TestDbContextFactory(options));
        var result = await sut.UninputAsync(balanceId, CancellationToken.None);

        Assert.False(result.Success);
        Assert.Equal(409, result.StatusCode);
    }

    [Fact]
    public async Task UninputAsync_WhenCountedButNoInput_Returns400()
    {
        var dbName = Guid.NewGuid().ToString();
        var options = CreateOptions(dbName);
        var balanceId = Guid.NewGuid();
        var metaTime = new DateTime(2026, 4, 5, 12, 0, 0, DateTimeKind.Utc);

        await using (var db = new BalancerDbContext(options))
        {
            db.ExperimentalBalanceLogs.Add(BuildLog(balanceId, metaTime, counted: true, gameId: PayloadMongoGameId));
            await db.SaveChangesAsync();
        }

        var sut = new ExperimentalBalanceInputService(new TestDbContextFactory(options));
        var result = await sut.UninputAsync(balanceId, CancellationToken.None);

        Assert.False(result.Success);
        Assert.Equal(400, result.StatusCode);
    }

    [Fact]
    public async Task UninputAsync_WhenStoredInputInvalidJson_Returns400()
    {
        var dbName = Guid.NewGuid().ToString();
        var options = CreateOptions(dbName);
        var balanceId = Guid.NewGuid();
        var metaTime = new DateTime(2026, 4, 5, 12, 0, 0, DateTimeKind.Utc);

        await using (var db = new BalancerDbContext(options))
        {
            db.ExperimentalBalanceLogs.Add(BuildLog(balanceId, metaTime, inputJson: "{", counted: true, gameId: PayloadMongoGameId));
            await db.SaveChangesAsync();
        }

        var sut = new ExperimentalBalanceInputService(new TestDbContextFactory(options));
        var result = await sut.UninputAsync(balanceId, CancellationToken.None);

        Assert.False(result.Success);
        Assert.Equal(400, result.StatusCode);
    }

    [Fact]
    public async Task UninputAsync_SecondCall_Returns409()
    {
        var dbName = Guid.NewGuid().ToString();
        var options = CreateOptions(dbName);
        var balanceId = Guid.NewGuid();
        var metaTime = new DateTime(2026, 4, 5, 12, 0, 0, DateTimeKind.Utc);

        await using (var db = new BalancerDbContext(options))
        {
            db.ExperimentalBalanceLogs.Add(BuildLog(balanceId, metaTime));
            db.ExperimentalSpecLogs.AddRange(
                new ExperimentalSpecLog { BalanceId = balanceId, Pyromancer = U1, Cryomancer = U2 },
                new ExperimentalSpecLog { BalanceId = balanceId, Aquamancer = U3, Berserker = U4 });
            foreach (var u in new[] { U1, U2, U3, U4 })
            {
                db.ExperimentalSpecsWl.Add(new ExperimentalSpecsWl { Uuid = u });
            }

            await db.SaveChangesAsync();
        }

        var sut = new ExperimentalBalanceInputService(new TestDbContextFactory(options));
        Assert.True((await sut.InputAsync(balanceId, BuildValidZeroStatsBody(), CancellationToken.None)).Success);
        Assert.True((await sut.UninputAsync(balanceId, CancellationToken.None)).Success);

        var second = await sut.UninputAsync(balanceId, CancellationToken.None);
        Assert.False(second.Success);
        Assert.Equal(409, second.StatusCode);
    }

    [Fact]
    public async Task UninputAsync_WhenWlStatsTampered_Returns409AndDoesNotCommit()
    {
        var dbName = Guid.NewGuid().ToString();
        var options = CreateOptions(dbName);
        var balanceId = Guid.NewGuid();
        var metaTime = new DateTime(2026, 4, 5, 12, 0, 0, DateTimeKind.Utc);

        await using (var db = new BalancerDbContext(options))
        {
            db.ExperimentalBalanceLogs.Add(BuildLog(balanceId, metaTime));
            db.ExperimentalSpecLogs.AddRange(
                new ExperimentalSpecLog { BalanceId = balanceId, Pyromancer = U1, Cryomancer = U2 },
                new ExperimentalSpecLog { BalanceId = balanceId, Aquamancer = U3, Berserker = U4 });
            foreach (var u in new[] { U1, U2, U3, U4 })
            {
                db.ExperimentalSpecsWl.Add(new ExperimentalSpecsWl { Uuid = u });
            }

            await db.SaveChangesAsync();
        }

        var sut = new ExperimentalBalanceInputService(new TestDbContextFactory(options));
        var body = new ExperimentalBalanceInputBody(
            Winners: [new ExperimentalBalanceInputPlayerLine(U1, 0, 0), new ExperimentalBalanceInputPlayerLine(U2, 0, 0)],
            Losers: [new ExperimentalBalanceInputPlayerLine(U3, 0, 0), new ExperimentalBalanceInputPlayerLine(U4, 0, 0)],
            GameId: PayloadMongoGameId);
        Assert.True((await sut.InputAsync(balanceId, body, CancellationToken.None)).Success);

        await using (var tamper = new BalancerDbContext(options))
        {
            var w1 = tamper.ExperimentalSpecsWl.Single(x => x.Uuid == U1);
            w1.PyromancerWins = 0;
            await tamper.SaveChangesAsync();
        }

        var uninput = await sut.UninputAsync(balanceId, CancellationToken.None);
        Assert.False(uninput.Success);
        Assert.Equal(409, uninput.StatusCode);

        await using (var verify = new BalancerDbContext(options))
        {
            Assert.True(verify.ExperimentalBalanceLogs.Single(x => x.BalanceId == balanceId).Counted);
            Assert.Single(verify.ExperimentalInputLogs.Where(x => x.BalanceId == balanceId));
        }
    }

    [Fact]
    public async Task ClearInputAsync_WhenValid_ClearsInputAndWritesClearAudit()
    {
        var dbName = Guid.NewGuid().ToString();
        var options = CreateOptions(dbName);
        var balanceId = Guid.NewGuid();
        var metaTime = new DateTime(2026, 4, 5, 12, 0, 0, DateTimeKind.Utc);
        var inputJson = SerializeInputBody(BuildValidZeroStatsBody());

        await using (var db = new BalancerDbContext(options))
        {
            db.ExperimentalBalanceLogs.Add(BuildLog(
                balanceId,
                metaTime,
                inputJson: inputJson,
                counted: true,
                gameId: PayloadMongoGameId));
            await db.SaveChangesAsync();
        }

        var sut = new ExperimentalBalanceInputService(new TestDbContextFactory(options));
        var result = await sut.ClearInputAsync(balanceId, CancellationToken.None);

        Assert.True(result.Success);
        Assert.Equal(200, result.StatusCode);

        await using (var verify = new BalancerDbContext(options))
        {
            var log = verify.ExperimentalBalanceLogs.Single(x => x.BalanceId == balanceId);
            Assert.Null(log.Input);

            var clearAudit = verify.ExperimentalInputLogs.Single(x => x.BalanceId == balanceId);
            Assert.Equal("clear", clearAudit.Action);
            Assert.Equal(PayloadMongoGameId, clearAudit.GameId);
        }
    }

    [Fact]
    public async Task ClearInputAsync_WhenBalanceMissing_Returns404()
    {
        var options = CreateOptions(Guid.NewGuid().ToString());
        var sut = new ExperimentalBalanceInputService(new TestDbContextFactory(options));

        var result = await sut.ClearInputAsync(Guid.NewGuid(), CancellationToken.None);

        Assert.False(result.Success);
        Assert.Equal(404, result.StatusCode);
    }

    [Fact]
    public async Task ClearInputAsync_WhenStoredInputJsonInvalid_Returns400()
    {
        var dbName = Guid.NewGuid().ToString();
        var options = CreateOptions(dbName);
        var balanceId = Guid.NewGuid();
        var metaTime = new DateTime(2026, 4, 5, 12, 0, 0, DateTimeKind.Utc);

        await using (var db = new BalancerDbContext(options))
        {
            db.ExperimentalBalanceLogs.Add(BuildLog(
                balanceId,
                metaTime,
                inputJson: "not-json",
                gameId: PayloadMongoGameId));
            await db.SaveChangesAsync();
        }

        var sut = new ExperimentalBalanceInputService(new TestDbContextFactory(options));
        var result = await sut.ClearInputAsync(balanceId, CancellationToken.None);

        Assert.False(result.Success);
        Assert.Equal(400, result.StatusCode);
    }

    [Fact]
    public async Task ClearInputAsync_WhenStoredInputHasInvalidGameId_Returns400()
    {
        var dbName = Guid.NewGuid().ToString();
        var options = CreateOptions(dbName);
        var balanceId = Guid.NewGuid();
        var metaTime = new DateTime(2026, 4, 5, 12, 0, 0, DateTimeKind.Utc);
        var badGameBody = new ExperimentalBalanceInputBody(
            Winners: [new ExperimentalBalanceInputPlayerLine(U1, 0, 0), new ExperimentalBalanceInputPlayerLine(U2, 0, 0)],
            Losers: [new ExperimentalBalanceInputPlayerLine(U3, 0, 0), new ExperimentalBalanceInputPlayerLine(U4, 0, 0)],
            GameId: "not-an-object-id");

        await using (var db = new BalancerDbContext(options))
        {
            db.ExperimentalBalanceLogs.Add(BuildLog(
                balanceId,
                metaTime,
                inputJson: SerializeInputBody(badGameBody),
                gameId: null));
            await db.SaveChangesAsync();
        }

        var sut = new ExperimentalBalanceInputService(new TestDbContextFactory(options));
        var result = await sut.ClearInputAsync(balanceId, CancellationToken.None);

        Assert.False(result.Success);
        Assert.Equal(400, result.StatusCode);
    }

    [Fact]
    public async Task ClearInputAsync_WhenGameIdMismatch_Returns400()
    {
        var dbName = Guid.NewGuid().ToString();
        var options = CreateOptions(dbName);
        var balanceId = Guid.NewGuid();
        var metaTime = new DateTime(2026, 4, 5, 12, 0, 0, DateTimeKind.Utc);

        var otherGameBody = new ExperimentalBalanceInputBody(
            Winners: [new ExperimentalBalanceInputPlayerLine(U1, 0, 0), new ExperimentalBalanceInputPlayerLine(U2, 0, 0)],
            Losers: [new ExperimentalBalanceInputPlayerLine(U3, 0, 0), new ExperimentalBalanceInputPlayerLine(U4, 0, 0)],
            GameId: "bbbbbbbbbbbbbbbbbbbbbbbb");

        await using (var db = new BalancerDbContext(options))
        {
            db.ExperimentalBalanceLogs.Add(BuildLog(
                balanceId,
                metaTime,
                inputJson: SerializeInputBody(otherGameBody),
                gameId: PayloadMongoGameId));
            await db.SaveChangesAsync();
        }

        var sut = new ExperimentalBalanceInputService(new TestDbContextFactory(options));
        var result = await sut.ClearInputAsync(balanceId, CancellationToken.None);

        Assert.False(result.Success);
        Assert.Equal(400, result.StatusCode);
    }

    [Fact]
    public async Task ClearInputAsync_WhenLogGameIdNull_UsesGameIdFromStoredInputJson()
    {
        var dbName = Guid.NewGuid().ToString();
        var options = CreateOptions(dbName);
        var balanceId = Guid.NewGuid();
        var metaTime = new DateTime(2026, 4, 5, 12, 0, 0, DateTimeKind.Utc);

        await using (var db = new BalancerDbContext(options))
        {
            db.ExperimentalBalanceLogs.Add(BuildLog(
                balanceId,
                metaTime,
                inputJson: SerializeInputBody(BuildValidZeroStatsBody()),
                gameId: null));
            await db.SaveChangesAsync();
        }

        var sut = new ExperimentalBalanceInputService(new TestDbContextFactory(options));
        var result = await sut.ClearInputAsync(balanceId, CancellationToken.None);

        Assert.True(result.Success);

        await using (var verify = new BalancerDbContext(options))
        {
            Assert.Null(verify.ExperimentalBalanceLogs.Single(x => x.BalanceId == balanceId).Input);
            var audit = verify.ExperimentalInputLogs.Single(x => x.BalanceId == balanceId);
            Assert.Equal("clear", audit.Action);
            Assert.Equal(PayloadMongoGameId, audit.GameId);
        }
    }

    [Fact]
    public async Task ClearInputAsync_WhenInputAlreadyNull_Returns409()
    {
        var dbName = Guid.NewGuid().ToString();
        var options = CreateOptions(dbName);
        var balanceId = Guid.NewGuid();
        var metaTime = new DateTime(2026, 4, 5, 12, 0, 0, DateTimeKind.Utc);

        await using (var db = new BalancerDbContext(options))
        {
            db.ExperimentalBalanceLogs.Add(BuildLog(
                balanceId,
                metaTime,
                inputJson: null,
                gameId: PayloadMongoGameId));
            await db.SaveChangesAsync();
        }

        var sut = new ExperimentalBalanceInputService(new TestDbContextFactory(options));
        var result = await sut.ClearInputAsync(balanceId, CancellationToken.None);

        Assert.False(result.Success);
        Assert.Equal(409, result.StatusCode);
    }
}
