using BalancerAPI.Api.Controllers;
using BalancerAPI.Business.Services;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace BalancerAPI.Tests.Controllers;

public class ExperimentalControllerTests
{
    private static readonly Guid TestUuid = Guid.Parse("a1b2c3d4-e5f6-7890-abcd-ef1234567890");
    private static readonly Guid U2 = Guid.Parse("b2c3d4e5-f6a7-8901-bcde-f12345678901");
    private static readonly Guid U3 = Guid.Parse("c3d4e5f6-a7b8-9012-cdef-123456789012");
    private static readonly Guid U4 = Guid.Parse("d4e5f6a7-b8c9-0123-def0-234567890123");

    private static ExperimentalController CreateController(
        ISpecWeightsService specWeights,
        IExperimentalBalanceService? balance = null)
    {
        var b = balance ?? Mock.Of<IExperimentalBalanceService>();
        return new ExperimentalController(specWeights, b);
    }

    [Fact]
    public async Task GetSpecWeights_WhenFound_ReturnsOkWithPayload()
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

        var controller = CreateController(service.Object);

        var result = await controller.GetSpecWeights(TestUuid, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<SpecWeightsResponse>(ok.Value);
        Assert.Equal(expected, response);
    }

    [Fact]
    public async Task GetSpecWeights_WhenMissing_ReturnsNotFound()
    {
        var service = new Mock<ISpecWeightsService>();
        service.Setup(x => x.GetCombinedAsync(TestUuid, It.IsAny<CancellationToken>()))
            .ReturnsAsync((SpecWeightsResponse?)null);

        var controller = CreateController(service.Object);

        var result = await controller.GetSpecWeights(TestUuid, CancellationToken.None);

        Assert.IsType<NotFoundResult>(result.Result);
    }

    [Fact]
    public async Task Balance_WhenValid_ReturnsOkWithPayload()
    {
        var meta = new ExperimentalBalanceMeta(
            1, 1.0, 0, 0, 0, 0, 0, 0, 0);
        var expected = new ExperimentalBalanceResponse(
            new Dictionary<string, string>
            {
                [TestUuid.ToString()] = "Pyromancer",
                [U2.ToString()] = "Cryomancer",
                [U3.ToString()] = "Aquamancer",
                [U4.ToString()] = "Berserker"
            },
            meta);

        var balance = new Mock<IExperimentalBalanceService>();
        balance.Setup(x => x.BalanceAsync(It.IsAny<ExperimentalBalanceRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ExperimentalBalanceServiceResult(true, expected, null));

        var specWeights = new Mock<ISpecWeightsService>();
        var controller = CreateController(specWeights.Object, balance.Object);

        var result = await controller.Balance(
            new ExperimentalBalanceRequest([TestUuid, U2, U3, U4]),
            CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<ExperimentalBalanceResponse>(ok.Value);
        Assert.Equal(expected, response);
    }

    [Fact]
    public async Task Balance_WhenServiceReturns400_ReturnsBadRequest()
    {
        var balance = new Mock<IExperimentalBalanceService>();
        balance.Setup(x => x.BalanceAsync(It.IsAny<ExperimentalBalanceRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ExperimentalBalanceServiceResult(
                false,
                null,
                new ExperimentalBalanceError(400, "players must not be empty.")));

        var specWeights = new Mock<ISpecWeightsService>();
        var controller = CreateController(specWeights.Object, balance.Object);

        var result = await controller.Balance(new ExperimentalBalanceRequest([]), CancellationToken.None);

        Assert.IsType<BadRequestObjectResult>(result.Result);
    }
}
