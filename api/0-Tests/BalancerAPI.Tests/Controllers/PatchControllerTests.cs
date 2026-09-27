using BalancerAPI.Api.Controllers;
using BalancerAPI.Business.Services;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace BalancerAPI.Tests.Controllers;

public class PatchControllerTests
{
    [Fact]
    public async Task WeeklyAdjusts_ReturnsOkWithServicePayload()
    {
        var recordedAt = new DateTime(2026, 9, 26, 12, 0, 0, DateTimeKind.Utc);
        var cutoff = WeeklyAdjustPatchCutoff.Utc;
        var expected = new WeeklyAdjustPatchResponse(
            1,
            recordedAt,
            cutoff,
            [
                new WeeklyAdjustPatchEntry(
                    Guid.Parse("a1b2c3d4-e5f6-7890-abcd-ef1234567890"),
                    "n",
                    "Pyromancer",
                    -5,
                    0,
                    -5,
                    -5,
                    true,
                    20,
                    25,
                    80,
                    75,
                    -5,
                    100)
            ]);

        var weeklyPatch = new Mock<IWeeklyAdjustPatchService>();
        weeklyPatch.Setup(x => x.ApplyPatchAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);
        var controller = new PatchController(weeklyPatch.Object);
        var actionResult = await controller.WeeklyAdjusts(CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(actionResult.Result);
        Assert.Equal(expected, ok.Value);
    }
}
