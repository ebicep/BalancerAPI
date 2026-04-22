using BalancerAPI.Api.Controllers;
using BalancerAPI.Business.Services;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace BalancerAPI.Tests.Controllers;

public class AdjustControllerTests
{
    [Fact]
    public async Task AutoDaily_ReturnsOkWithServicePayload()
    {
        var expected = new AdjustmentAutoDailyResponse(1,
        [
            new AdjustmentAutoDailyAdjustedEntry(
                Guid.Parse("a1b2c3d4-e5f6-7890-abcd-ef1234567890"),
                "n",
                10,
                11)
        ]);

        var service = new Mock<IAdjustmentAutoDailyService>();
        service.Setup(x => x.ApplyAutoDailyAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        var manual = new Mock<IManualWeightAdjustmentService>();
        var controller = new AdjustController(service.Object, manual.Object);
        var actionResult = await controller.AutoDaily(CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(actionResult.Result);
        Assert.Equal(expected, ok.Value);
    }

}
