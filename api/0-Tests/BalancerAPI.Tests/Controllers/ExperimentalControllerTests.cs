using BalancerAPI.Api.Controllers;
using BalancerAPI.Business.Services;
using BalancerAPI.Data.Data;
using BalancerAPI.Domain.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace BalancerAPI.Tests.Controllers;

public class ExperimentalControllerTests
{
    private static readonly Guid TestUuid = Guid.Parse("a1b2c3d4-e5f6-7890-abcd-ef1234567890");
    private static readonly Guid U2 = Guid.Parse("b2c3d4e5-f6a7-8901-bcde-f12345678901");
    private static readonly Guid U3 = Guid.Parse("c3d4e5f6-a7b8-9012-cdef-123456789012");
    private static readonly Guid U4 = Guid.Parse("d4e5f6a7-b8c9-0123-def0-234567890123");
    private static readonly Guid TestBalanceId = Guid.Parse("e5f6a7b8-c9d0-1234-ef01-345678901234");

    private static ExperimentalController CreateController(
        ISpecWeightsService specWeights,
        IExperimentalBalanceService? balance = null,
        IExperimentalBalanceConfirmService? confirm = null,
        IExperimentalBalanceInputService? input = null,
        BalancerDbContext? dbContext = null)
    {
        var b = balance ?? Mock.Of<IExperimentalBalanceService>();
        var c = confirm ?? Mock.Of<IExperimentalBalanceConfirmService>();
        var i = input ?? Mock.Of<IExperimentalBalanceInputService>();
        var db = dbContext ?? CreateDbContext();
        return new ExperimentalController(specWeights, b, c, i, db);
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
        var now = new DateTime(2026, 3, 15, 20, 32, 0, DateTimeKind.Utc);
        var meta = new ExperimentalBalanceMeta(
            1,
            1.0,
            [
                new ExperimentalBalanceMetaStep("db.query.playerData", 0.2, 0.0),
                new ExperimentalBalanceMetaStep("algorithm.computeBalance", 0.6, 0.2),
                new ExperimentalBalanceMetaStep("response.serialize", 0.2, 0.8)
            ],
            9,
            now);
        var expected = new ExperimentalBalanceResponse(
            TestBalanceId,
            [
                new ExperimentalBalanceTeam(
                    200,
                    0,
                    12,
                    8.0,
                    [
                        new(TestUuid, "alpha", "Pyromancer", 100, 0, 7, 4.0),
                        new(U2, "beta", "Cryomancer", 100, 0, 5, 4.0)
                    ]),
                new ExperimentalBalanceTeam(
                    200,
                    0,
                    8,
                    4.0,
                    [
                        new(U3, "gamma", "Aquamancer", 100, 0, 3, 2.0),
                        new(U4, "delta", "Berserker", 100, 0, 5, 2.0)
                    ])
            ],
            meta);

        var balance = new Mock<IExperimentalBalanceService>();
        balance.Setup(x => x.BalanceAsync(It.IsAny<ExperimentalBalanceRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ExperimentalBalanceServiceResult(true, expected, null));

        var specWeights = new Mock<ISpecWeightsService>();
        await using var db = CreateDbContext();
        db.Names.AddRange(
            new PlayerName { Uuid = TestUuid, Name = "alpha", PreviousNames = [] },
            new PlayerName { Uuid = U2, Name = "beta", PreviousNames = [] },
            new PlayerName { Uuid = U3, Name = "gamma", PreviousNames = [] },
            new PlayerName { Uuid = U4, Name = "delta", PreviousNames = [] });
        await db.SaveChangesAsync();
        var controller = CreateController(specWeights.Object, balance.Object, dbContext: db);

        var result = await controller.Balance(
            new ExperimentalController.ExperimentalBalanceInputRequest([TestUuid.ToString(), U2.ToString(), U3.ToString(), U4.ToString()]),
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

        var result = await controller.Balance(new ExperimentalController.ExperimentalBalanceInputRequest([]), CancellationToken.None);

        Assert.IsType<BadRequestObjectResult>(result.Result);
    }

    [Fact]
    public async Task Balance_WhenPlayersAreNames_ResolvesToUuidsAndReturnsOk()
    {
        await using var db = CreateDbContext();
        db.Names.AddRange(
            new PlayerName { Uuid = TestUuid, Name = "alpha", PreviousNames = [] },
            new PlayerName { Uuid = U2, Name = "beta", PreviousNames = [] },
            new PlayerName { Uuid = U3, Name = "gamma", PreviousNames = [] },
            new PlayerName { Uuid = U4, Name = "delta", PreviousNames = [] });
        await db.SaveChangesAsync();

        var now = new DateTime(2026, 3, 15, 20, 32, 0, DateTimeKind.Utc);
        var meta = new ExperimentalBalanceMeta(
            1,
            1.0,
            [
                new ExperimentalBalanceMetaStep("db.query.playerData", 0.2, 0.0),
                new ExperimentalBalanceMetaStep("algorithm.computeBalance", 0.6, 0.2),
                new ExperimentalBalanceMetaStep("response.serialize", 0.2, 0.8)
            ],
            9,
            now);
        var expected = new ExperimentalBalanceResponse(
            TestBalanceId,
            [
                new ExperimentalBalanceTeam(
                    200,
                    0,
                    12,
                    8.0,
                    [
                        new(TestUuid, "alpha", "Pyromancer", 100, 0, 7, 4.0),
                        new(U2, "beta", "Cryomancer", 100, 0, 5, 4.0)
                    ]),
                new ExperimentalBalanceTeam(
                    200,
                    0,
                    8,
                    4.0,
                    [
                        new(U3, "gamma", "Aquamancer", 100, 0, 3, 2.0),
                        new(U4, "delta", "Berserker", 100, 0, 5, 2.0)
                    ])
            ],
            meta);

        var balance = new Mock<IExperimentalBalanceService>();
        balance.Setup(x => x.BalanceAsync(It.IsAny<ExperimentalBalanceRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ExperimentalBalanceServiceResult(true, expected, null));

        var specWeights = new Mock<ISpecWeightsService>();
        var controller = CreateController(specWeights.Object, balance.Object, dbContext: db);

        var result = await controller.Balance(
            new ExperimentalController.ExperimentalBalanceInputRequest(["alpha", "beta", "gamma", "delta"]),
            CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        _ = Assert.IsType<ExperimentalBalanceResponse>(ok.Value);
        balance.Verify(x => x.BalanceAsync(
            It.Is<ExperimentalBalanceRequest>(r => r.Players.SequenceEqual(new[] { TestUuid, U2, U3, U4 })),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Balance_WhenNameCannotBeResolved_ReturnsBadRequest()
    {
        await using var db = CreateDbContext();
        db.Names.Add(new PlayerName { Uuid = TestUuid, Name = "alpha", PreviousNames = [] });
        await db.SaveChangesAsync();

        var specWeights = new Mock<ISpecWeightsService>();
        var balance = new Mock<IExperimentalBalanceService>();
        var controller = CreateController(specWeights.Object, balance.Object, dbContext: db);

        var result = await controller.Balance(
            new ExperimentalController.ExperimentalBalanceInputRequest(["alpha", "does-not-exist"]),
            CancellationToken.None);

        Assert.IsType<BadRequestObjectResult>(result.Result);
        balance.Verify(x => x.BalanceAsync(It.IsAny<ExperimentalBalanceRequest>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ConfirmBalance_WhenServiceSucceeds_ReturnsOkWithBalanceId()
    {
        var confirm = new Mock<IExperimentalBalanceConfirmService>();
        confirm.Setup(x => x.ConfirmAsync(TestBalanceId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ExperimentalBalanceConfirmServiceResult(true, 200, null));

        var specWeights = new Mock<ISpecWeightsService>();
        var controller = CreateController(specWeights.Object, confirm: confirm.Object);

        var result = await controller.ConfirmBalance(TestBalanceId, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var body = Assert.IsType<ExperimentalBalanceConfirmResponse>(ok.Value);
        Assert.Equal(TestBalanceId, body.BalanceId);
    }

    [Fact]
    public async Task InputBalance_WhenServiceSucceeds_ReturnsOkWithBalanceId()
    {
        var input = new Mock<IExperimentalBalanceInputService>();
        input.Setup(x => x.InputAsync(TestBalanceId, It.IsAny<ExperimentalBalanceInputBody>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ExperimentalBalanceInputServiceResult(true, 200, null));

        var specWeights = new Mock<ISpecWeightsService>();
        var controller = CreateController(specWeights.Object, input: input.Object);

        var body = new ExperimentalBalanceInputBody([], [], "aaaaaaaaaaaaaaaaaaaaaaaa");
        var result = await controller.InputBalance(TestBalanceId, body, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<ExperimentalBalanceInputResponse>(ok.Value);
        Assert.Equal(TestBalanceId, response.BalanceId);
    }

    [Fact]
    public async Task InputBalance_WhenBodyNull_ReturnsBadRequest()
    {
        var specWeights = new Mock<ISpecWeightsService>();
        var controller = CreateController(specWeights.Object);

        var result = await controller.InputBalance(TestBalanceId, null, CancellationToken.None);

        Assert.IsType<BadRequestObjectResult>(result.Result);
    }

    private static BalancerDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<BalancerDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new BalancerDbContext(options);
    }
}
