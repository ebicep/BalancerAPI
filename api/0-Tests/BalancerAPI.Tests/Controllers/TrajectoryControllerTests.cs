using BalancerAPI.Api.Controllers;
using BalancerAPI.Business.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace BalancerAPI.Tests.Controllers;

public class TrajectoryControllerTests
{
    private static readonly Guid U1 = Guid.Parse("a1b2c3d4-e5f6-7890-abcd-ef1234567890");

    [Fact]
    public async Task List_ReturnsOkWithTopLevelArray()
    {
        IReadOnlyList<PlayerTrajectoryEntry> expected =
        [
            new PlayerTrajectoryEntry(U1, "Amy", 3)
        ];
        var service = new Mock<ITrajectoryService>();
        service.Setup(x => x.ListAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        var controller = new TrajectoryController(service.Object);

        var result = await controller.List(CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var list = Assert.IsAssignableFrom<IReadOnlyList<PlayerTrajectoryEntry>>(ok.Value);
        Assert.Single(list);
        Assert.Equal("Amy", list[0].Name);
        Assert.Equal(3, list[0].Trajectory);
    }

    [Fact]
    public async Task Set_WhenSuccess_ReturnsOkWithEntry()
    {
        var expected = new PlayerTrajectoryEntry(U1, "Amy", 2);
        var service = new Mock<ITrajectoryService>();
        service.Setup(x => x.SetAsync("Amy", It.IsAny<SetTrajectoryRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(TrajectoryServiceResult<PlayerTrajectoryEntry>.Ok(expected));

        var controller = new TrajectoryController(service.Object);

        var result = await controller.Set("Amy", new SetTrajectoryRequest(2), CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var entry = Assert.IsType<PlayerTrajectoryEntry>(ok.Value);
        Assert.Equal(U1, entry.Uuid);
        Assert.Equal(2, entry.Trajectory);
    }

    [Fact]
    public async Task Set_WhenBodyMissing_ReturnsBadRequestProblem()
    {
        var service = new Mock<ITrajectoryService>();
        var controller = new TrajectoryController(service.Object);

        var result = await controller.Set("Amy", null, CancellationToken.None);

        var obj = Assert.IsType<ObjectResult>(result.Result);
        Assert.Equal(StatusCodes.Status400BadRequest, obj.StatusCode);
        var pd = Assert.IsType<ProblemDetails>(obj.Value);
        Assert.Equal("Request body is required.", pd.Detail);
    }

    [Fact]
    public async Task Set_WhenServiceFails_ReturnsProblemWithStatus()
    {
        var service = new Mock<ITrajectoryService>();
        service.Setup(x => x.SetAsync("nobody", It.IsAny<SetTrajectoryRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(TrajectoryServiceResult<PlayerTrajectoryEntry>.Fail(404, "No matching UUID found in names table for: nobody."));

        var controller = new TrajectoryController(service.Object);

        var result = await controller.Set("nobody", new SetTrajectoryRequest(1), CancellationToken.None);

        var obj = Assert.IsType<ObjectResult>(result.Result);
        Assert.Equal(StatusCodes.Status404NotFound, obj.StatusCode);
        var pd = Assert.IsType<ProblemDetails>(obj.Value);
        Assert.Contains("nobody", pd.Detail);
    }
}
