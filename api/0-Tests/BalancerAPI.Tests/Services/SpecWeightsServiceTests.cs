using BalancerAPI.Business.Services;
using BalancerAPI.Data.Data;
using BalancerAPI.Domain.Models;
using Microsoft.EntityFrameworkCore;

namespace BalancerAPI.Tests.Services;

public class SpecWeightsServiceTests
{
    private static readonly Guid TestUuid = Guid.Parse("a1b2c3d4-e5f6-7890-abcd-ef1234567890");
    private static readonly DateTime FixedLastUpdated = new(2025, 3, 1, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public async Task GetCombinedAsync_WhenBothRowsExist_ReturnsBaseWeightMinusEachOffset()
    {
        await using var db = CreateDbContext();
        db.BaseWeights.Add(new BaseWeight { Uuid = TestUuid, Weight = 1000 });
        db.ExperimentalSpecWeights.Add(new ExperimentalSpecWeight
        {
            Uuid = TestUuid,
            LastUpdated = FixedLastUpdated,
            PyromancerOffset = 1,
            CryomancerOffset = 2,
            AquamancerOffset = 3,
            BerserkerOffset = 4,
            DefenderOffset = 5,
            RevenantOffset = 6,
            AvengerOffset = 7,
            CrusaderOffset = 8,
            ProtectorOffset = 9,
            ThunderlordOffset = 10,
            SpiritguardOffset = 11,
            EarthwardenOffset = 12,
            AssassinOffset = 13,
            VindicatorOffset = 14,
            ApothecaryOffset = 15,
            ConjurerOffset = 16,
            SentinelOffset = 17,
            LuminaryOffset = 18
        });
        await db.SaveChangesAsync();

        var service = new SpecWeightsService(db);
        var result = await service.GetCombinedAsync(TestUuid, CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(999, result.Pyromancer);
        Assert.Equal(998, result.Cryomancer);
        Assert.Equal(997, result.Aquamancer);
        Assert.Equal(996, result.Berserker);
        Assert.Equal(995, result.Defender);
        Assert.Equal(994, result.Revenant);
        Assert.Equal(993, result.Avenger);
        Assert.Equal(992, result.Crusader);
        Assert.Equal(991, result.Protector);
        Assert.Equal(990, result.Thunderlord);
        Assert.Equal(989, result.Spiritguard);
        Assert.Equal(988, result.Earthwarden);
        Assert.Equal(987, result.Assassin);
        Assert.Equal(986, result.Vindicator);
        Assert.Equal(985, result.Apothecary);
        Assert.Equal(984, result.Conjurer);
        Assert.Equal(983, result.Sentinel);
        Assert.Equal(982, result.Luminary);
    }

    [Fact]
    public async Task GetCombinedAsync_WhenOffsetsAreZero_ReturnsBaseWeightForEveryClass()
    {
        await using var db = CreateDbContext();
        const int baseWeight = 42;
        db.BaseWeights.Add(new BaseWeight { Uuid = TestUuid, Weight = baseWeight });
        db.ExperimentalSpecWeights.Add(new ExperimentalSpecWeight
        {
            Uuid = TestUuid,
            LastUpdated = FixedLastUpdated
        });
        await db.SaveChangesAsync();

        var service = new SpecWeightsService(db);
        var result = await service.GetCombinedAsync(TestUuid, CancellationToken.None);

        Assert.NotNull(result);
        foreach (var prop in typeof(SpecWeightsResponse).GetProperties().Where(p => p.PropertyType == typeof(int)))
        {
            Assert.Equal(baseWeight, (int)prop.GetValue(result)!);
        }
    }

    [Fact]
    public async Task GetCombinedAsync_WhenOnlyBaseWeight_ReturnsNull()
    {
        await using var db = CreateDbContext();
        db.BaseWeights.Add(new BaseWeight { Uuid = TestUuid, Weight = 10 });
        await db.SaveChangesAsync();

        var service = new SpecWeightsService(db);
        var result = await service.GetCombinedAsync(TestUuid, CancellationToken.None);

        Assert.Null(result);
    }

    [Fact]
    public async Task GetCombinedAsync_WhenOnlyExperimentalSpecWeight_ReturnsNull()
    {
        await using var db = CreateDbContext();
        db.ExperimentalSpecWeights.Add(new ExperimentalSpecWeight
        {
            Uuid = TestUuid,
            LastUpdated = FixedLastUpdated,
            PyromancerOffset = 5
        });
        await db.SaveChangesAsync();

        var service = new SpecWeightsService(db);
        var result = await service.GetCombinedAsync(TestUuid, CancellationToken.None);

        Assert.Null(result);
    }

    [Fact]
    public async Task GetCombinedAsync_WhenUuidDoesNotMatch_ReturnsNull()
    {
        await using var db = CreateDbContext();
        var other = Guid.Parse("b2c3d4e5-f6a7-8901-bcde-f12345678901");
        db.BaseWeights.Add(new BaseWeight { Uuid = TestUuid, Weight = 1 });
        db.ExperimentalSpecWeights.Add(new ExperimentalSpecWeight { Uuid = TestUuid, LastUpdated = FixedLastUpdated });
        await db.SaveChangesAsync();

        var service = new SpecWeightsService(db);
        var result = await service.GetCombinedAsync(other, CancellationToken.None);

        Assert.Null(result);
    }

    private static BalancerDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<BalancerDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new BalancerDbContext(options);
    }
}