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

        var weekly = new Mock<IAdjustmentAutoWeeklyService>();
        var controller = new AdjustController(service.Object, weekly.Object);
        var actionResult = await controller.AutoDaily(CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(actionResult.Result);
        Assert.Equal(expected, ok.Value);
    }

    [Fact]
    public async Task AutoWeekly_ReturnsOkWithServicePayload()
    {
        var uuid = Guid.Parse("a1b2c3d4-e5f6-7890-abcd-ef1234567890");
        var expected = new AdjustmentAutoWeeklyResponse(1, new Dictionary<Guid, AdjustmentAutoWeeklyPlayerBlock>
        {
            [uuid] = new AdjustmentAutoWeeklyPlayerBlock(
                "n",
                100,
                [new AdjustmentAutoWeeklySpecChange("Pyromancer", 98, 100, 2, 0)])
        });

        var daily = new Mock<IAdjustmentAutoDailyService>();
        var weekly = new Mock<IAdjustmentAutoWeeklyService>();
        weekly.Setup(x => x.ApplyAutoWeeklyAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        var controller = new AdjustController(daily.Object, weekly.Object);
        var actionResult = await controller.AutoWeekly(CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(actionResult.Result);
        Assert.Equal(expected, ok.Value);
    }
}
