using BalancerAPI.Controllers;
using BalancerAPI.Services;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace BalancerAPI.Tests.Controllers;

public class ExperimentalSpecWeightsControllerTests
{
    private static readonly Guid TestUuid = Guid.Parse("a1b2c3d4-e5f6-7890-abcd-ef1234567890");

    [Fact]
    public async Task Get_WhenFound_ReturnsOkWithPayload()
    {
        var expected = new SpecWeightsResponse(
            Pyromancer: 100,
            Cryomancer: 101,
            Aquamancer: 102,
            Berserker: 103,
            Defender: 104,
            Revenant: 105,
            Avenger: 106,
            Crusader: 107,
            Protector: 108,
            Thunderlord: 109,
            Spiritguard: 110,
            Earthwarden: 111,
            Assassin: 112,
            Vindicator: 113,
            Apothecary: 114,
            Conjurer: 115,
            Sentinel: 116,
            Luminary: 117);

        var service = new Mock<ISpecWeightsService>();
        service.Setup(x => x.GetCombinedAsync(TestUuid, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        var controller = new ExperimentalSpecWeightsController(service.Object);

        var result = await controller.Get(TestUuid, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<SpecWeightsResponse>(ok.Value);
        Assert.Equal(expected, response);
    }

    [Fact]
    public async Task Get_WhenMissing_ReturnsNotFound()
    {
        var service = new Mock<ISpecWeightsService>();
        service.Setup(x => x.GetCombinedAsync(TestUuid, It.IsAny<CancellationToken>()))
            .ReturnsAsync((SpecWeightsResponse?)null);

        var controller = new ExperimentalSpecWeightsController(service.Object);

        var result = await controller.Get(TestUuid, CancellationToken.None);

        Assert.IsType<NotFoundResult>(result.Result);
    }
}
