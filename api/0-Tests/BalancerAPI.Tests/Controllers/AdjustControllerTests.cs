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
        var batchDate = new DateTime(2026, 5, 18, 14, 30, 0, DateTimeKind.Utc);
        var expected = new AdjustmentAutoDailyResponse(1,
        [
            new AdjustmentAutoDailyAdjustedEntry(
                Guid.Parse("a1b2c3d4-e5f6-7890-abcd-ef1234567890"),
                "n",
                10,
                11,
                3,
                2)
        ],
        batchDate);

        var service = new Mock<IAdjustmentAutoDailyService>();
        service.Setup(x => x.ApplyAutoDailyAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        var manual = new Mock<IManualWeightAdjustmentService>();
        var controller = new AdjustController(service.Object, manual.Object);
        var actionResult = await controller.AutoDaily(CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(actionResult.Result);
        Assert.Equal(expected, ok.Value);
    }

    [Fact]
    public async Task UndoAutoDaily_ReturnsOkWithServicePayload()
    {
        var batchDate = new DateTime(2026, 5, 18, 14, 30, 0, DateTimeKind.Utc);
        var expected = new AdjustmentAutoDailyResponse(1,
        [
            new AdjustmentAutoDailyAdjustedEntry(
                Guid.Parse("a1b2c3d4-e5f6-7890-abcd-ef1234567890"),
                "n",
                10,
                11,
                3,
                2)
        ],
        batchDate);

        var service = new Mock<IAdjustmentAutoDailyService>();
        service.Setup(x => x.UndoAutoDailyAsync(
                It.IsAny<AdjustmentAutoDailyResponse>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(AdjustmentAutoDailyUndoResult.Ok(expected));

        var manual = new Mock<IManualWeightAdjustmentService>();
        var controller = new AdjustController(service.Object, manual.Object);
        var actionResult = await controller.UndoAutoDaily(expected, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(actionResult.Result);
        Assert.Equal(expected, ok.Value);
    }

    [Fact]
    public async Task UndoAutoDaily_WhenBodyNull_Returns400Problem()
    {
        var service = new Mock<IAdjustmentAutoDailyService>();
        var manual = new Mock<IManualWeightAdjustmentService>();
        var controller = new AdjustController(service.Object, manual.Object);

        var actionResult = await controller.UndoAutoDaily(null, CancellationToken.None);

        var problem = Assert.IsType<ObjectResult>(actionResult.Result);
        Assert.Equal(400, problem.StatusCode);
    }

    [Fact]
    public async Task UndoAutoDaily_WhenServiceFails_ReturnsProblem()
    {
        var service = new Mock<IAdjustmentAutoDailyService>();
        service.Setup(x => x.UndoAutoDailyAsync(
                It.IsAny<AdjustmentAutoDailyResponse>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(AdjustmentAutoDailyUndoResult.Fail(409, "date must match the latest auto-daily adjustment batch."));

        var manual = new Mock<IManualWeightAdjustmentService>();
        var controller = new AdjustController(service.Object, manual.Object);
        var body = new AdjustmentAutoDailyResponse(1, [], DateTime.UtcNow);
        var actionResult = await controller.UndoAutoDaily(body, CancellationToken.None);

        var problem = Assert.IsType<ObjectResult>(actionResult.Result);
        Assert.Equal(409, problem.StatusCode);
    }

    [Fact]
    public async Task PatchBase_ReturnsOkWithServicePayload()
    {
        var expected = new ManualBaseAdjustResponse(
            Guid.Parse("a1b2c3d4-e5f6-7890-abcd-ef1234567890"),
            "TestPlayer",
            100,
            105,
            5,
            0);

        var service = new Mock<IAdjustmentAutoDailyService>();
        var manual = new Mock<IManualWeightAdjustmentService>();
        manual.Setup(x => x.PatchBaseAsync(
                It.IsAny<string>(),
                It.IsAny<ManualAdjustBaseRequest>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(ManualWeightAdjustServiceResult<ManualBaseAdjustResponse>.Ok(expected));

        var controller = new AdjustController(service.Object, manual.Object);
        var actionResult = await controller.PatchBase("TestPlayer", new ManualAdjustBaseRequest(5), CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(actionResult.Result);
        Assert.Equal(expected, ok.Value);
    }

    [Fact]
    public async Task PatchSpec_ReturnsOkWithServicePayload()
    {
        var expected = new ManualSpecAdjustResponse(
            Guid.Parse("a1b2c3d4-e5f6-7890-abcd-ef1234567890"),
            "TestPlayer",
            "Pyromancer",
            10,
            13,
            100,
            90,
            87);

        var service = new Mock<IAdjustmentAutoDailyService>();
        var manual = new Mock<IManualWeightAdjustmentService>();
        manual.Setup(x => x.PatchSpecAsync(
                It.IsAny<string>(),
                It.IsAny<ManualAdjustSpecRequest>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(ManualWeightAdjustServiceResult<ManualSpecAdjustResponse>.Ok(expected));

        var controller = new AdjustController(service.Object, manual.Object);
        var actionResult = await controller.PatchSpec("TestPlayer", new ManualAdjustSpecRequest(3, "Pyromancer"), CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(actionResult.Result);
        Assert.Equal(expected, ok.Value);
    }
}
