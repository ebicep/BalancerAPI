using BalancerAPI.Api.Controllers;
using BalancerAPI.Business.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace BalancerAPI.Tests.Controllers;

public class PlayerControllerTests
{
    private static readonly Guid U1 = Guid.Parse("a1b2c3d4-e5f6-7890-abcd-ef1234567890");

    [Fact]
    public async Task Get_WhenResolvedByName_CallsGetServiceWithUuid()
    {
        var resolver = new Mock<IPlayerKeyResolver>();
        resolver.Setup(x => x.ResolveAsync("Amy", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PlayerKeyResolveResult(true, 200, null, U1, "Amy"));

        var getService = new Mock<IPlayerGetService>();
        var payload = new PlayerGetPayload("Amy", U1, new Dictionary<string, object>());
        getService.Setup(x => x.GetAsync(U1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(PlayerGetServiceResult.Ok(payload));

        var controller = CreateController(resolver.Object, getService.Object);

        var result = await controller.Get("Amy", CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<PlayerGetResponse>(ok.Value);
        Assert.Equal("Amy", response.Name);
        Assert.Equal(U1, response.Uuid);
        getService.Verify(x => x.GetAsync(U1, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Get_WhenResolverFails_ReturnsProblemWithStatus()
    {
        var resolver = new Mock<IPlayerKeyResolver>();
        resolver.Setup(x => x.ResolveAsync("nobody", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PlayerKeyResolveResult(
                false,
                404,
                "No matching UUID found in names table for: nobody.",
                null,
                null));

        var getService = new Mock<IPlayerGetService>();
        var controller = CreateController(resolver.Object, getService.Object);

        var result = await controller.Get("nobody", CancellationToken.None);

        var obj = Assert.IsType<ObjectResult>(result.Result);
        Assert.Equal(StatusCodes.Status404NotFound, obj.StatusCode);
        var pd = Assert.IsType<ProblemDetails>(obj.Value);
        Assert.Contains("nobody", pd.Detail);
        getService.Verify(x => x.GetAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    private static PlayerController CreateController(
        IPlayerKeyResolver playerKeyResolver,
        IPlayerGetService playerGetService)
    {
        return new PlayerController(
            Mock.Of<IPlayerAddService>(),
            playerGetService,
            Mock.Of<IPlayerDeleteService>(),
            Mock.Of<IPlayerUuidUpdateService>(),
            playerKeyResolver);
    }
}
