using System.Net;
using BalancerAPI.Business.Services;
using Moq;
using Moq.Protected;

namespace BalancerAPI.Tests.Services;

public class MinecraftPlayerResolveServiceTests
{
    [Fact]
    public async Task ResolveAsync_WhenInputIsNotUuid_ReturnsBadRequest()
    {
        var service = new MinecraftPlayerResolveService(new HttpClient());

        var result = await service.ResolveAsync("sumSmash", CancellationToken.None);

        Assert.False(result.Success);
        Assert.Equal(400, result.StatusCode);
    }

    [Fact]
    public async Task ResolveAsync_WhenGivenUuid_UsesSessionProfileEndpoint()
    {
        var inputUuid = Guid.Parse("9f2b2230-3b2c-4b0f-a141-d7b598e236c7");
        var expectedPath = $"/session/minecraft/profile/{inputUuid:N}".ToLowerInvariant();

        var handler = new Mock<HttpMessageHandler>(MockBehavior.Strict);
        handler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(request =>
                    request.Method == HttpMethod.Get &&
                    request.RequestUri != null &&
                    request.RequestUri.Host == "sessionserver.mojang.com" &&
                    request.RequestUri.AbsolutePath == expectedPath),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("""{"id":"9f2b22303b2c4b0fa141d7b598e236c7","name":"sumSmash"}""")
            });

        var service = new MinecraftPlayerResolveService(new HttpClient(handler.Object));

        var result = await service.ResolveAsync(inputUuid.ToString(), CancellationToken.None);

        Assert.True(result.Success);
        Assert.Equal(inputUuid, result.Uuid);
        Assert.Equal("sumSmash", result.Name);
    }
}
