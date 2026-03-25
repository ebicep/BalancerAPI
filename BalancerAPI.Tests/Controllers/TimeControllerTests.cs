using BalancerAPI.Controllers;
using BalancerAPI.Services;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace BalancerAPI.Tests.Controllers;

public class TimeControllerTests
{
    [Fact]
    public async Task NewDay_ReturnsOkWithNewDay()
    {
        var service = new Mock<ITimeService>();
        service.Setup(x => x.CreateNewDayAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(3);

        var controller = new TimeController(service.Object);

        var result = await controller.NewDay(CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<NewDayResponse>(ok.Value);
        Assert.Equal(3, response.NewDay);
    }

    [Fact]
    public async Task NewWeek_ReturnsOkWithNewWeek()
    {
        var service = new Mock<ITimeService>();
        service.Setup(x => x.CreateNewWeekAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(7);

        var controller = new TimeController(service.Object);

        var result = await controller.NewWeek(CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<NewWeekResponse>(ok.Value);
        Assert.Equal(7, response.NewWeek);
    }
}
