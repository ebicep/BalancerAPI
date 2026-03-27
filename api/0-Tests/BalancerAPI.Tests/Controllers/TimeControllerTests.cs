using BalancerAPI.Api.Controllers;
using BalancerAPI.Business.Services;
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

    [Fact]
    public async Task NewSeason_ReturnsOkWithNewSeason()
    {
        var service = new Mock<ITimeService>();
        var timestamp = new DateTime(2026, 3, 27, 12, 0, 0, DateTimeKind.Utc);
        service.Setup(x => x.CreateNewSeasonAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync((5, timestamp));

        var controller = new TimeController(service.Object);

        var result = await controller.NewSeason(CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<NewSeasonResponse>(ok.Value);
        Assert.Equal(5, response.Season);
        Assert.Equal(timestamp, response.Timestamp);
    }

    [Fact]
    public async Task GetSeason_WhenMissing_ReturnsNotFound()
    {
        var service = new Mock<ITimeService>();
        service.Setup(x => x.GetLatestSeasonAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(((int Season, DateTime Timestamp)?)null);

        var controller = new TimeController(service.Object);

        var result = await controller.GetSeason(CancellationToken.None);

        Assert.IsType<NotFoundResult>(result.Result);
    }

    [Fact]
    public async Task GetSeason_WhenPresent_ReturnsOkWithLatestSeason()
    {
        var service = new Mock<ITimeService>();
        var timestamp = new DateTime(2026, 3, 27, 12, 0, 0, DateTimeKind.Utc);
        service.Setup(x => x.GetLatestSeasonAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(((int Season, DateTime Timestamp)?)(9, timestamp));

        var controller = new TimeController(service.Object);

        var result = await controller.GetSeason(CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<LatestSeasonResponse>(ok.Value);
        Assert.Equal(9, response.Season);
        Assert.Equal(timestamp, response.Timestamp);
    }
}