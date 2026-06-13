using System.Reflection;
using System.Text.Json.Serialization;
using Asp.Versioning;
using BalancerAPI.Common.Auth;
using BalancerAPI.Business.Services;
using BalancerAPI.Data.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BalancerAPI.Api.Controllers;

[ApiVersion("1.0")]
[ApiController]
[Route("api/v{version:apiVersion}/[controller]")]
public class ExperimentalController(
    ISpecWeightsService specWeightsService,
    ISpecWeightLeaderboardService specWeightLeaderboardService,
    IExperimentalBalanceService experimentalBalanceService,
    IExperimentalBalanceConfirmService experimentalBalanceConfirmService,
    IExperimentalBalanceInputService experimentalBalanceInputService,
    IExperimentalSpecLogsService experimentalSpecLogsService,
    IExperimentalSpecBanService experimentalSpecBanService,
    IExperimentalSpecRequestService experimentalSpecRequestService,
    IPlayerKeyResolver playerKeyResolver,
    BalancerDbContext dbContext) : ControllerBase
{
    [HttpGet("logs")]
    [MapToApiVersion("1.0")]
    [Authorize(Policy = ApiPermissions.ExperimentalRead)]
    [ProducesResponseType(typeof(ExperimentalSpecLogsResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ExperimentalSpecLogsResponse>> GetLogs(CancellationToken cancellationToken)
    {
        var result = await experimentalSpecLogsService.GetAllAsync(cancellationToken);
        if (!result.Success)
        {
            return Problem(detail: result.Message, statusCode: result.StatusCode);
        }

        return Ok(result.Data);
    }

    [HttpPost("logs/truncate")]
    [MapToApiVersion("1.0")]
    [Authorize(Policy = ApiPermissions.ExperimentalLogsTruncate)]
    [ProducesResponseType(typeof(ExperimentalSpecLogsResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ExperimentalSpecLogsResponse>> TruncateLogs(CancellationToken cancellationToken)
    {
        var result = await experimentalSpecLogsService.TruncateAsync(cancellationToken);
        if (!result.Success)
        {
            return Problem(detail: result.Message, statusCode: result.StatusCode);
        }

        return Ok(result.Data);
    }

    [HttpPost("logs/truncate-last")]
    [MapToApiVersion("1.0")]
    [Authorize(Policy = ApiPermissions.ExperimentalLogsTruncate)]
    [ProducesResponseType(typeof(ExperimentalSpecLogsResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ExperimentalSpecLogsResponse>> TruncateLogsLast(CancellationToken cancellationToken)
    {
        var result = await experimentalSpecLogsService.TruncateLastAsync(cancellationToken);
        if (!result.Success)
        {
            return Problem(detail: result.Message, statusCode: result.StatusCode);
        }

        return Ok(result.Data);
    }

    [HttpPost("logs/clear")]
    [MapToApiVersion("1.0")]
    [Authorize(Policy = ApiPermissions.ExperimentalLogsClear)]
    [ProducesResponseType(typeof(ExperimentalSpecLogsResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ExperimentalSpecLogsResponse>> ClearLogs(CancellationToken cancellationToken)
    {
        var result = await experimentalSpecLogsService.ClearAsync(cancellationToken);
        if (!result.Success)
        {
            return Problem(detail: result.Message, statusCode: result.StatusCode);
        }

        return Ok(result.Data);
    }

    [HttpGet("spec-weights/leaderboard")]
    [MapToApiVersion("1.0")]
    [Authorize(Policy = ApiPermissions.ExperimentalRead)]
    [ProducesResponseType(typeof(Dictionary<string, IReadOnlyList<SpecWeightLeaderboardEntry>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<Dictionary<string, IReadOnlyList<SpecWeightLeaderboardEntry>>>> GetSpecWeightLeaderboard(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        CancellationToken cancellationToken = default)
    {
        if (page < 1)
        {
            return Problem(
                detail: "page must be greater than or equal to 1.",
                statusCode: StatusCodes.Status400BadRequest);
        }

        if (pageSize is < 1 or > 100)
        {
            return Problem(
                detail: "pageSize must be between 1 and 100.",
                statusCode: StatusCodes.Status400BadRequest);
        }

        var result = await specWeightLeaderboardService.GetLeaderboardAsync(page, pageSize, cancellationToken);
        return Ok(result);
    }

    [HttpGet("spec-weights/{nameOrUuid}")]
    [MapToApiVersion("1.0")]
    [Authorize(Policy = ApiPermissions.ExperimentalRead)]
    [ProducesResponseType(typeof(SpecWeightsResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<SpecWeightsResponse>> GetSpecWeights(
        string nameOrUuid,
        CancellationToken cancellationToken)
    {
        var resolved = await playerKeyResolver.ResolveAsync(nameOrUuid, cancellationToken);
        var problem = ProblemFrom(resolved);
        if (problem is not null)
        {
            return problem;
        }

        var result = await specWeightsService.GetCombinedAsync(resolved.Uuid!.Value, cancellationToken);
        if (result is null)
        {
            return Problem(
                detail: "The requested resource was not found.",
                statusCode: StatusCodes.Status404NotFound);
        }

        return Ok(result);
    }

    [HttpGet("spec-bans/{nameOrUuid}")]
    [MapToApiVersion("1.0")]
    [Authorize(Policy = ApiPermissions.ExperimentalRead)]
    [ProducesResponseType(typeof(ExperimentalSpecBansResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ExperimentalSpecBansResponse>> GetSpecBans(
        string nameOrUuid,
        CancellationToken cancellationToken)
    {
        var resolved = await playerKeyResolver.ResolveAsync(nameOrUuid, cancellationToken);
        var problem = ProblemFrom(resolved);
        if (problem is not null)
        {
            return problem;
        }

        var result = await experimentalSpecBanService.GetBansAsync(resolved.Uuid!.Value, cancellationToken);
        if (!result.Success)
        {
            return Problem(detail: result.Message, statusCode: result.StatusCode);
        }

        return Ok(result.Data);
    }

    [HttpPost("spec-bans/ban/{nameOrUuid}")]
    [MapToApiVersion("1.0")]
    [Authorize(Policy = ApiPermissions.ExperimentalSpecBansWrite)]
    [ProducesResponseType(typeof(ExperimentalSpecBansResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ExperimentalSpecBansResponse>> BanSpec(
        string nameOrUuid,
        [FromBody] ExperimentalSpecBanRequest? request,
        CancellationToken cancellationToken)
    {
        return await SetSpecBanAsync(nameOrUuid, request, banned: true, cancellationToken);
    }

    [HttpPost("spec-bans/unban/{nameOrUuid}")]
    [MapToApiVersion("1.0")]
    [Authorize(Policy = ApiPermissions.ExperimentalSpecBansWrite)]
    [ProducesResponseType(typeof(ExperimentalSpecBansResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ExperimentalSpecBansResponse>> UnbanSpec(
        string nameOrUuid,
        [FromBody] ExperimentalSpecBanRequest? request,
        CancellationToken cancellationToken)
    {
        return await SetSpecBanAsync(nameOrUuid, request, banned: false, cancellationToken);
    }

    private async Task<ActionResult<ExperimentalSpecBansResponse>> SetSpecBanAsync(
        string nameOrUuid,
        ExperimentalSpecBanRequest? request,
        bool banned,
        CancellationToken cancellationToken)
    {
        var canonicalSpec = ManualWeightAdjustmentService.TryNormalizeSpec(request?.Spec);
        if (canonicalSpec is null)
        {
            return Problem(
                detail: "Unknown or missing spec.",
                statusCode: StatusCodes.Status400BadRequest);
        }

        var resolved = await playerKeyResolver.ResolveAsync(nameOrUuid, cancellationToken);
        var problem = ProblemFrom(resolved);
        if (problem is not null)
        {
            return problem;
        }

        var result = await experimentalSpecBanService.SetBanAsync(
            resolved.Uuid!.Value,
            canonicalSpec,
            banned,
            cancellationToken);
        if (!result.Success)
        {
            return Problem(detail: result.Message, statusCode: result.StatusCode);
        }

        return Ok(result.Data);
    }

    [HttpPost("request-spec/{nameOrUuid}")]
    [MapToApiVersion("1.0")]
    [Authorize(Policy = ApiPermissions.ExperimentalRequestSpecWrite)]
    [ProducesResponseType(typeof(ExperimentalSpecRequestResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ExperimentalSpecRequestResponse>> RequestSpec(
        string nameOrUuid,
        [FromBody] ExperimentalSpecRequestBody? request,
        CancellationToken cancellationToken)
    {
        var canonicalSpec = ManualWeightAdjustmentService.TryNormalizeSpec(request?.Spec);
        if (canonicalSpec is null)
        {
            return Problem(
                detail: "Unknown or missing spec.",
                statusCode: StatusCodes.Status400BadRequest);
        }

        var resolved = await playerKeyResolver.ResolveAsync(nameOrUuid, cancellationToken);
        var problem = ProblemFrom(resolved);
        if (problem is not null)
        {
            return problem;
        }

        var result = await experimentalSpecRequestService.UpsertAsync(
            resolved.Uuid!.Value,
            canonicalSpec,
            cancellationToken);
        if (!result.Success)
        {
            return Problem(detail: result.Message, statusCode: result.StatusCode);
        }

        return Ok(result.Data);
    }

    [HttpGet("daily/{name}")]
    [MapToApiVersion("1.0")]
    [Authorize(Policy = ApiPermissions.ExperimentalRead)]
    [ProducesResponseType(typeof(ExperimentalDailyStatsResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ExperimentalDailyStatsResponse>> GetDaily(
        string name,
        [FromQuery] int? id,
        CancellationToken cancellationToken)
    {
        var resolved = await playerKeyResolver.ResolveAsync(name, cancellationToken);
        var problem = ProblemFrom(resolved);
        if (problem is not null)
        {
            return problem;
        }

        if (id is not null)
        {
            var dayExists = await dbContext.TimeDays
                .AsNoTracking()
                .AnyAsync(x => x.Id == id.Value, cancellationToken);
            if (!dayExists)
            {
                return Problem(
                    detail: $"No day found with id {id.Value}.",
                    statusCode: StatusCodes.Status404NotFound);
            }

            var historical = await dbContext.GetExperimentalDailyStatsForDayAsync(
                id.Value,
                resolved.Uuid!.Value,
                cancellationToken);

            return Ok(new ExperimentalDailyStatsResponse(
                historical?.Wins ?? 0,
                historical?.Losses ?? 0,
                historical?.Kills ?? 0,
                historical?.Deaths ?? 0));
        }

        var row = await dbContext.ExperimentalDailyStats
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Uuid == resolved.Uuid, cancellationToken);

        return Ok(new ExperimentalDailyStatsResponse(
            row?.Wins ?? 0,
            row?.Losses ?? 0,
            row?.Kills ?? 0,
            row?.Deaths ?? 0));
    }

    [HttpGet("daily-all")]
    [MapToApiVersion("1.0")]
    [Authorize(Policy = ApiPermissions.ExperimentalRead)]
    [ProducesResponseType(typeof(ExperimentalDailyAllStatsResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ExperimentalDailyAllStatsResponse>> GetDailyAll(
        [FromQuery] int? id,
        CancellationToken cancellationToken)
    {
        if (id is not null)
        {
            var dayExists = await dbContext.TimeDays
                .AsNoTracking()
                .AnyAsync(x => x.Id == id.Value, cancellationToken);
            if (!dayExists)
            {
                return Problem(
                    detail: $"No day found with id {id.Value}.",
                    statusCode: StatusCodes.Status404NotFound);
            }

            var historical = await dbContext.ExperimentalDailyStatsDay
                .AsNoTracking()
                .Where(x => x.DayStartDate == id.Value)
                .Join(
                    dbContext.Names,
                    s => s.Uuid,
                    n => n.Uuid,
                    (s, n) => new { n.Name, s.Wins, s.Losses, s.Kills, s.Deaths })
                .Where(x => x.Wins + x.Losses > 0)
                .OrderByDescending(x => x.Wins - x.Losses)
                .Select(x => new ExperimentalDailyAllStatsEntry(x.Name, x.Wins, x.Losses, x.Kills, x.Deaths))
                .ToListAsync(cancellationToken);

            return Ok(new ExperimentalDailyAllStatsResponse(historical));
        }

        var rows = await dbContext.ExperimentalDailyStats
            .AsNoTracking()
            .Join(
                dbContext.Names,
                s => s.Uuid,
                n => n.Uuid,
                (s, n) => new { n.Name, s.Wins, s.Losses, s.Kills, s.Deaths })
            .Where(x => x.Wins + x.Losses > 0)
            .OrderByDescending(x => x.Wins - x.Losses)
            .Select(x => new ExperimentalDailyAllStatsEntry(x.Name, x.Wins, x.Losses, x.Kills, x.Deaths))
            .ToListAsync(cancellationToken);

        return Ok(new ExperimentalDailyAllStatsResponse(rows));
    }

    [HttpGet("daily-experimental-specs/{name}")]
    [MapToApiVersion("1.0")]
    [Authorize(Policy = ApiPermissions.ExperimentalRead)]
    [ProducesResponseType(typeof(ExperimentalDailySpecsResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ExperimentalDailySpecsResponse>> GetDailyExperimentalSpecs(
        string name,
        [FromQuery] int? id,
        CancellationToken cancellationToken)
    {
        var resolved = await playerKeyResolver.ResolveAsync(name, cancellationToken);
        var problem = ProblemFrom(resolved);
        if (problem is not null)
        {
            return problem;
        }

        if (id is not null)
        {
            var dayExists = await dbContext.TimeDays
                .AsNoTracking()
                .AnyAsync(x => x.Id == id.Value, cancellationToken);
            if (!dayExists)
            {
                return Problem(
                    detail: $"No day found with id {id.Value}.",
                    statusCode: StatusCodes.Status404NotFound);
            }

            var historical = await dbContext.GetExperimentalSpecsWlForDayAsync(
                id.Value,
                resolved.Uuid!.Value,
                cancellationToken);

            return Ok(MapExperimentalDailySpecs(historical));
        }

        var row = await dbContext.ExperimentalSpecsWlCurrentDay
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Uuid == resolved.Uuid, cancellationToken);

        return Ok(MapExperimentalDailySpecs(row));
    }

    [HttpGet("weekly/{name}")]
    [MapToApiVersion("1.0")]
    [Authorize(Policy = ApiPermissions.ExperimentalRead)]
    [ProducesResponseType(typeof(ExperimentalWeeklyStatsResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ExperimentalWeeklyStatsResponse>> GetWeekly(
        string name,
        [FromQuery] int? id,
        CancellationToken cancellationToken)
    {
        var resolved = await playerKeyResolver.ResolveAsync(name, cancellationToken);
        var problem = ProblemFrom(resolved);
        if (problem is not null)
        {
            return problem;
        }

        if (id is not null)
        {
            var weekExists = await dbContext.TimeWeeks
                .AsNoTracking()
                .AnyAsync(x => x.Id == id.Value, cancellationToken);
            if (!weekExists)
            {
                return Problem(
                    detail: $"No week found with id {id.Value}.",
                    statusCode: StatusCodes.Status404NotFound);
            }

            var historical = await dbContext.GetExperimentalWeeklyStatsForWeekAsync(
                id.Value,
                resolved.Uuid!.Value,
                cancellationToken);

            return Ok(new ExperimentalWeeklyStatsResponse(
                historical?.Wins ?? 0,
                historical?.Losses ?? 0,
                historical?.Kills ?? 0,
                historical?.Deaths ?? 0));
        }

        var row = await dbContext.ExperimentalWeeklyStats
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Uuid == resolved.Uuid, cancellationToken);

        return Ok(new ExperimentalWeeklyStatsResponse(
            row?.Wins ?? 0,
            row?.Losses ?? 0,
            row?.Kills ?? 0,
            row?.Deaths ?? 0));
    }

    [HttpGet("weekly-all")]
    [MapToApiVersion("1.0")]
    [Authorize(Policy = ApiPermissions.ExperimentalRead)]
    [ProducesResponseType(typeof(ExperimentalWeeklyAllStatsResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ExperimentalWeeklyAllStatsResponse>> GetWeeklyAll(
        [FromQuery] int? id,
        CancellationToken cancellationToken)
    {
        if (id is not null)
        {
            var weekExists = await dbContext.TimeWeeks
                .AsNoTracking()
                .AnyAsync(x => x.Id == id.Value, cancellationToken);
            if (!weekExists)
            {
                return Problem(
                    detail: $"No week found with id {id.Value}.",
                    statusCode: StatusCodes.Status404NotFound);
            }

            var historical = await dbContext.ExperimentalWeeklyStatsWeek
                .AsNoTracking()
                .Where(x => x.WeekStartDate == id.Value)
                .Join(
                    dbContext.Names,
                    s => s.Uuid,
                    n => n.Uuid,
                    (s, n) => new { n.Name, s.Wins, s.Losses, s.Kills, s.Deaths })
                .Where(x => x.Wins + x.Losses > 0)
                .OrderByDescending(x => x.Wins - x.Losses)
                .Select(x => new ExperimentalWeeklyAllStatsEntry(x.Name, x.Wins, x.Losses, x.Kills, x.Deaths))
                .ToListAsync(cancellationToken);

            return Ok(new ExperimentalWeeklyAllStatsResponse(historical));
        }

        var rows = await dbContext.ExperimentalWeeklyStats
            .AsNoTracking()
            .Join(
                dbContext.Names,
                s => s.Uuid,
                n => n.Uuid,
                (s, n) => new { n.Name, s.Wins, s.Losses, s.Kills, s.Deaths })
            .Where(x => x.Wins + x.Losses > 0)
            .OrderByDescending(x => x.Wins - x.Losses)
            .Select(x => new ExperimentalWeeklyAllStatsEntry(x.Name, x.Wins, x.Losses, x.Kills, x.Deaths))
            .ToListAsync(cancellationToken);

        return Ok(new ExperimentalWeeklyAllStatsResponse(rows));
    }

    [HttpGet("weekly-experimental-specs/{name}")]
    [MapToApiVersion("1.0")]
    [Authorize(Policy = ApiPermissions.ExperimentalRead)]
    [ProducesResponseType(typeof(ExperimentalWeeklySpecsResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ExperimentalWeeklySpecsResponse>> GetWeeklyExperimentalSpecs(
        string name,
        [FromQuery] int? id,
        CancellationToken cancellationToken)
    {
        var resolved = await playerKeyResolver.ResolveAsync(name, cancellationToken);
        var problem = ProblemFrom(resolved);
        if (problem is not null)
        {
            return problem;
        }

        if (id is not null)
        {
            var weekExists = await dbContext.TimeWeeks
                .AsNoTracking()
                .AnyAsync(x => x.Id == id.Value, cancellationToken);
            if (!weekExists)
            {
                return Problem(
                    detail: $"No week found with id {id.Value}.",
                    statusCode: StatusCodes.Status404NotFound);
            }

            var historical = await dbContext.GetExperimentalSpecsWlForWeekAsync(
                id.Value,
                resolved.Uuid!.Value,
                cancellationToken);

            return Ok(MapExperimentalWeeklySpecs(historical));
        }

        var row = await dbContext.ExperimentalSpecsWlCurrentWeek
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Uuid == resolved.Uuid, cancellationToken);

        return Ok(MapExperimentalWeeklySpecs(row));
    }

    [HttpPost("balance")]
    [MapToApiVersion("1.0")]
    [Authorize(Policy = ApiPermissions.ExperimentalBalance)]
    [ProducesResponseType(typeof(ExperimentalBalanceResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ExperimentalBalanceResponse>> Balance(
        [FromBody] ExperimentalBalanceInputRequest request,
        CancellationToken cancellationToken)
    {
        var players = request.Players ?? [];
        var resolved = await playerKeyResolver.ResolveManyAsync(players, cancellationToken);
        if (!resolved.Success || resolved.Uuids is null)
        {
            return Problem(detail: resolved.Message, statusCode: resolved.StatusCode);
        }

        var result = await experimentalBalanceService.BalanceAsync(
            new ExperimentalBalanceRequest(resolved.Uuids),
            cancellationToken);
        if (result.Success && result.Data is not null)
        {
            return Ok(result.Data);
        }

        var err = result.Error!;
        return Problem(detail: err.Message, statusCode: err.StatusCode);
    }

    [HttpPost("balance/{balanceId:guid}/confirm")]
    [MapToApiVersion("1.0")]
    [Authorize(Policy = ApiPermissions.ExperimentalConfirm)]
    [ProducesResponseType(typeof(ExperimentalBalanceConfirmResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ExperimentalBalanceConfirmResponse>> ConfirmBalance(
        Guid balanceId,
        CancellationToken cancellationToken)
    {
        var result = await experimentalBalanceConfirmService.ConfirmAsync(balanceId, cancellationToken);
        if (result.Success)
        {
            return Ok(new ExperimentalBalanceConfirmResponse(balanceId));
        }

        return Problem(detail: result.Message, statusCode: result.StatusCode);
    }

    [HttpPost("balance/{balanceId:guid}/unconfirm")]
    [MapToApiVersion("1.0")]
    [Authorize(Policy = ApiPermissions.ExperimentalConfirm)]
    [ProducesResponseType(typeof(ExperimentalBalanceConfirmResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ExperimentalBalanceConfirmResponse>> UnconfirmBalance(
        Guid balanceId,
        CancellationToken cancellationToken)
    {
        var result = await experimentalBalanceConfirmService.UnconfirmAsync(balanceId, cancellationToken);
        if (result.Success)
        {
            return Ok(new ExperimentalBalanceConfirmResponse(balanceId));
        }

        return Problem(detail: result.Message, statusCode: result.StatusCode);
    }

    [HttpGet("balance/{balanceId:guid}/generate-input")]
    [MapToApiVersion("1.0")]
    [Authorize(Policy = ApiPermissions.ExperimentalRead)]
    [ProducesResponseType(typeof(ExperimentalBalanceInputBody), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ExperimentalBalanceInputBody>> GenerateInputBalance(
        Guid balanceId,
        CancellationToken cancellationToken)
    {
        var log = await dbContext.ExperimentalBalanceLogs
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.BalanceId == balanceId, cancellationToken);
        if (log is null)
        {
            return Problem(
                detail: "The requested resource was not found.",
                statusCode: StatusCodes.Status404NotFound);
        }

        var built = ExperimentalBalanceMockInputBodyBuilder.TryBuild(log.Balance);
        if (!built.Success)
        {
            return Problem(detail: built.Error, statusCode: StatusCodes.Status400BadRequest);
        }

        return Ok(built.Body!);
    }

    [HttpPost("balance/{balanceId:guid}/input")]
    [MapToApiVersion("1.0")]
    [Authorize(Policy = ApiPermissions.ExperimentalInput)]
    [ProducesResponseType(typeof(ExperimentalBalanceInputResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ExperimentalBalanceInputResponse>> InputBalance(
        Guid balanceId,
        [FromBody] ExperimentalBalanceInputBody? body,
        CancellationToken cancellationToken)
    {
        if (body is null)
        {
            return Problem(
                detail: "Request body is required.",
                statusCode: StatusCodes.Status400BadRequest);
        }

        var result = await experimentalBalanceInputService.InputAsync(balanceId, body, cancellationToken);
        if (result.Success)
        {
            return Ok(result.Response!);
        }

        return Problem(detail: result.Message, statusCode: result.StatusCode);
    }

    [HttpPost("balance/{balanceId:guid}/uninput")]
    [MapToApiVersion("1.0")]
    [Authorize(Policy = ApiPermissions.ExperimentalInput)]
    [ProducesResponseType(typeof(ExperimentalBalanceInputResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ExperimentalBalanceInputResponse>> UninputBalance(
        Guid balanceId,
        [FromBody] ExperimentalBalanceInputBody? body,
        CancellationToken cancellationToken)
    {
        if (body is null)
        {
            return Problem(
                detail: "Request body is required.",
                statusCode: StatusCodes.Status400BadRequest);
        }

        var result = await experimentalBalanceInputService.UninputAsync(balanceId, body, cancellationToken);
        if (result.Success)
        {
            return Ok(result.Response!);
        }

        return Problem(detail: result.Message, statusCode: result.StatusCode);
    }

    [HttpPost("balance/{balanceId:guid}/clear-input")]
    [MapToApiVersion("1.0")]
    [Authorize(Policy = ApiPermissions.ExperimentalInput)]
    [ProducesResponseType(typeof(ExperimentalBalanceInputResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ExperimentalBalanceInputResponse>> ClearInputBalance(
        Guid balanceId,
        CancellationToken cancellationToken)
    {
        var result = await experimentalBalanceInputService.ClearInputAsync(balanceId, cancellationToken);
        if (result.Success)
        {
            return Ok(result.Response!);
        }

        return Problem(detail: result.Message, statusCode: result.StatusCode);
    }

    private ActionResult? ProblemFrom(PlayerKeyResolveResult resolved) =>
        resolved is { Success: true, Uuid: not null }
            ? null
            : Problem(detail: resolved.Message, statusCode: resolved.StatusCode);

    private static ExperimentalDailySpecsResponse MapExperimentalDailySpecs(object? row)
    {
        var mapped = MapExperimentalAllSpecs(row);
        return new ExperimentalDailySpecsResponse(
            mapped.Specs.Select(s => new ExperimentalDailySpecStatsEntry(s.Spec, s.Wins, s.Losses, s.Kills, s.Deaths)).ToList(),
            new ExperimentalDailySpecStatsEntry(
                mapped.Total.Spec,
                mapped.Total.Wins,
                mapped.Total.Losses,
                mapped.Total.Kills,
                mapped.Total.Deaths));
    }

    private static ExperimentalWeeklySpecsResponse MapExperimentalWeeklySpecs(object? row)
    {
        var mapped = MapExperimentalAllSpecs(row);
        return new ExperimentalWeeklySpecsResponse(
            mapped.Specs.Select(s => new ExperimentalWeeklySpecStatsEntry(s.Spec, s.Wins, s.Losses, s.Kills, s.Deaths)).ToList(),
            new ExperimentalWeeklySpecStatsEntry(
                mapped.Total.Spec,
                mapped.Total.Wins,
                mapped.Total.Losses,
                mapped.Total.Kills,
                mapped.Total.Deaths));
    }

    private static (List<ExperimentalSpecStatsEntryData> Specs, ExperimentalSpecStatsEntryData Total) MapExperimentalAllSpecs(
        object? row)
    {
        var specs = new List<ExperimentalSpecStatsEntryData>(ExperimentalSpecs.AllOrdered.Length);
        var totalWins = 0;
        var totalLosses = 0;
        var totalKills = 0;
        var totalDeaths = 0;

        foreach (var spec in ExperimentalSpecs.AllOrdered)
        {
            var wins = GetWlStat(row, spec, "Wins");
            var losses = GetWlStat(row, spec, "Losses");
            var kills = GetWlStat(row, spec, "Kills");
            var deaths = GetWlStat(row, spec, "Deaths");
            specs.Add(new ExperimentalSpecStatsEntryData(spec, wins, losses, kills, deaths));
            totalWins += wins;
            totalLosses += losses;
            totalKills += kills;
            totalDeaths += deaths;
        }

        return (specs, new ExperimentalSpecStatsEntryData("Total", totalWins, totalLosses, totalKills, totalDeaths));
    }

    private sealed record ExperimentalSpecStatsEntryData(
        string Spec,
        int Wins,
        int Losses,
        int Kills,
        int Deaths);

    private static int GetWlStat(object? row, string spec, string statSuffix)
    {
        if (row is null)
        {
            return 0;
        }

        var property = row.GetType().GetProperty(
            $"{spec}{statSuffix}",
            BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
        return property?.GetValue(row) is int value ? value : 0;
    }

    public sealed record ExperimentalDailySpecStatsEntry(
        string Spec,
        int Wins,
        int Losses,
        int Kills,
        int Deaths);

    public sealed record ExperimentalDailySpecsResponse(
        IReadOnlyList<ExperimentalDailySpecStatsEntry> Specs,
        ExperimentalDailySpecStatsEntry Total);

    public sealed record ExperimentalWeeklySpecStatsEntry(
        string Spec,
        int Wins,
        int Losses,
        int Kills,
        int Deaths);

    public sealed record ExperimentalWeeklySpecsResponse(
        IReadOnlyList<ExperimentalWeeklySpecStatsEntry> Specs,
        ExperimentalWeeklySpecStatsEntry Total);

    public sealed record ExperimentalDailyStatsResponse(
        int Wins,
        int Losses,
        int Kills,
        int Deaths);

    public sealed record ExperimentalWeeklyStatsResponse(
        int Wins,
        int Losses,
        int Kills,
        int Deaths);

    public sealed record ExperimentalDailyAllStatsEntry(
        string Name,
        int Wins,
        int Losses,
        int Kills,
        int Deaths);

    public sealed record ExperimentalDailyAllStatsResponse(
        IReadOnlyList<ExperimentalDailyAllStatsEntry> Players);

    public sealed record ExperimentalWeeklyAllStatsEntry(
        string Name,
        int Wins,
        int Losses,
        int Kills,
        int Deaths);

    public sealed record ExperimentalWeeklyAllStatsResponse(
        IReadOnlyList<ExperimentalWeeklyAllStatsEntry> Players);

    public sealed record ExperimentalBalanceInputRequest(
        [property: JsonPropertyName("players")] IReadOnlyList<string> Players);
}
