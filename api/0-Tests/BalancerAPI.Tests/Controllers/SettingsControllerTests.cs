using BalancerAPI.Api.Controllers;
using BalancerAPI.Business.Services;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace BalancerAPI.Tests.Controllers;

public class SettingsControllerTests
{
    [Fact]
    public async Task GetAll_ReturnsOkWithDataMap()
    {
        var service = new Mock<ISettingsService>();
        IReadOnlyDictionary<string, decimal> data = new Dictionary<string, decimal>
        {
            ["max_flat_team_diff"] = 10m,
            ["max_weight_diff"] = 20m
        };
        service.Setup(x => x.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(data);

        var controller = new SettingsController(service.Object);

        var result = await controller.GetAll(CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<SettingsResponse>(ok.Value);
        Assert.Equal(10m, response.Data["max_flat_team_diff"]);
        Assert.Equal(20m, response.Data["max_weight_diff"]);
    }

    [Fact]
    public async Task GetByKey_WhenMissing_ReturnsNotFound()
    {
        var service = new Mock<ISettingsService>();
        service.Setup(x => x.GetByKeyAsync("max_flat_team_diff", It.IsAny<CancellationToken>()))
            .ReturnsAsync((SettingEntry?)null);

        var controller = new SettingsController(service.Object);

        var result = await controller.GetByKey("max_flat_team_diff", CancellationToken.None);

        Assert.IsType<NotFoundResult>(result.Result);
    }

    [Fact]
    public async Task GetByKey_WhenPresent_ReturnsOkWithSetting()
    {
        var service = new Mock<ISettingsService>();
        service.Setup(x => x.GetByKeyAsync("max_flat_team_diff", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SettingEntry("max_flat_team_diff", 10m, "Max Flat Team Diff"));

        var controller = new SettingsController(service.Object);

        var result = await controller.GetByKey("max_flat_team_diff", CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<SettingResponse>(ok.Value);
        Assert.Equal("max_flat_team_diff", response.Data.Key);
        Assert.Equal(10m, response.Data.Value);
        Assert.Equal("Max Flat Team Diff", response.Data.DisplayName);
    }

    [Fact]
    public async Task Upsert_ReturnsOkWithUpdatedSetting()
    {
        var service = new Mock<ISettingsService>();
        service.Setup(x => x.UpsertAsync("max_flat_team_diff", 11m, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SettingEntry("max_flat_team_diff", 11m, "Max Flat Team Diff"));

        var controller = new SettingsController(service.Object);

        var result = await controller.Upsert(
            "max_flat_team_diff",
            new UpdateSettingRequest(11m),
            CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<SettingResponse>(ok.Value);
        Assert.Equal("max_flat_team_diff", response.Data.Key);
        Assert.Equal(11m, response.Data.Value);
    }
}
