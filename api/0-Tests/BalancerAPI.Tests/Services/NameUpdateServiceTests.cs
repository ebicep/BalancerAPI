using System.Net;
using System.Net.Http;
using BalancerAPI.Data;
using BalancerAPI.Domain.Models;
using BalancerAPI.Services;
using Microsoft.EntityFrameworkCore;
using Moq;
using Moq.Protected;

namespace BalancerAPI.Tests.Services;

public class NameUpdateServiceTests
{
    private static readonly Guid KnownUuid = Guid.Parse("9f2b2230-3b2c-4b0f-a141-d7b598e236c7");
    private const string KnownUuidNoDash = "9f2b22303b2c4b0fa141d7b598e236c7";

    [Fact]
    public async Task UpdateNamesAsync_WhenNameUnchanged_ReturnsNoUpdates()
    {
        var uuid = KnownUuid;
        await using var dbContext = CreateDbContext();
        dbContext.Names.Add(new PlayerName { Uuid = uuid, Name = "oldName", PreviousNames = [] });
        await dbContext.SaveChangesAsync();

        var handler = CreateMockHandler(_ => true, $$$"""{"id":"{{{KnownUuidNoDash}}}","name":"oldName","properties":[],"profileActions":[]}""");
        var service = new NameUpdateService(dbContext, CreateHttpClient(handler));

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

        var handler = CreateMockHandler(_ => true, $$$"""{"id":"{{{KnownUuidNoDash}}}","name":"sumSmash","properties":[],"profileActions":[]}""");
        var service = new NameUpdateService(dbContext, CreateHttpClient(handler));

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

        var handler = CreateMockHandler(_ => true, "{}");
        var service = new NameUpdateService(dbContext, CreateHttpClient(handler));

        var updated = await service.UpdateNamesAsync(CancellationToken.None);

        Assert.Empty(updated);
        var row = dbContext.Names.Single();
        Assert.Equal("oldName", row.Name);
        Assert.Empty(row.PreviousNames);
    }

    [Fact]
    public async Task UpdateNamesAsync_UsesUuidWithoutDashesInMojangUrl()
    {
        var uuid = KnownUuid;
        var expectedPath = $"/session/minecraft/profile/{uuid:N}".ToLowerInvariant();

        await using var dbContext = CreateDbContext();
        dbContext.Names.Add(new PlayerName { Uuid = uuid, Name = "oldName", PreviousNames = [] });
        await dbContext.SaveChangesAsync();

        var handler = new Mock<HttpMessageHandler>(MockBehavior.Strict);
        handler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(request =>
                    request.Method == HttpMethod.Get &&
                    request.RequestUri != null &&
                    request.RequestUri.AbsolutePath == expectedPath),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent($$$"""{"id":"{{{KnownUuidNoDash}}}","name":"sumSmash","properties":[],"profileActions":[]}""")
            })
            .Verifiable();

        var service = new NameUpdateService(dbContext, CreateHttpClient(handler));
        _ = await service.UpdateNamesAsync(CancellationToken.None);

        handler.Protected().Verify(
            "SendAsync",
            Times.Once(),
            ItExpr.IsAny<HttpRequestMessage>(),
            ItExpr.IsAny<CancellationToken>());
    }

    [Fact]
    public async Task UpdateNamesAsync_WithMultipleRows_UpdatesOnlyChangedRowsAfterParallelFetches()
    {
        var uuid1 = KnownUuid;
        var uuid2 = Guid.Parse("11111111-2222-3333-4444-555555555555");
        var uuid3 = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");
        var noDash2 = uuid2.ToString("N").ToLowerInvariant();
        var noDash3 = uuid3.ToString("N").ToLowerInvariant();

        await using var dbContext = CreateDbContext();
        dbContext.Names.AddRange(
            new PlayerName { Uuid = uuid1, Name = "oldName", PreviousNames = [] },
            new PlayerName { Uuid = uuid2, Name = "sameName", PreviousNames = [] },
            new PlayerName { Uuid = uuid3, Name = "otherOld", PreviousNames = [] });
        await dbContext.SaveChangesAsync();

        var handler = CreateMockHandler(
            _ => true,
            request =>
            {
                var path = request.RequestUri!.AbsolutePath;
                if (path.EndsWith(KnownUuidNoDash, StringComparison.Ordinal))
                {
                    return $$$"""{"id":"{{{KnownUuidNoDash}}}","name":"sumSmash","properties":[],"profileActions":[]}""";
                }

                if (path.EndsWith(noDash2, StringComparison.Ordinal))
                {
                    return $$$"""{"id":"{{{noDash2}}}","name":"sameName","properties":[],"profileActions":[]}""";
                }

                if (path.EndsWith(noDash3, StringComparison.Ordinal))
                {
                    return $$$"""{"id":"{{{noDash3}}}","name":"otherNew","properties":[],"profileActions":[]}""";
                }

                throw new InvalidOperationException($"Unexpected path {path}");
            });

        var service = new NameUpdateService(dbContext, CreateHttpClient(handler));

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

    private static Mock<HttpMessageHandler> CreateMockHandler(
        Func<HttpRequestMessage, bool> requestMatcher,
        string responseBody)
    {
        var handler = new Mock<HttpMessageHandler>(MockBehavior.Strict);
        handler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(request => requestMatcher(request)),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(responseBody)
            });
        return handler;
    }

    private static Mock<HttpMessageHandler> CreateMockHandler(
        Func<HttpRequestMessage, bool> requestMatcher,
        Func<HttpRequestMessage, string> responseBodyFactory)
    {
        var handler = new Mock<HttpMessageHandler>(MockBehavior.Strict);
        handler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(request => requestMatcher(request)),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync((HttpRequestMessage request, CancellationToken _) => new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(responseBodyFactory(request))
            });
        return handler;
    }

    private static HttpClient CreateHttpClient(Mock<HttpMessageHandler> handler)
    {
        return new HttpClient(handler.Object)
        {
            BaseAddress = new Uri("https://sessionserver.mojang.com/")
        };
    }
}
