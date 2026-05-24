using System.Collections.Generic;
using System.Text.Json;
using BalancerAPI.Api.Controllers;
using BalancerAPI.Business.Services;
using BalancerAPI.Data.Data;
using BalancerAPI.Domain.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
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
        ISpecWeightLeaderboardService? leaderboard = null,
        IExperimentalBalanceService? balance = null,
        IExperimentalBalanceConfirmService? confirm = null,
        IExperimentalBalanceInputService? input = null,
        IExperimentalSpecLogsService? specLogs = null,
        IExperimentalSpecBanService? specBans = null,
        BalancerDbContext? dbContext = null)
    {
        var lb = leaderboard ?? Mock.Of<ISpecWeightLeaderboardService>();
        var b = balance ?? Mock.Of<IExperimentalBalanceService>();
        var c = confirm ?? Mock.Of<IExperimentalBalanceConfirmService>();
        var i = input ?? Mock.Of<IExperimentalBalanceInputService>();
        var sl = specLogs ?? Mock.Of<IExperimentalSpecLogsService>();
        var sb = specBans ?? Mock.Of<IExperimentalSpecBanService>();
        var db = dbContext ?? CreateDbContext();
        return new ExperimentalController(specWeights, lb, b, c, i, sl, sb, db);
    }

    [Fact]
    public async Task GetSpecWeightLeaderboard_WhenValid_ReturnsOkAndForwardsQueryParams()
    {
        var expected = new Dictionary<string, IReadOnlyList<SpecWeightLeaderboardEntry>>
        {
            ["pyromancer"] =
            [
                new SpecWeightLeaderboardEntry
                {
                    Uuid = TestUuid.ToString(),
                    Name = "alpha",
                    SpecWeight = 142
                }
            ]
        };

        var leaderboard = new Mock<ISpecWeightLeaderboardService>();
        leaderboard.Setup(x => x.GetLeaderboardAsync(2, 5, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        var controller = CreateController(Mock.Of<ISpecWeightsService>(), leaderboard.Object);

        var result = await controller.GetSpecWeightLeaderboard(2, 5, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<Dictionary<string, IReadOnlyList<SpecWeightLeaderboardEntry>>>(ok.Value);
        Assert.Same(expected, response);
        leaderboard.Verify(x => x.GetLeaderboardAsync(2, 5, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Theory]
    [InlineData(0, 10, "page must be greater than or equal to 1.")]
    [InlineData(1, 0, "pageSize must be between 1 and 100.")]
    [InlineData(1, 101, "pageSize must be between 1 and 100.")]
    public async Task GetSpecWeightLeaderboard_WhenInvalidQuery_ReturnsBadRequest(
        int page,
        int pageSize,
        string expectedDetail)
    {
        var leaderboard = new Mock<ISpecWeightLeaderboardService>();
        var controller = CreateController(Mock.Of<ISpecWeightsService>(), leaderboard.Object);

        var result = await controller.GetSpecWeightLeaderboard(page, pageSize, CancellationToken.None);

        AssertProblem(result.Result!, StatusCodes.Status400BadRequest, expectedDetail);
        leaderboard.Verify(
            x => x.GetLeaderboardAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()),
            Times.Never);
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

        var result = await controller.GetSpecWeights(TestUuid.ToString(), CancellationToken.None);

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

        var result = await controller.GetSpecWeights(TestUuid.ToString(), CancellationToken.None);

        AssertProblem(
            result.Result!,
            StatusCodes.Status404NotFound,
            "The requested resource was not found.");
    }

    [Fact]
    public async Task GetSpecWeights_WhenNameResolves_ReturnsOkWithPayload()
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

        await using var db = CreateDbContext();
        db.Names.Add(new PlayerName { Uuid = TestUuid, Name = "alpha", PreviousNames = [] });
        await db.SaveChangesAsync();

        var service = new Mock<ISpecWeightsService>();
        service.Setup(x => x.GetCombinedAsync(TestUuid, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        var controller = CreateController(service.Object, dbContext: db);

        var result = await controller.GetSpecWeights("alpha", CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<SpecWeightsResponse>(ok.Value);
        Assert.Equal(expected, response);
        service.Verify(x => x.GetCombinedAsync(TestUuid, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetSpecWeights_WhenNameNotFound_ReturnsBadRequest()
    {
        var service = new Mock<ISpecWeightsService>();
        var controller = CreateController(service.Object);

        var result = await controller.GetSpecWeights("does-not-exist", CancellationToken.None);

        var pd = AssertProblem(result.Result!, StatusCodes.Status400BadRequest);
        Assert.Contains("does-not-exist", pd.Detail, StringComparison.Ordinal);
        service.Verify(x => x.GetCombinedAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task GetSpecWeights_WhenNameAmbiguous_ReturnsConflict()
    {
        await using var db = CreateDbContext();
        db.Names.AddRange(
            new PlayerName { Uuid = TestUuid, Name = "alpha", PreviousNames = [] },
            new PlayerName { Uuid = U2, Name = "alpha", PreviousNames = [] });
        await db.SaveChangesAsync();

        var service = new Mock<ISpecWeightsService>();
        var controller = CreateController(service.Object, dbContext: db);

        var result = await controller.GetSpecWeights("alpha", CancellationToken.None);

        AssertProblem(
            result.Result!,
            StatusCodes.Status409Conflict,
            "One or more player names are ambiguous in names table: alpha.");
        service.Verify(x => x.GetCombinedAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task GetSpecWeights_WhenUuidStringWithoutNamesRow_CallsServiceWithUuid()
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

        var result = await controller.GetSpecWeights(TestUuid.ToString(), CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        Assert.IsType<SpecWeightsResponse>(ok.Value);
        service.Verify(x => x.GetCombinedAsync(TestUuid, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetSpecBans_WhenFound_ReturnsOkWithBans()
    {
        var expected = new ExperimentalSpecBansResponse(["Pyromancer", "Cryomancer"]);
        var specBans = new Mock<IExperimentalSpecBanService>();
        specBans.Setup(x => x.GetBansAsync(TestUuid, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ExperimentalSpecBanServiceResult(true, 200, null, expected));

        var controller = CreateController(Mock.Of<ISpecWeightsService>(), specBans: specBans.Object);

        var result = await controller.GetSpecBans(TestUuid.ToString(), CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<ExperimentalSpecBansResponse>(ok.Value);
        Assert.Equal(expected.Bans, response.Bans);
    }

    [Fact]
    public async Task BanSpec_WhenInvalidSpec_ReturnsBadRequest()
    {
        var specBans = new Mock<IExperimentalSpecBanService>();
        var controller = CreateController(Mock.Of<ISpecWeightsService>(), specBans: specBans.Object);

        var result = await controller.BanSpec(
            TestUuid.ToString(),
            new ExperimentalSpecBanRequest("nope"),
            CancellationToken.None);

        AssertProblem(result.Result!, StatusCodes.Status400BadRequest, "Unknown or missing spec.");
        specBans.Verify(
            x => x.SetBanAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task BanSpec_WhenValid_CallsServiceAndReturnsOk()
    {
        var expected = new ExperimentalSpecBansResponse(["Pyromancer"]);
        var specBans = new Mock<IExperimentalSpecBanService>();
        specBans.Setup(x => x.SetBanAsync(TestUuid, "Pyromancer", true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ExperimentalSpecBanServiceResult(true, 200, null, expected));

        var controller = CreateController(Mock.Of<ISpecWeightsService>(), specBans: specBans.Object);

        var result = await controller.BanSpec(
            TestUuid.ToString(),
            new ExperimentalSpecBanRequest("pyromancer"),
            CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<ExperimentalSpecBansResponse>(ok.Value);
        Assert.Equal(expected.Bans, response.Bans);
        specBans.Verify(x => x.SetBanAsync(TestUuid, "Pyromancer", true, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UnbanSpec_WhenValid_CallsServiceWithBannedFalse()
    {
        var expected = new ExperimentalSpecBansResponse([]);
        var specBans = new Mock<IExperimentalSpecBanService>();
        specBans.Setup(x => x.SetBanAsync(TestUuid, "Pyromancer", false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ExperimentalSpecBanServiceResult(true, 200, null, expected));

        var controller = CreateController(Mock.Of<ISpecWeightsService>(), specBans: specBans.Object);

        var result = await controller.UnbanSpec(
            TestUuid.ToString(),
            new ExperimentalSpecBanRequest("Pyromancer"),
            CancellationToken.None);

        Assert.IsType<OkObjectResult>(result.Result);
        specBans.Verify(x => x.SetBanAsync(TestUuid, "Pyromancer", false, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetSpecBans_WhenNameResolves_ReturnsOk()
    {
        await using var db = CreateDbContext();
        db.Names.Add(new PlayerName { Uuid = TestUuid, Name = "alpha", PreviousNames = [] });
        await db.SaveChangesAsync();

        var expected = new ExperimentalSpecBansResponse(["Berserker"]);
        var specBans = new Mock<IExperimentalSpecBanService>();
        specBans.Setup(x => x.GetBansAsync(TestUuid, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ExperimentalSpecBanServiceResult(true, 200, null, expected));

        var controller = CreateController(Mock.Of<ISpecWeightsService>(), specBans: specBans.Object, dbContext: db);

        var result = await controller.GetSpecBans("alpha", CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<ExperimentalSpecBansResponse>(ok.Value);
        Assert.Equal(expected.Bans, response.Bans);
    }

    [Fact]
    public async Task GetDaily_WhenNameNotFound_ReturnsBadRequest()
    {
        var specWeights = new Mock<ISpecWeightsService>();
        var controller = CreateController(specWeights.Object);

        var result = await controller.GetDaily("does-not-exist", null, CancellationToken.None);

        var pd = AssertProblem(result.Result!, StatusCodes.Status400BadRequest);
        Assert.Contains("does-not-exist", pd.Detail, StringComparison.Ordinal);
    }

    [Fact]
    public async Task GetDaily_WhenNameEmpty_ReturnsBadRequest()
    {
        var specWeights = new Mock<ISpecWeightsService>();
        var controller = CreateController(specWeights.Object);

        var result = await controller.GetDaily("   ", null, CancellationToken.None);

        AssertProblem(result.Result!, StatusCodes.Status400BadRequest, "name must not be empty.");
    }

    [Fact]
    public async Task GetDaily_WhenNameAmbiguous_ReturnsConflict()
    {
        await using var db = CreateDbContext();
        db.Names.AddRange(
            new PlayerName { Uuid = TestUuid, Name = "alpha", PreviousNames = [] },
            new PlayerName { Uuid = U2, Name = "alpha", PreviousNames = [] });
        await db.SaveChangesAsync();

        var specWeights = new Mock<ISpecWeightsService>();
        var controller = CreateController(specWeights.Object, dbContext: db);

        var result = await controller.GetDaily("alpha", null, CancellationToken.None);

        AssertProblem(
            result.Result!,
            StatusCodes.Status409Conflict,
            "One or more player names are ambiguous in names table: alpha.");
    }

    [Fact]
    public async Task GetDaily_WhenNoDailyRow_ReturnsOkWithZeros()
    {
        await using var db = CreateDbContext();
        db.Names.Add(new PlayerName { Uuid = TestUuid, Name = "alpha", PreviousNames = [] });
        await db.SaveChangesAsync();

        var specWeights = new Mock<ISpecWeightsService>();
        var controller = CreateController(specWeights.Object, dbContext: db);

        var result = await controller.GetDaily("alpha", null, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var body = Assert.IsType<ExperimentalController.ExperimentalDailyStatsResponse>(ok.Value);
        Assert.Equal(0, body.Wins);
        Assert.Equal(0, body.Losses);
        Assert.Equal(0, body.Kills);
        Assert.Equal(0, body.Deaths);
    }

    [Fact]
    public async Task GetDaily_WhenRowExists_ReturnsOkWithStats()
    {
        await using var db = CreateDbContextWithDailyStatsTable();
        db.Names.Add(new PlayerName { Uuid = TestUuid, Name = "alpha", PreviousNames = [] });
        db.ExperimentalDailyStats.Add(new ExperimentalDailyStats
        {
            Uuid = TestUuid,
            Wins = 3,
            Losses = 1,
            Kills = 10,
            Deaths = 4
        });
        await db.SaveChangesAsync();

        var specWeights = new Mock<ISpecWeightsService>();
        var controller = CreateController(specWeights.Object, dbContext: db);

        var result = await controller.GetDaily("alpha", null, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var body = Assert.IsType<ExperimentalController.ExperimentalDailyStatsResponse>(ok.Value);
        Assert.Equal(3, body.Wins);
        Assert.Equal(1, body.Losses);
        Assert.Equal(10, body.Kills);
        Assert.Equal(4, body.Deaths);
    }

    [Fact]
    public async Task GetDaily_WhenIdGiven_UsesHistoricalStats()
    {
        const int dayId = 42;
        await using var db = CreateDbContextWithDailyStatsTable();
        db.Names.Add(new PlayerName { Uuid = TestUuid, Name = "alpha", PreviousNames = [] });
        db.TimeDays.Add(new TimeDay { Id = dayId, Timestamp = DateTime.UtcNow });
        db.ExperimentalDailyStatsDay.Add(new ExperimentalDailyStatsDay
        {
            DayStartDate = dayId,
            Uuid = TestUuid,
            Wins = 5,
            Losses = 2,
            Kills = 20,
            Deaths = 8
        });
        await db.SaveChangesAsync();

        var specWeights = new Mock<ISpecWeightsService>();
        var controller = CreateController(specWeights.Object, dbContext: db);

        var result = await controller.GetDaily("alpha", dayId, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var body = Assert.IsType<ExperimentalController.ExperimentalDailyStatsResponse>(ok.Value);
        Assert.Equal(5, body.Wins);
        Assert.Equal(2, body.Losses);
        Assert.Equal(20, body.Kills);
        Assert.Equal(8, body.Deaths);
    }

    [Fact]
    public async Task GetDaily_WhenIdUnknown_Returns404()
    {
        await using var db = CreateDbContextWithDailyStatsTable();
        db.Names.Add(new PlayerName { Uuid = TestUuid, Name = "alpha", PreviousNames = [] });
        await db.SaveChangesAsync();

        var specWeights = new Mock<ISpecWeightsService>();
        var controller = CreateController(specWeights.Object, dbContext: db);

        var result = await controller.GetDaily("alpha", 99999, CancellationToken.None);

        AssertProblem(
            result.Result!,
            StatusCodes.Status404NotFound,
            "No day found with id 99999.");
    }

    [Fact]
    public async Task GetDailyExperimentalAll_WhenNameNotFound_ReturnsBadRequest()
    {
        var specWeights = new Mock<ISpecWeightsService>();
        var controller = CreateController(specWeights.Object);

        var result = await controller.GetDailyExperimentalAll("does-not-exist", null, CancellationToken.None);

        var pd = AssertProblem(result.Result!, StatusCodes.Status400BadRequest);
        Assert.Contains("does-not-exist", pd.Detail, StringComparison.Ordinal);
    }

    [Fact]
    public async Task GetDailyExperimentalAll_WhenNameEmpty_ReturnsBadRequest()
    {
        var specWeights = new Mock<ISpecWeightsService>();
        var controller = CreateController(specWeights.Object);

        var result = await controller.GetDailyExperimentalAll("   ", null, CancellationToken.None);

        AssertProblem(result.Result!, StatusCodes.Status400BadRequest, "name must not be empty.");
    }

    [Fact]
    public async Task GetDailyExperimentalAll_WhenNameAmbiguous_ReturnsConflict()
    {
        await using var db = CreateDbContextWithDailyStatsTable();
        db.Names.AddRange(
            new PlayerName { Uuid = TestUuid, Name = "alpha", PreviousNames = [] },
            new PlayerName { Uuid = U2, Name = "alpha", PreviousNames = [] });
        await db.SaveChangesAsync();

        var specWeights = new Mock<ISpecWeightsService>();
        var controller = CreateController(specWeights.Object, dbContext: db);

        var result = await controller.GetDailyExperimentalAll("alpha", null, CancellationToken.None);

        AssertProblem(
            result.Result!,
            StatusCodes.Status409Conflict,
            "One or more player names are ambiguous in names table: alpha.");
    }

    [Fact]
    public async Task GetDailyExperimentalAll_WhenNoRow_ReturnsOkWithZeros()
    {
        await using var db = CreateDbContextWithDailyStatsTable();
        db.Names.Add(new PlayerName { Uuid = TestUuid, Name = "alpha", PreviousNames = [] });
        await db.SaveChangesAsync();

        var specWeights = new Mock<ISpecWeightsService>();
        var controller = CreateController(specWeights.Object, dbContext: db);

        var result = await controller.GetDailyExperimentalAll("alpha", null, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var body = Assert.IsType<ExperimentalController.ExperimentalDailyAllSpecsResponse>(ok.Value);
        Assert.Equal(18, body.Specs.Count);
        Assert.All(body.Specs, entry =>
        {
            Assert.Equal(0, entry.Wins);
            Assert.Equal(0, entry.Losses);
            Assert.Equal(0, entry.Kills);
            Assert.Equal(0, entry.Deaths);
        });
        Assert.Equal("Total", body.Total.Spec);
        Assert.Equal(0, body.Total.Wins);
        Assert.Equal(0, body.Total.Losses);
        Assert.Equal(0, body.Total.Kills);
        Assert.Equal(0, body.Total.Deaths);
    }

    [Fact]
    public async Task GetDailyExperimentalAll_WhenCurrentRowExists_ReturnsOkWithStats()
    {
        await using var db = CreateDbContextWithDailyStatsTable();
        db.Names.Add(new PlayerName { Uuid = TestUuid, Name = "alpha", PreviousNames = [] });
        db.ExperimentalSpecsWlCurrentDay.Add(new ExperimentalSpecsWlCurrentDay
        {
            Uuid = TestUuid,
            PyromancerWins = 3,
            PyromancerLosses = 1,
            PyromancerKills = 10,
            PyromancerDeaths = 4
        });
        await db.SaveChangesAsync();

        var specWeights = new Mock<ISpecWeightsService>();
        var controller = CreateController(specWeights.Object, dbContext: db);

        var result = await controller.GetDailyExperimentalAll("alpha", null, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var body = Assert.IsType<ExperimentalController.ExperimentalDailyAllSpecsResponse>(ok.Value);
        var pyro = Assert.Single(body.Specs, x => x.Spec == "Pyromancer");
        Assert.Equal(3, pyro.Wins);
        Assert.Equal(1, pyro.Losses);
        Assert.Equal(10, pyro.Kills);
        Assert.Equal(4, pyro.Deaths);
        Assert.Equal(3, body.Total.Wins);
        Assert.Equal(1, body.Total.Losses);
        Assert.Equal(10, body.Total.Kills);
        Assert.Equal(4, body.Total.Deaths);
    }

    [Fact]
    public async Task GetDailyExperimentalAll_WhenIdGiven_UsesHistoricalStats()
    {
        const int dayId = 42;
        await using var db = CreateDbContextWithDailyStatsTable();
        db.Names.Add(new PlayerName { Uuid = TestUuid, Name = "alpha", PreviousNames = [] });
        db.TimeDays.Add(new TimeDay { Id = dayId, Timestamp = DateTime.UtcNow });
        db.ExperimentalSpecsWlDay.Add(new ExperimentalSpecsWlDay
        {
            DayStartDate = dayId,
            Uuid = TestUuid,
            PyromancerWins = 5,
            PyromancerLosses = 2,
            PyromancerKills = 20,
            PyromancerDeaths = 8
        });
        await db.SaveChangesAsync();

        var specWeights = new Mock<ISpecWeightsService>();
        var controller = CreateController(specWeights.Object, dbContext: db);

        var result = await controller.GetDailyExperimentalAll("alpha", dayId, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var body = Assert.IsType<ExperimentalController.ExperimentalDailyAllSpecsResponse>(ok.Value);
        var pyro = Assert.Single(body.Specs, x => x.Spec == "Pyromancer");
        Assert.Equal(5, pyro.Wins);
        Assert.Equal(2, pyro.Losses);
        Assert.Equal(20, pyro.Kills);
        Assert.Equal(8, pyro.Deaths);
        Assert.Equal(5, body.Total.Wins);
        Assert.Equal(2, body.Total.Losses);
        Assert.Equal(20, body.Total.Kills);
        Assert.Equal(8, body.Total.Deaths);
    }

    [Fact]
    public async Task GetDailyExperimentalAll_WhenIdUnknown_Returns404()
    {
        await using var db = CreateDbContextWithDailyStatsTable();
        db.Names.Add(new PlayerName { Uuid = TestUuid, Name = "alpha", PreviousNames = [] });
        await db.SaveChangesAsync();

        var specWeights = new Mock<ISpecWeightsService>();
        var controller = CreateController(specWeights.Object, dbContext: db);

        var result = await controller.GetDailyExperimentalAll("alpha", 99999, CancellationToken.None);

        AssertProblem(
            result.Result!,
            StatusCodes.Status404NotFound,
            "No day found with id 99999.");
    }

    [Fact]
    public async Task GetWeekly_WhenNameNotFound_ReturnsBadRequest()
    {
        var specWeights = new Mock<ISpecWeightsService>();
        var controller = CreateController(specWeights.Object);

        var result = await controller.GetWeekly("does-not-exist", null, CancellationToken.None);

        var pd = AssertProblem(result.Result!, StatusCodes.Status400BadRequest);
        Assert.Contains("does-not-exist", pd.Detail, StringComparison.Ordinal);
    }

    [Fact]
    public async Task GetWeekly_WhenNameEmpty_ReturnsBadRequest()
    {
        var specWeights = new Mock<ISpecWeightsService>();
        var controller = CreateController(specWeights.Object);

        var result = await controller.GetWeekly("   ", null, CancellationToken.None);

        AssertProblem(result.Result!, StatusCodes.Status400BadRequest, "name must not be empty.");
    }

    [Fact]
    public async Task GetWeekly_WhenNameAmbiguous_ReturnsConflict()
    {
        await using var db = CreateDbContext();
        db.Names.AddRange(
            new PlayerName { Uuid = TestUuid, Name = "alpha", PreviousNames = [] },
            new PlayerName { Uuid = U2, Name = "alpha", PreviousNames = [] });
        await db.SaveChangesAsync();

        var specWeights = new Mock<ISpecWeightsService>();
        var controller = CreateController(specWeights.Object, dbContext: db);

        var result = await controller.GetWeekly("alpha", null, CancellationToken.None);

        AssertProblem(
            result.Result!,
            StatusCodes.Status409Conflict,
            "One or more player names are ambiguous in names table: alpha.");
    }

    [Fact]
    public async Task GetWeekly_WhenNoWeeklyRow_ReturnsOkWithZeros()
    {
        await using var db = CreateDbContextWithWeeklyStatsTable();
        db.Names.Add(new PlayerName { Uuid = TestUuid, Name = "alpha", PreviousNames = [] });
        await db.SaveChangesAsync();

        var specWeights = new Mock<ISpecWeightsService>();
        var controller = CreateController(specWeights.Object, dbContext: db);

        var result = await controller.GetWeekly("alpha", null, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var body = Assert.IsType<ExperimentalController.ExperimentalWeeklyStatsResponse>(ok.Value);
        Assert.Equal(0, body.Wins);
        Assert.Equal(0, body.Losses);
        Assert.Equal(0, body.Kills);
        Assert.Equal(0, body.Deaths);
    }

    [Fact]
    public async Task GetWeekly_WhenRowExists_ReturnsOkWithStats()
    {
        await using var db = CreateDbContextWithWeeklyStatsTable();
        db.Names.Add(new PlayerName { Uuid = TestUuid, Name = "alpha", PreviousNames = [] });
        db.ExperimentalWeeklyStats.Add(new ExperimentalWeeklyStats
        {
            Uuid = TestUuid,
            Wins = 3,
            Losses = 1,
            Kills = 10,
            Deaths = 4
        });
        await db.SaveChangesAsync();

        var specWeights = new Mock<ISpecWeightsService>();
        var controller = CreateController(specWeights.Object, dbContext: db);

        var result = await controller.GetWeekly("alpha", null, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var body = Assert.IsType<ExperimentalController.ExperimentalWeeklyStatsResponse>(ok.Value);
        Assert.Equal(3, body.Wins);
        Assert.Equal(1, body.Losses);
        Assert.Equal(10, body.Kills);
        Assert.Equal(4, body.Deaths);
    }

    [Fact]
    public async Task GetWeekly_WhenIdGiven_UsesHistoricalStats()
    {
        const int weekId = 42;
        await using var db = CreateDbContextWithWeeklyStatsTable();
        db.Names.Add(new PlayerName { Uuid = TestUuid, Name = "alpha", PreviousNames = [] });
        db.TimeWeeks.Add(new TimeWeek { Id = weekId, Timestamp = DateTime.UtcNow });
        db.ExperimentalWeeklyStatsWeek.Add(new ExperimentalWeeklyStatsWeek
        {
            WeekStartDate = weekId,
            Uuid = TestUuid,
            Wins = 5,
            Losses = 2,
            Kills = 20,
            Deaths = 8
        });
        await db.SaveChangesAsync();

        var specWeights = new Mock<ISpecWeightsService>();
        var controller = CreateController(specWeights.Object, dbContext: db);

        var result = await controller.GetWeekly("alpha", weekId, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var body = Assert.IsType<ExperimentalController.ExperimentalWeeklyStatsResponse>(ok.Value);
        Assert.Equal(5, body.Wins);
        Assert.Equal(2, body.Losses);
        Assert.Equal(20, body.Kills);
        Assert.Equal(8, body.Deaths);
    }

    [Fact]
    public async Task GetWeekly_WhenIdUnknown_Returns404()
    {
        await using var db = CreateDbContextWithWeeklyStatsTable();
        db.Names.Add(new PlayerName { Uuid = TestUuid, Name = "alpha", PreviousNames = [] });
        await db.SaveChangesAsync();

        var specWeights = new Mock<ISpecWeightsService>();
        var controller = CreateController(specWeights.Object, dbContext: db);

        var result = await controller.GetWeekly("alpha", 99999, CancellationToken.None);

        AssertProblem(
            result.Result!,
            StatusCodes.Status404NotFound,
            "No week found with id 99999.");
    }

    [Fact]
    public async Task GetWeeklyExperimentalAll_WhenNameNotFound_ReturnsBadRequest()
    {
        var specWeights = new Mock<ISpecWeightsService>();
        var controller = CreateController(specWeights.Object);

        var result = await controller.GetWeeklyExperimentalAll("does-not-exist", null, CancellationToken.None);

        var pd = AssertProblem(result.Result!, StatusCodes.Status400BadRequest);
        Assert.Contains("does-not-exist", pd.Detail, StringComparison.Ordinal);
    }

    [Fact]
    public async Task GetWeeklyExperimentalAll_WhenNameEmpty_ReturnsBadRequest()
    {
        var specWeights = new Mock<ISpecWeightsService>();
        var controller = CreateController(specWeights.Object);

        var result = await controller.GetWeeklyExperimentalAll("   ", null, CancellationToken.None);

        AssertProblem(result.Result!, StatusCodes.Status400BadRequest, "name must not be empty.");
    }

    [Fact]
    public async Task GetWeeklyExperimentalAll_WhenNameAmbiguous_ReturnsConflict()
    {
        await using var db = CreateDbContextWithWeeklyStatsTable();
        db.Names.AddRange(
            new PlayerName { Uuid = TestUuid, Name = "alpha", PreviousNames = [] },
            new PlayerName { Uuid = U2, Name = "alpha", PreviousNames = [] });
        await db.SaveChangesAsync();

        var specWeights = new Mock<ISpecWeightsService>();
        var controller = CreateController(specWeights.Object, dbContext: db);

        var result = await controller.GetWeeklyExperimentalAll("alpha", null, CancellationToken.None);

        AssertProblem(
            result.Result!,
            StatusCodes.Status409Conflict,
            "One or more player names are ambiguous in names table: alpha.");
    }

    [Fact]
    public async Task GetWeeklyExperimentalAll_WhenNoRow_ReturnsOkWithZeros()
    {
        await using var db = CreateDbContextWithWeeklyStatsTable();
        db.Names.Add(new PlayerName { Uuid = TestUuid, Name = "alpha", PreviousNames = [] });
        await db.SaveChangesAsync();

        var specWeights = new Mock<ISpecWeightsService>();
        var controller = CreateController(specWeights.Object, dbContext: db);

        var result = await controller.GetWeeklyExperimentalAll("alpha", null, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var body = Assert.IsType<ExperimentalController.ExperimentalWeeklyAllSpecsResponse>(ok.Value);
        Assert.Equal(18, body.Specs.Count);
        Assert.All(body.Specs, entry =>
        {
            Assert.Equal(0, entry.Wins);
            Assert.Equal(0, entry.Losses);
            Assert.Equal(0, entry.Kills);
            Assert.Equal(0, entry.Deaths);
        });
        Assert.Equal("Total", body.Total.Spec);
        Assert.Equal(0, body.Total.Wins);
        Assert.Equal(0, body.Total.Losses);
        Assert.Equal(0, body.Total.Kills);
        Assert.Equal(0, body.Total.Deaths);
    }

    [Fact]
    public async Task GetWeeklyExperimentalAll_WhenCurrentRowExists_ReturnsOkWithStats()
    {
        await using var db = CreateDbContextWithWeeklyStatsTable();
        db.Names.Add(new PlayerName { Uuid = TestUuid, Name = "alpha", PreviousNames = [] });
        db.ExperimentalSpecsWlCurrentWeek.Add(new ExperimentalSpecsWlCurrentWeek
        {
            Uuid = TestUuid,
            PyromancerWins = 3,
            PyromancerLosses = 1,
            PyromancerKills = 10,
            PyromancerDeaths = 4
        });
        await db.SaveChangesAsync();

        var specWeights = new Mock<ISpecWeightsService>();
        var controller = CreateController(specWeights.Object, dbContext: db);

        var result = await controller.GetWeeklyExperimentalAll("alpha", null, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var body = Assert.IsType<ExperimentalController.ExperimentalWeeklyAllSpecsResponse>(ok.Value);
        var pyro = Assert.Single(body.Specs, x => x.Spec == "Pyromancer");
        Assert.Equal(3, pyro.Wins);
        Assert.Equal(1, pyro.Losses);
        Assert.Equal(10, pyro.Kills);
        Assert.Equal(4, pyro.Deaths);
        Assert.Equal(3, body.Total.Wins);
        Assert.Equal(1, body.Total.Losses);
        Assert.Equal(10, body.Total.Kills);
        Assert.Equal(4, body.Total.Deaths);
    }

    [Fact]
    public async Task GetWeeklyExperimentalAll_WhenIdGiven_UsesHistoricalStats()
    {
        const int weekId = 42;
        await using var db = CreateDbContextWithWeeklyStatsTable();
        db.Names.Add(new PlayerName { Uuid = TestUuid, Name = "alpha", PreviousNames = [] });
        db.TimeWeeks.Add(new TimeWeek { Id = weekId, Timestamp = DateTime.UtcNow });
        db.ExperimentalSpecsWlWeek.Add(new ExperimentalSpecsWlWeek
        {
            WeekStartDate = weekId,
            Uuid = TestUuid,
            PyromancerWins = 5,
            PyromancerLosses = 2,
            PyromancerKills = 20,
            PyromancerDeaths = 8
        });
        await db.SaveChangesAsync();

        var specWeights = new Mock<ISpecWeightsService>();
        var controller = CreateController(specWeights.Object, dbContext: db);

        var result = await controller.GetWeeklyExperimentalAll("alpha", weekId, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var body = Assert.IsType<ExperimentalController.ExperimentalWeeklyAllSpecsResponse>(ok.Value);
        var pyro = Assert.Single(body.Specs, x => x.Spec == "Pyromancer");
        Assert.Equal(5, pyro.Wins);
        Assert.Equal(2, pyro.Losses);
        Assert.Equal(20, pyro.Kills);
        Assert.Equal(8, pyro.Deaths);
        Assert.Equal(5, body.Total.Wins);
        Assert.Equal(2, body.Total.Losses);
        Assert.Equal(20, body.Total.Kills);
        Assert.Equal(8, body.Total.Deaths);
    }

    [Fact]
    public async Task GetWeeklyExperimentalAll_WhenIdUnknown_Returns404()
    {
        await using var db = CreateDbContextWithWeeklyStatsTable();
        db.Names.Add(new PlayerName { Uuid = TestUuid, Name = "alpha", PreviousNames = [] });
        await db.SaveChangesAsync();

        var specWeights = new Mock<ISpecWeightsService>();
        var controller = CreateController(specWeights.Object, dbContext: db);

        var result = await controller.GetWeeklyExperimentalAll("alpha", 99999, CancellationToken.None);

        AssertProblem(
            result.Result!,
            StatusCodes.Status404NotFound,
            "No week found with id 99999.");
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
                        new(TestUuid, "alpha", "Pyromancer", 100, 0, 7, 4.0, false),
                        new(U2, "beta", "Cryomancer", 100, 0, 5, 4.0, false)
                    ]),
                new ExperimentalBalanceTeam(
                    200,
                    0,
                    8,
                    4.0,
                    [
                        new(U3, "gamma", "Aquamancer", 100, 0, 3, 2.0, false),
                        new(U4, "delta", "Berserker", 100, 0, 5, 2.0, false)
                    ])
            ],
            0,
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
        var controller = CreateController(specWeights.Object, balance: balance.Object, dbContext: db);

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
        var controller = CreateController(specWeights.Object, balance: balance.Object);

        var result = await controller.Balance(new ExperimentalController.ExperimentalBalanceInputRequest([]), CancellationToken.None);

        AssertProblem(
            result.Result!,
            StatusCodes.Status400BadRequest,
            "players must not be empty.");
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
                        new(TestUuid, "alpha", "Pyromancer", 100, 0, 7, 4.0, false),
                        new(U2, "beta", "Cryomancer", 100, 0, 5, 4.0, false)
                    ]),
                new ExperimentalBalanceTeam(
                    200,
                    0,
                    8,
                    4.0,
                    [
                        new(U3, "gamma", "Aquamancer", 100, 0, 3, 2.0, false),
                        new(U4, "delta", "Berserker", 100, 0, 5, 2.0, false)
                    ])
            ],
            0,
            meta);

        var balance = new Mock<IExperimentalBalanceService>();
        balance.Setup(x => x.BalanceAsync(It.IsAny<ExperimentalBalanceRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ExperimentalBalanceServiceResult(true, expected, null));

        var specWeights = new Mock<ISpecWeightsService>();
        var controller = CreateController(specWeights.Object, balance: balance.Object, dbContext: db);

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
        var controller = CreateController(specWeights.Object, balance: balance.Object, dbContext: db);

        var result = await controller.Balance(
            new ExperimentalController.ExperimentalBalanceInputRequest(["alpha", "does-not-exist"]),
            CancellationToken.None);

        var pd = AssertProblem(result.Result!, StatusCodes.Status400BadRequest);
        Assert.Contains("does-not-exist", pd.Detail, StringComparison.Ordinal);
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
    public async Task UnconfirmBalance_WhenServiceSucceeds_ReturnsOkWithBalanceId()
    {
        var confirm = new Mock<IExperimentalBalanceConfirmService>();
        confirm.Setup(x => x.UnconfirmAsync(TestBalanceId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ExperimentalBalanceConfirmServiceResult(true, 200, null));

        var specWeights = new Mock<ISpecWeightsService>();
        var controller = CreateController(specWeights.Object, confirm: confirm.Object);

        var result = await controller.UnconfirmBalance(TestBalanceId, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var body = Assert.IsType<ExperimentalBalanceConfirmResponse>(ok.Value);
        Assert.Equal(TestBalanceId, body.BalanceId);
    }

    [Fact]
    public async Task InputBalance_WhenServiceSucceeds_ReturnsOkWithBalanceId()
    {
        var input = new Mock<IExperimentalBalanceInputService>();
        input.Setup(x => x.InputAsync(TestBalanceId, It.IsAny<ExperimentalBalanceInputBody>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ExperimentalBalanceInputServiceResult(
                true,
                200,
                null,
                new ExperimentalBalanceInputResponse(TestBalanceId, Array.Empty<ExperimentalBalanceChangeItem>())));

        var specWeights = new Mock<ISpecWeightsService>();
        var controller = CreateController(specWeights.Object, input: input.Object);

        var body = new ExperimentalBalanceInputBody([], [], "aaaaaaaaaaaaaaaaaaaaaaaa");
        var result = await controller.InputBalance(TestBalanceId, body, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<ExperimentalBalanceInputResponse>(ok.Value);
        Assert.Equal(TestBalanceId, response.BalanceId);
        Assert.NotNull(response.Changes);
    }

    [Fact]
    public async Task InputBalance_WhenBodyNull_ReturnsBadRequest()
    {
        var specWeights = new Mock<ISpecWeightsService>();
        var controller = CreateController(specWeights.Object);

        var result = await controller.InputBalance(TestBalanceId, null, CancellationToken.None);

        AssertProblem(
            result.Result!,
            StatusCodes.Status400BadRequest,
            "Request body is required.");
    }

    [Fact]
    public async Task UninputBalance_WhenServiceSucceeds_ReturnsOkWithBalanceId()
    {
        var input = new Mock<IExperimentalBalanceInputService>();
        input.Setup(x => x.UninputAsync(TestBalanceId, It.IsAny<ExperimentalBalanceInputBody>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ExperimentalBalanceInputServiceResult(
                true,
                200,
                null,
                new ExperimentalBalanceInputResponse(TestBalanceId, null)));

        var specWeights = new Mock<ISpecWeightsService>();
        var controller = CreateController(specWeights.Object, input: input.Object);

        var body = new ExperimentalBalanceInputBody([], [], "aaaaaaaaaaaaaaaaaaaaaaaa");
        var result = await controller.UninputBalance(TestBalanceId, body, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<ExperimentalBalanceInputResponse>(ok.Value);
        Assert.Equal(TestBalanceId, response.BalanceId);
    }

    [Fact]
    public async Task UninputBalance_WhenBodyNull_ReturnsBadRequest()
    {
        var specWeights = new Mock<ISpecWeightsService>();
        var controller = CreateController(specWeights.Object);

        var result = await controller.UninputBalance(TestBalanceId, null, CancellationToken.None);

        AssertProblem(
            result.Result!,
            StatusCodes.Status400BadRequest,
            "Request body is required.");
    }

    [Fact]
    public async Task ClearInputBalance_WhenServiceSucceeds_ReturnsOkWithBalanceId()
    {
        var input = new Mock<IExperimentalBalanceInputService>();
        input.Setup(x => x.ClearInputAsync(TestBalanceId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ExperimentalBalanceInputServiceResult(
                true,
                200,
                null,
                new ExperimentalBalanceInputResponse(TestBalanceId, null)));

        var specWeights = new Mock<ISpecWeightsService>();
        var controller = CreateController(specWeights.Object, input: input.Object);

        var result = await controller.ClearInputBalance(TestBalanceId, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<ExperimentalBalanceInputResponse>(ok.Value);
        Assert.Equal(TestBalanceId, response.BalanceId);
    }

    [Fact]
    public async Task GenerateInputBalance_WhenLogExists_ReturnsOkWithMockBody()
    {
        var teams = new List<ExperimentalBalanceTeam>
        {
            new(
                200,
                0,
                12,
                8.0,
                [
                    new ExperimentalBalancePlayerSpec(TestUuid, "alpha", "Pyromancer", 100, 0, 7, 4.0, false),
                    new ExperimentalBalancePlayerSpec(U2, "beta", "Cryomancer", 100, 0, 5, 4.0, false)
                ]),
            new(
                200,
                0,
                8,
                4.0,
                [
                    new ExperimentalBalancePlayerSpec(U3, "gamma", "Aquamancer", 100, 0, 3, 2.0, false),
                    new ExperimentalBalancePlayerSpec(U4, "delta", "Berserker", 100, 0, 5, 2.0, false)
                ])
        };
        await using var db = CreateDbContext();
        db.ExperimentalBalanceLogs.Add(new ExperimentalBalanceLog
        {
            BalanceId = TestBalanceId,
            Balance = JsonSerializer.Serialize(teams),
            Meta = "{}",
            CreatedAt = DateTime.UtcNow,
            Posted = false
        });
        await db.SaveChangesAsync();

        var specWeights = new Mock<ISpecWeightsService>();
        var controller = CreateController(specWeights.Object, dbContext: db);

        var result = await controller.GenerateInputBalance(TestBalanceId, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var body = Assert.IsType<ExperimentalBalanceInputBody>(ok.Value);
        Assert.Equal(ExperimentalBalanceMockInputBodyBuilder.PlaceholderGameId, body.GameId);
        Assert.Equal(2, body.Winners?.Count);
        Assert.Equal(2, body.Losers?.Count);
        Assert.Contains(body.Winners!, w => w.Uuid == TestUuid);
        Assert.Contains(body.Losers!, l => l.Uuid == U3);
        Assert.All(body.Winners!, w =>
        {
            Assert.InRange(w.Kills, 0, 15);
            Assert.InRange(w.Deaths, 0, 15);
        });
        Assert.All(body.Losers!, l =>
        {
            Assert.InRange(l.Kills, 0, 15);
            Assert.InRange(l.Deaths, 0, 15);
        });
    }

    [Fact]
    public async Task GenerateInputBalance_WhenMissing_ReturnsNotFound()
    {
        await using var db = CreateDbContext();
        var specWeights = new Mock<ISpecWeightsService>();
        var controller = CreateController(specWeights.Object, dbContext: db);

        var result = await controller.GenerateInputBalance(TestBalanceId, CancellationToken.None);

        AssertProblem(
            result.Result!,
            StatusCodes.Status404NotFound,
            "The requested resource was not found.");
    }

    [Fact]
    public async Task GetLogs_WhenEmpty_ReturnsZeroCountAndAllSpecKeys()
    {
        var specWeights = new Mock<ISpecWeightsService>();
        var (controller, _) = CreateControllerWithSpecLogsService(specWeights.Object);

        var result = await controller.GetLogs(CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var body = Assert.IsType<ExperimentalSpecLogsResponse>(ok.Value);
        Assert.Equal(0, body.Count);
        Assert.Equal(18, body.Log.Count);
        Assert.All(body.Log.Values, names => Assert.Empty(names));
        Assert.Contains("pyromancer", body.Log.Keys);
    }

    [Fact]
    public async Task GetLogs_WhenValid_ReturnsNamesOrderedByBalanceCreatedAt()
    {
        var earlierBalanceId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var laterBalanceId = Guid.Parse("22222222-2222-2222-2222-222222222222");
        var earlierTime = new DateTime(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc);
        var laterTime = new DateTime(2026, 1, 2, 12, 0, 0, DateTimeKind.Utc);

        var specWeights = new Mock<ISpecWeightsService>();
        var (controller, db) = CreateControllerWithSpecLogsService(specWeights.Object);
        db.Names.AddRange(
            new PlayerName { Uuid = TestUuid, Name = "alpha", PreviousNames = [] },
            new PlayerName { Uuid = U2, Name = "beta", PreviousNames = [] });
        db.ExperimentalBalanceLogs.AddRange(
            new ExperimentalBalanceLog
            {
                BalanceId = earlierBalanceId,
                Balance = "[]",
                Meta = "{}",
                CreatedAt = earlierTime
            },
            new ExperimentalBalanceLog
            {
                BalanceId = laterBalanceId,
                Balance = "[]",
                Meta = "{}",
                CreatedAt = laterTime
            });
        db.ExperimentalSpecLogs.AddRange(
            new ExperimentalSpecLog { BalanceId = laterBalanceId, Pyromancer = U2 },
            new ExperimentalSpecLog { BalanceId = earlierBalanceId, Pyromancer = TestUuid, Cryomancer = U2 });
        await db.SaveChangesAsync();

        var result = await controller.GetLogs(CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var body = Assert.IsType<ExperimentalSpecLogsResponse>(ok.Value);
        Assert.Equal(2, body.Count);
        Assert.Equal(["alpha", "beta"], body.Log["pyromancer"]);
        Assert.Equal(["beta"], body.Log["cryomancer"]);
    }

    [Fact]
    public async Task GetLogs_WhenMissingName_ReturnsInternalServerError()
    {
        var balanceId = Guid.Parse("33333333-3333-3333-3333-333333333333");
        var unknownUuid = Guid.Parse("99999999-9999-9999-9999-999999999999");

        var specWeights = new Mock<ISpecWeightsService>();
        var (controller, db) = CreateControllerWithSpecLogsService(specWeights.Object);
        db.ExperimentalBalanceLogs.Add(new ExperimentalBalanceLog
        {
            BalanceId = balanceId,
            Balance = "[]",
            Meta = "{}",
            CreatedAt = DateTime.UtcNow
        });
        db.ExperimentalSpecLogs.Add(new ExperimentalSpecLog { BalanceId = balanceId, Pyromancer = unknownUuid });
        await db.SaveChangesAsync();

        var result = await controller.GetLogs(CancellationToken.None);

        var pd = AssertProblem(
            result.Result!,
            StatusCodes.Status500InternalServerError,
            $"No name found for player {unknownUuid}.");
    }

    [Fact]
    public async Task TruncateLogs_WhenEmpty_ReturnsZeroCountAndAllSpecKeys()
    {
        var specWeights = new Mock<ISpecWeightsService>();
        var (controller, db) = CreateControllerWithSpecLogsService(specWeights.Object);

        var result = await controller.TruncateLogs(CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var body = Assert.IsType<ExperimentalSpecLogsResponse>(ok.Value);
        Assert.Equal(0, body.Count);
        Assert.Equal(18, body.Log.Count);
        Assert.All(body.Log.Values, names => Assert.Empty(names));
        Assert.Empty(db.ExperimentalSpecLogs);
    }

    [Fact]
    public async Task TruncateLogs_WhenTwentyRows_RemovesEightOldest()
    {
        var specWeights = new Mock<ISpecWeightsService>();
        var (controller, db) = CreateControllerWithSpecLogsService(specWeights.Object);
        var baseTime = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var names = new List<PlayerName>();
        var balanceLogs = new List<ExperimentalBalanceLog>();
        var specLogs = new List<ExperimentalSpecLog>();

        for (var i = 0; i < 10; i++)
        {
            var balanceId = Guid.Parse($"10000000-0000-0000-0000-{i:D12}");
            var uuid1 = Guid.Parse($"20000000-0000-0000-0000-{i * 2:D12}");
            var uuid2 = Guid.Parse($"20000000-0000-0000-0001-{i * 2:D12}");
            names.Add(new PlayerName { Uuid = uuid1, Name = $"p{i}a", PreviousNames = [] });
            names.Add(new PlayerName { Uuid = uuid2, Name = $"p{i}b", PreviousNames = [] });
            balanceLogs.Add(new ExperimentalBalanceLog
            {
                BalanceId = balanceId,
                Balance = "[]",
                Meta = "{}",
                CreatedAt = baseTime.AddHours(i)
            });
            specLogs.Add(new ExperimentalSpecLog { BalanceId = balanceId, Pyromancer = uuid1 });
            specLogs.Add(new ExperimentalSpecLog { BalanceId = balanceId, Cryomancer = uuid2 });
        }

        db.Names.AddRange(names);
        db.ExperimentalBalanceLogs.AddRange(balanceLogs);
        db.ExperimentalSpecLogs.AddRange(specLogs);
        await db.SaveChangesAsync();

        var result = await controller.TruncateLogs(CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var body = Assert.IsType<ExperimentalSpecLogsResponse>(ok.Value);
        Assert.Equal(8, body.Count);
        Assert.Equal(["p0a", "p1a", "p2a", "p3a"], body.Log["pyromancer"]);
        Assert.Equal(["p0b", "p1b", "p2b", "p3b"], body.Log["cryomancer"]);
        Assert.Equal(12, await db.ExperimentalSpecLogs.CountAsync());
        var remainingPyro = await db.ExperimentalSpecLogs
            .Where(x => x.Pyromancer != null)
            .Join(db.Names, x => x.Pyromancer, n => n.Uuid, (_, n) => n.Name)
            .OrderBy(x => x)
            .ToListAsync();
        Assert.Equal(["p4a", "p5a", "p6a", "p7a", "p8a", "p9a"], remainingPyro);
    }

    [Fact]
    public async Task TruncateLogs_WhenFiveRows_RemovesTwoOldest()
    {
        var specWeights = new Mock<ISpecWeightsService>();
        var (controller, db) = CreateControllerWithSpecLogsService(specWeights.Object);
        var baseTime = new DateTime(2026, 2, 1, 0, 0, 0, DateTimeKind.Utc);

        for (var i = 0; i < 5; i++)
        {
            var balanceId = Guid.Parse($"30000000-0000-0000-0000-{i:D12}");
            var uuid = Guid.Parse($"40000000-0000-0000-0000-{i:D12}");
            db.Names.Add(new PlayerName { Uuid = uuid, Name = $"r{i}", PreviousNames = [] });
            db.ExperimentalBalanceLogs.Add(new ExperimentalBalanceLog
            {
                BalanceId = balanceId,
                Balance = "[]",
                Meta = "{}",
                CreatedAt = baseTime.AddHours(i)
            });
            db.ExperimentalSpecLogs.Add(new ExperimentalSpecLog { BalanceId = balanceId, Pyromancer = uuid });
        }

        await db.SaveChangesAsync();

        var result = await controller.TruncateLogs(CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var body = Assert.IsType<ExperimentalSpecLogsResponse>(ok.Value);
        Assert.Equal(2, body.Count);
        Assert.Equal(["r0", "r1"], body.Log["pyromancer"]);
        Assert.Equal(3, await db.ExperimentalSpecLogs.CountAsync());
    }

    [Fact]
    public async Task TruncateLogs_WhenMissingNameOnRemovedRow_ReturnsInternalServerError()
    {
        var unknownUuid = Guid.Parse("99999999-9999-9999-9999-999999999999");
        var specWeights = new Mock<ISpecWeightsService>();
        var (controller, db) = CreateControllerWithSpecLogsService(specWeights.Object);
        var baseTime = new DateTime(2026, 3, 1, 0, 0, 0, DateTimeKind.Utc);

        for (var i = 0; i < 5; i++)
        {
            var balanceId = Guid.Parse($"50000000-0000-0000-0000-{i:D12}");
            var uuid = i == 0 ? unknownUuid : Guid.Parse($"60000000-0000-0000-0000-{i:D12}");
            if (i > 0)
            {
                db.Names.Add(new PlayerName { Uuid = uuid, Name = $"s{i}", PreviousNames = [] });
            }

            db.ExperimentalBalanceLogs.Add(new ExperimentalBalanceLog
            {
                BalanceId = balanceId,
                Balance = "[]",
                Meta = "{}",
                CreatedAt = baseTime.AddHours(i)
            });
            db.ExperimentalSpecLogs.Add(new ExperimentalSpecLog { BalanceId = balanceId, Pyromancer = uuid });
        }

        await db.SaveChangesAsync();

        var result = await controller.TruncateLogs(CancellationToken.None);

        AssertProblem(
            result.Result!,
            StatusCodes.Status500InternalServerError,
            $"No name found for player {unknownUuid}.");
        Assert.Equal(5, await db.ExperimentalSpecLogs.CountAsync());
    }

    [Fact]
    public async Task TruncateLogsLast_WhenEmpty_ReturnsZeroCountAndAllSpecKeys()
    {
        var specWeights = new Mock<ISpecWeightsService>();
        var (controller, db) = CreateControllerWithSpecLogsService(specWeights.Object);

        var result = await controller.TruncateLogsLast(CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var body = Assert.IsType<ExperimentalSpecLogsResponse>(ok.Value);
        Assert.Equal(0, body.Count);
        Assert.Equal(18, body.Log.Count);
        Assert.All(body.Log.Values, names => Assert.Empty(names));
        Assert.Empty(db.ExperimentalSpecLogs);
    }

    [Fact]
    public async Task TruncateLogsLast_WhenSixRows_RemovesTwoNewest()
    {
        var specWeights = new Mock<ISpecWeightsService>();
        var (controller, db) = CreateControllerWithSpecLogsService(specWeights.Object);
        var baseTime = new DateTime(2026, 4, 1, 0, 0, 0, DateTimeKind.Utc);
        var names = new List<PlayerName>();
        var balanceLogs = new List<ExperimentalBalanceLog>();
        var specLogs = new List<ExperimentalSpecLog>();

        for (var i = 0; i < 3; i++)
        {
            var balanceId = Guid.Parse($"70000000-0000-0000-0000-{i:D12}");
            var uuid1 = Guid.Parse($"80000000-0000-0000-0000-{i * 2:D12}");
            var uuid2 = Guid.Parse($"80000000-0000-0000-0001-{i * 2:D12}");
            names.Add(new PlayerName { Uuid = uuid1, Name = $"t{i}a", PreviousNames = [] });
            names.Add(new PlayerName { Uuid = uuid2, Name = $"t{i}b", PreviousNames = [] });
            balanceLogs.Add(new ExperimentalBalanceLog
            {
                BalanceId = balanceId,
                Balance = "[]",
                Meta = "{}",
                CreatedAt = baseTime.AddHours(i)
            });
            specLogs.Add(new ExperimentalSpecLog { BalanceId = balanceId, Pyromancer = uuid1 });
            specLogs.Add(new ExperimentalSpecLog { BalanceId = balanceId, Cryomancer = uuid2 });
        }

        db.Names.AddRange(names);
        db.ExperimentalBalanceLogs.AddRange(balanceLogs);
        db.ExperimentalSpecLogs.AddRange(specLogs);
        await db.SaveChangesAsync();

        var result = await controller.TruncateLogsLast(CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var body = Assert.IsType<ExperimentalSpecLogsResponse>(ok.Value);
        Assert.Equal(2, body.Count);
        Assert.Equal(["t2a"], body.Log["pyromancer"]);
        Assert.Equal(["t2b"], body.Log["cryomancer"]);
        Assert.Equal(4, await db.ExperimentalSpecLogs.CountAsync());
        var remainingPyro = await db.ExperimentalSpecLogs
            .Where(x => x.Pyromancer != null)
            .Join(db.Names, x => x.Pyromancer, n => n.Uuid, (_, n) => n.Name)
            .OrderBy(x => x)
            .ToListAsync();
        Assert.Equal(["t0a", "t1a"], remainingPyro);
    }

    [Fact]
    public async Task TruncateLogsLast_WhenOneRow_RemovesOne()
    {
        var specWeights = new Mock<ISpecWeightsService>();
        var (controller, db) = CreateControllerWithSpecLogsService(specWeights.Object);
        var balanceId = Guid.Parse("71000000-0000-0000-0000-000000000001");
        var uuid = Guid.Parse("81000000-0000-0000-0000-000000000001");
        db.Names.Add(new PlayerName { Uuid = uuid, Name = "solo", PreviousNames = [] });
        db.ExperimentalBalanceLogs.Add(new ExperimentalBalanceLog
        {
            BalanceId = balanceId,
            Balance = "[]",
            Meta = "{}",
            CreatedAt = DateTime.UtcNow
        });
        db.ExperimentalSpecLogs.Add(new ExperimentalSpecLog { BalanceId = balanceId, Pyromancer = uuid });
        await db.SaveChangesAsync();

        var result = await controller.TruncateLogsLast(CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var body = Assert.IsType<ExperimentalSpecLogsResponse>(ok.Value);
        Assert.Equal(1, body.Count);
        Assert.Equal(["solo"], body.Log["pyromancer"]);
        Assert.Empty(db.ExperimentalSpecLogs);
    }

    [Fact]
    public async Task TruncateLogsLast_WhenMissingNameOnRemovedRow_ReturnsInternalServerError()
    {
        var unknownUuid = Guid.Parse("99999999-9999-9999-9999-999999999999");
        var specWeights = new Mock<ISpecWeightsService>();
        var (controller, db) = CreateControllerWithSpecLogsService(specWeights.Object);
        var baseTime = new DateTime(2026, 5, 1, 0, 0, 0, DateTimeKind.Utc);

        for (var i = 0; i < 5; i++)
        {
            var balanceId = Guid.Parse($"72000000-0000-0000-0000-{i:D12}");
            var uuid = i == 4 ? unknownUuid : Guid.Parse($"82000000-0000-0000-0000-{i:D12}");
            if (i < 4)
            {
                db.Names.Add(new PlayerName { Uuid = uuid, Name = $"u{i}", PreviousNames = [] });
            }

            db.ExperimentalBalanceLogs.Add(new ExperimentalBalanceLog
            {
                BalanceId = balanceId,
                Balance = "[]",
                Meta = "{}",
                CreatedAt = baseTime.AddHours(i)
            });
            db.ExperimentalSpecLogs.Add(new ExperimentalSpecLog { BalanceId = balanceId, Pyromancer = uuid });
        }

        await db.SaveChangesAsync();

        var result = await controller.TruncateLogsLast(CancellationToken.None);

        AssertProblem(
            result.Result!,
            StatusCodes.Status500InternalServerError,
            $"No name found for player {unknownUuid}.");
        Assert.Equal(5, await db.ExperimentalSpecLogs.CountAsync());
    }

    [Fact]
    public async Task ClearLogs_WhenEmpty_ReturnsZeroCountAndAllSpecKeys()
    {
        var specWeights = new Mock<ISpecWeightsService>();
        var (controller, db) = CreateControllerWithSpecLogsService(specWeights.Object);

        var result = await controller.ClearLogs(CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var body = Assert.IsType<ExperimentalSpecLogsResponse>(ok.Value);
        Assert.Equal(0, body.Count);
        Assert.Equal(18, body.Log.Count);
        Assert.All(body.Log.Values, names => Assert.Empty(names));
        Assert.Empty(db.ExperimentalSpecLogs);
    }

    [Fact]
    public async Task ClearLogs_WhenFiveRows_RemovesAllFive()
    {
        var specWeights = new Mock<ISpecWeightsService>();
        var (controller, db) = CreateControllerWithSpecLogsService(specWeights.Object);
        var baseTime = new DateTime(2026, 2, 1, 0, 0, 0, DateTimeKind.Utc);

        for (var i = 0; i < 5; i++)
        {
            var balanceId = Guid.Parse($"30000000-0000-0000-0000-{i:D12}");
            var uuid = Guid.Parse($"40000000-0000-0000-0000-{i:D12}");
            db.Names.Add(new PlayerName { Uuid = uuid, Name = $"r{i}", PreviousNames = [] });
            db.ExperimentalBalanceLogs.Add(new ExperimentalBalanceLog
            {
                BalanceId = balanceId,
                Balance = "[]",
                Meta = "{}",
                CreatedAt = baseTime.AddHours(i)
            });
            db.ExperimentalSpecLogs.Add(new ExperimentalSpecLog { BalanceId = balanceId, Pyromancer = uuid });
        }

        await db.SaveChangesAsync();

        var result = await controller.ClearLogs(CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var body = Assert.IsType<ExperimentalSpecLogsResponse>(ok.Value);
        Assert.Equal(5, body.Count);
        Assert.Equal(["r0", "r1", "r2", "r3", "r4"], body.Log["pyromancer"]);
        Assert.Empty(db.ExperimentalSpecLogs);
    }

    [Fact]
    public async Task ClearLogs_WhenMissingNameOnRow_ReturnsInternalServerError()
    {
        var unknownUuid = Guid.Parse("99999999-9999-9999-9999-999999999999");
        var specWeights = new Mock<ISpecWeightsService>();
        var (controller, db) = CreateControllerWithSpecLogsService(specWeights.Object);
        var baseTime = new DateTime(2026, 3, 1, 0, 0, 0, DateTimeKind.Utc);

        for (var i = 0; i < 5; i++)
        {
            var balanceId = Guid.Parse($"50000000-0000-0000-0000-{i:D12}");
            var uuid = i == 0 ? unknownUuid : Guid.Parse($"60000000-0000-0000-0000-{i:D12}");
            if (i > 0)
            {
                db.Names.Add(new PlayerName { Uuid = uuid, Name = $"s{i}", PreviousNames = [] });
            }

            db.ExperimentalBalanceLogs.Add(new ExperimentalBalanceLog
            {
                BalanceId = balanceId,
                Balance = "[]",
                Meta = "{}",
                CreatedAt = baseTime.AddHours(i)
            });
            db.ExperimentalSpecLogs.Add(new ExperimentalSpecLog { BalanceId = balanceId, Pyromancer = uuid });
        }

        await db.SaveChangesAsync();

        var result = await controller.ClearLogs(CancellationToken.None);

        AssertProblem(
            result.Result!,
            StatusCodes.Status500InternalServerError,
            $"No name found for player {unknownUuid}.");
        Assert.Equal(5, await db.ExperimentalSpecLogs.CountAsync());
    }

    [Fact]
    public async Task GenerateInputBalance_WhenBalanceNotTwoTeams_ReturnsBadRequest()
    {
        var teams = new List<ExperimentalBalanceTeam>
        {
            new(
                200,
                0,
                12,
                8.0,
                [new ExperimentalBalancePlayerSpec(TestUuid, "alpha", "Pyromancer", 100, 0, 7, 4.0, false)])
        };
        await using var db = CreateDbContext();
        db.ExperimentalBalanceLogs.Add(new ExperimentalBalanceLog
        {
            BalanceId = TestBalanceId,
            Balance = JsonSerializer.Serialize(teams),
            Meta = "{}",
            CreatedAt = DateTime.UtcNow,
            Posted = false
        });
        await db.SaveChangesAsync();

        var specWeights = new Mock<ISpecWeightsService>();
        var controller = CreateController(specWeights.Object, dbContext: db);

        var result = await controller.GenerateInputBalance(TestBalanceId, CancellationToken.None);

        AssertProblem(
            result.Result!,
            StatusCodes.Status400BadRequest,
            "Stored balance must contain exactly two teams.");
    }

    private static ProblemDetails AssertProblem(
        IActionResult result,
        int expectedStatusCode,
        string? expectedDetail = null)
    {
        var obj = Assert.IsType<ObjectResult>(result);
        Assert.Equal(expectedStatusCode, obj.StatusCode);
        var pd = Assert.IsType<ProblemDetails>(obj.Value);
        if (expectedDetail is not null)
        {
            Assert.Equal(expectedDetail, pd.Detail);
        }

        return pd;
    }

    private static BalancerDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<BalancerDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new BalancerDbContext(options);
    }

    private static BalancerDbContext CreateDbContextWithDailyStatsTable()
    {
        var options = new DbContextOptionsBuilder<BalancerDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new TestBalancerDbContextForDailyStats(options);
    }

    private static BalancerDbContext CreateDbContextWithWeeklyStatsTable()
    {
        var options = new DbContextOptionsBuilder<BalancerDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new TestBalancerDbContextForWeeklyStats(options);
    }

    private sealed class TestBalancerDbContextForDailyStats(DbContextOptions<BalancerDbContext> options)
        : BalancerDbContext(options)
    {
        public override async Task<ExperimentalDailyStatsDay?> GetExperimentalDailyStatsForDayAsync(
            int dayId,
            Guid playerUuid,
            CancellationToken cancellationToken = default)
        {
            return await ExperimentalDailyStatsDay
                .AsNoTracking()
                .FirstOrDefaultAsync(
                    x => x.DayStartDate == dayId && x.Uuid == playerUuid,
                    cancellationToken);
        }

        public override async Task<ExperimentalSpecsWlDay?> GetExperimentalSpecsWlForDayAsync(
            int dayId,
            Guid playerUuid,
            CancellationToken cancellationToken = default)
        {
            return await ExperimentalSpecsWlDay
                .AsNoTracking()
                .FirstOrDefaultAsync(
                    x => x.DayStartDate == dayId && x.Uuid == playerUuid,
                    cancellationToken);
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Ignore<ExperimentalDailyStats>();
            modelBuilder.Entity<ExperimentalDailyStats>(entity =>
            {
                entity.ToTable("experimental_daily_stats_test");
                entity.HasKey(x => x.Uuid);
            });

            modelBuilder.Ignore<ExperimentalDailyStatsDay>();
            modelBuilder.Entity<ExperimentalDailyStatsDay>(entity =>
            {
                entity.ToTable("experimental_daily_stats_day_test");
                entity.HasKey(x => new { x.DayStartDate, x.Uuid });
            });

            modelBuilder.Ignore<ExperimentalSpecsWlCurrentDay>();
            modelBuilder.Entity<ExperimentalSpecsWlCurrentDay>(entity =>
            {
                entity.ToTable("experimental_specs_wl_current_day_test");
                entity.HasKey(x => x.Uuid);
            });

            modelBuilder.Ignore<ExperimentalSpecsWlDay>();
            modelBuilder.Entity<ExperimentalSpecsWlDay>(entity =>
            {
                entity.ToTable("experimental_specs_wl_day_test");
                entity.HasKey(x => new { x.DayStartDate, x.Uuid });
            });
        }
    }

    private sealed class TestBalancerDbContextForWeeklyStats(DbContextOptions<BalancerDbContext> options)
        : BalancerDbContext(options)
    {
        public override async Task<ExperimentalWeeklyStatsWeek?> GetExperimentalWeeklyStatsForWeekAsync(
            int weekId,
            Guid playerUuid,
            CancellationToken cancellationToken = default)
        {
            return await ExperimentalWeeklyStatsWeek
                .AsNoTracking()
                .FirstOrDefaultAsync(
                    x => x.WeekStartDate == weekId && x.Uuid == playerUuid,
                    cancellationToken);
        }

        public override async Task<ExperimentalSpecsWlWeek?> GetExperimentalSpecsWlForWeekAsync(
            int weekId,
            Guid playerUuid,
            CancellationToken cancellationToken = default)
        {
            return await ExperimentalSpecsWlWeek
                .AsNoTracking()
                .FirstOrDefaultAsync(
                    x => x.WeekStartDate == weekId && x.Uuid == playerUuid,
                    cancellationToken);
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Ignore<ExperimentalWeeklyStats>();
            modelBuilder.Entity<ExperimentalWeeklyStats>(entity =>
            {
                entity.ToTable("experimental_weekly_stats_test");
                entity.HasKey(x => x.Uuid);
            });

            modelBuilder.Ignore<ExperimentalWeeklyStatsWeek>();
            modelBuilder.Entity<ExperimentalWeeklyStatsWeek>(entity =>
            {
                entity.ToTable("experimental_weekly_stats_week_test");
                entity.HasKey(x => new { x.WeekStartDate, x.Uuid });
            });

            modelBuilder.Ignore<ExperimentalSpecsWlCurrentWeek>();
            modelBuilder.Entity<ExperimentalSpecsWlCurrentWeek>(entity =>
            {
                entity.ToTable("experimental_specs_wl_current_week_test");
                entity.HasKey(x => x.Uuid);
            });

            modelBuilder.Ignore<ExperimentalSpecsWlWeek>();
            modelBuilder.Entity<ExperimentalSpecsWlWeek>(entity =>
            {
                entity.ToTable("experimental_specs_wl_week_test");
                entity.HasKey(x => new { x.WeekStartDate, x.Uuid });
            });
        }
    }

    private sealed class TestDbContextFactory(DbContextOptions<BalancerDbContext> options) : IDbContextFactory<BalancerDbContext>
    {
        public BalancerDbContext CreateDbContext() => new(options);
    }

    private static (ExperimentalController Controller, BalancerDbContext Db) CreateControllerWithSpecLogsService(
        ISpecWeightsService specWeights)
    {
        var options = new DbContextOptionsBuilder<BalancerDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;
        var factory = new TestDbContextFactory(options);
        var db = factory.CreateDbContext();
        var specLogsService = new ExperimentalSpecLogsService(factory);
        var controller = CreateController(specWeights, specLogs: specLogsService, dbContext: db);
        return (controller, db);
    }
}
