using BalancerAPI.Business.Services;
using BalancerAPI.Data.Data;
using BalancerAPI.Domain.Models;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace BalancerAPI.Tests.Services;

public class NameUpdateServiceTests
{
    private static readonly Guid KnownUuid = Guid.Parse("9f2b2230-3b2c-4b0f-a141-d7b598e236c7");

    [Fact]
    public async Task UpdateNamesAsync_WhenNameUnchanged_ReturnsNoUpdates()
    {
        var uuid = KnownUuid;
        await using var dbContext = CreateDbContext();
        dbContext.Names.Add(new PlayerName { Uuid = uuid, Name = "oldName", PreviousNames = [] });
        await dbContext.SaveChangesAsync();

        var resolver = CreateResolverMock(new Dictionary<Guid, PlayerResolveResult>
        {
            [uuid] = PlayerResolveResult.Ok(uuid, "oldName")
        });
        var service = new NameUpdateService(dbContext, resolver.Object);

        var updated = await service.UpdateNamesAsync(CancellationToken.None);

        Assert.Empty(updated);
        Assert.Equal("oldName", dbContext.Names.Single().Name);
        Assert.Empty(dbContext.Names.Single().PreviousNames);
    }

    [Fact]
    public async Task UpdateNamesAsync_WhenNameChanged_UpdatesRowAndReturnsPayload()
    {
        var uuid = KnownUuid;
        await using var dbContext = CreateDbContext();
        dbContext.Names.Add(new PlayerName { Uuid = uuid, Name = "oldName", PreviousNames = [] });
        await dbContext.SaveChangesAsync();

        var resolver = CreateResolverMock(new Dictionary<Guid, PlayerResolveResult>
        {
            [uuid] = PlayerResolveResult.Ok(uuid, "sumSmash")
        });
        var service = new NameUpdateService(dbContext, resolver.Object);

        var updated = await service.UpdateNamesAsync(CancellationToken.None);

        var change = Assert.Single(updated);
        Assert.Equal(uuid, change.Uuid);
        Assert.Equal("oldName", change.Previous);
        Assert.Equal("sumSmash", change.Current);

        var row = dbContext.Names.Single();
        Assert.Equal("sumSmash", row.Name);
        Assert.Contains("oldName", row.PreviousNames);
    }

    [Fact]
    public async Task UpdateNamesAsync_WhenExternalResponseHasNoName_SkipsUpdate()
    {
        var uuid = KnownUuid;
        await using var dbContext = CreateDbContext();
        dbContext.Names.Add(new PlayerName { Uuid = uuid, Name = "oldName", PreviousNames = [] });
        await dbContext.SaveChangesAsync();

        var resolver = CreateResolverMock(new Dictionary<Guid, PlayerResolveResult>
        {
            [uuid] = PlayerResolveResult.Ok(uuid, string.Empty)
        });
        var service = new NameUpdateService(dbContext, resolver.Object);

        var updated = await service.UpdateNamesAsync(CancellationToken.None);

        Assert.Empty(updated);
        var row = dbContext.Names.Single();
        Assert.Equal("oldName", row.Name);
        Assert.Empty(row.PreviousNames);
    }

    [Fact]
    public async Task UpdateNamesAsync_WithMultipleRows_UpdatesOnlyChangedRowsAfterParallelFetches()
    {
        var uuid1 = KnownUuid;
        var uuid2 = Guid.Parse("11111111-2222-3333-4444-555555555555");
        var uuid3 = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");
        await using var dbContext = CreateDbContext();
        dbContext.Names.AddRange(
            new PlayerName { Uuid = uuid1, Name = "oldName", PreviousNames = [] },
            new PlayerName { Uuid = uuid2, Name = "sameName", PreviousNames = [] },
            new PlayerName { Uuid = uuid3, Name = "otherOld", PreviousNames = [] });
        await dbContext.SaveChangesAsync();

        var resolver = CreateResolverMock(new Dictionary<Guid, PlayerResolveResult>
        {
            [uuid1] = PlayerResolveResult.Ok(uuid1, "sumSmash"),
            [uuid2] = PlayerResolveResult.Ok(uuid2, "sameName"),
            [uuid3] = PlayerResolveResult.Ok(uuid3, "otherNew")
        });
        var service = new NameUpdateService(dbContext, resolver.Object);

        var updated = await service.UpdateNamesAsync(CancellationToken.None);

        Assert.Equal(2, updated.Count);
        Assert.Contains(updated, entry => entry.Uuid == uuid1 && entry.Previous == "oldName" && entry.Current == "sumSmash");
        Assert.Contains(updated, entry => entry.Uuid == uuid3 && entry.Previous == "otherOld" && entry.Current == "otherNew");
        Assert.DoesNotContain(updated, entry => entry.Uuid == uuid2);

        var all = dbContext.Names.ToDictionary(x => x.Uuid);
        Assert.Equal("sumSmash", all[uuid1].Name);
        Assert.Equal("sameName", all[uuid2].Name);
        Assert.Equal("otherNew", all[uuid3].Name);
        Assert.Contains("oldName", all[uuid1].PreviousNames);
        Assert.Contains("otherOld", all[uuid3].PreviousNames);
        Assert.Empty(all[uuid2].PreviousNames);
    }

    private static BalancerDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<BalancerDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new BalancerDbContext(options);
    }

    private static Mock<IMinecraftPlayerResolveService> CreateResolverMock(
        IReadOnlyDictionary<Guid, PlayerResolveResult> responses)
    {
        var resolver = new Mock<IMinecraftPlayerResolveService>(MockBehavior.Strict);
        resolver
            .Setup(x => x.ResolveAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((string player, CancellationToken _) =>
            {
                if (!Guid.TryParse(player, out var parsed))
                {
                    return PlayerResolveResult.Fail(400, "UUID format is invalid.");
                }

                return responses.TryGetValue(parsed, out var response)
                    ? response
                    : PlayerResolveResult.Fail(404, "UUID was not found in Mojang session profile.");
            });

        return resolver;
    }
}