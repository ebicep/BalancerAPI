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
        var resolved = await ResolvePlayerUuidFromNameOrUuidAsync(nameOrUuid, cancellationToken);
        if (!resolved.Success)
        {
            return Problem(detail: resolved.Message, statusCode: resolved.StatusCode);
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
        var resolved = await ResolvePlayerUuidFromNameAsync(name, cancellationToken);
        if (!resolved.Success)
        {
            return Problem(detail: resolved.Message, statusCode: resolved.StatusCode);
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

    [HttpGet("daily-experimental-all/{name}")]
    [MapToApiVersion("1.0")]
    [Authorize(Policy = ApiPermissions.ExperimentalRead)]
    [ProducesResponseType(typeof(ExperimentalDailyAllSpecsResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ExperimentalDailyAllSpecsResponse>> GetDailyExperimentalAll(
        string name,
        [FromQuery] int? id,
        CancellationToken cancellationToken)
    {
        var resolved = await ResolvePlayerUuidFromNameAsync(name, cancellationToken);
        if (!resolved.Success)
        {
            return Problem(detail: resolved.Message, statusCode: resolved.StatusCode);
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

            return Ok(MapExperimentalDailyAllSpecs(historical));
        }

        var row = await dbContext.ExperimentalSpecsWlCurrentDay
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Uuid == resolved.Uuid, cancellationToken);

        return Ok(MapExperimentalDailyAllSpecs(row));
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
        var resolved = await ResolvePlayerUuidFromNameAsync(name, cancellationToken);
        if (!resolved.Success)
        {
            return Problem(detail: resolved.Message, statusCode: resolved.StatusCode);
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
        var resolved = await ResolvePlayerUuidsAsync(request, cancellationToken);
        if (!resolved.Success)
        {
            return Problem(detail: resolved.Message, statusCode: resolved.StatusCode);
        }

        var result = await experimentalBalanceService.BalanceAsync(
            new ExperimentalBalanceRequest(resolved.PlayerUuids!),
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

    private async Task<ResolvePlayersResult> ResolvePlayerUuidsAsync(
        ExperimentalBalanceInputRequest request,
        CancellationToken cancellationToken)
    {
        var players = request.Players ?? [];
        if (players.Count == 0)
        {
            return ResolvePlayersResult.Fail(400, "players must not be empty.");
        }

        var uuids = new List<Guid>(players.Count);
        var namesToResolve = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var player in players)
        {
            if (Guid.TryParse(player, out var uuid))
            {
                uuids.Add(uuid);
            }
            else
            {
                namesToResolve.Add(player.Trim());
            }
        }

        if (namesToResolve.Count == 0)
        {
            return ResolvePlayersResult.Ok(uuids);
        }

        var normalizedNames = namesToResolve
            .Select(x => x.ToLowerInvariant())
            .ToHashSet(StringComparer.Ordinal);

        var rows = await dbContext.Names
            .AsNoTracking()
            .Where(x => normalizedNames.Contains(x.Name.ToLower()))
            .Select(x => new { x.Name, x.Uuid })
            .ToListAsync(cancellationToken);

        var uuidsByName = rows
            .GroupBy(x => x.Name, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.Select(r => r.Uuid).Distinct().ToList(), StringComparer.OrdinalIgnoreCase);

        var ambiguous = uuidsByName
            .Where(x => x.Value.Count > 1)
            .Select(x => x.Key)
            .OrderBy(x => x, StringComparer.OrdinalIgnoreCase)
            .ToList();
        if (ambiguous.Count > 0)
        {
            return ResolvePlayersResult.Fail(
                409,
                $"One or more player names are ambiguous in names table: {string.Join(", ", ambiguous)}.");
        }

        var missing = new List<string>();
        var finalUuids = new List<Guid>(players.Count);
        foreach (var player in players)
        {
            var trimmed = player.Trim();
            if (Guid.TryParse(trimmed, out var uuid))
            {
                finalUuids.Add(uuid);
                continue;
            }

            if (!uuidsByName.TryGetValue(trimmed, out var matches) || matches.Count == 0)
            {
                missing.Add(trimmed);
                continue;
            }

            finalUuids.Add(matches[0]);
        }

        if (missing.Count > 0)
        {
            return ResolvePlayersResult.Fail(
                400,
                $"No matching UUID found in names table for: {string.Join(", ", missing.Distinct(StringComparer.OrdinalIgnoreCase))}.");
        }

        return ResolvePlayersResult.Ok(finalUuids);
    }

    private async Task<ResolveNameResult> ResolvePlayerUuidFromNameOrUuidAsync(
        string nameOrUuid,
        CancellationToken cancellationToken)
    {
        var trimmed = nameOrUuid.Trim();
        if (string.IsNullOrEmpty(trimmed))
        {
            return ResolveNameResult.Fail(400, "nameOrUuid must not be empty.");
        }

        if (Guid.TryParse(trimmed, out var uuid))
        {
            return ResolveNameResult.Ok(uuid);
        }

        return await ResolvePlayerUuidFromNameAsync(trimmed, cancellationToken);
    }

    private async Task<ResolveNameResult> ResolvePlayerUuidFromNameAsync(
        string name,
        CancellationToken cancellationToken)
    {
        var trimmed = name.Trim();
        if (string.IsNullOrEmpty(trimmed))
        {
            return ResolveNameResult.Fail(400, "name must not be empty.");
        }

        var normalized = trimmed.ToLowerInvariant();
        var uuids = await dbContext.Names
            .AsNoTracking()
            .Where(x => x.Name.ToLower() == normalized)
            .Select(x => x.Uuid)
            .Distinct()
            .ToListAsync(cancellationToken);

        if (uuids.Count == 0)
        {
            return ResolveNameResult.Fail(
                400,
                $"No matching UUID found in names table for: {trimmed}.");
        }

        if (uuids.Count > 1)
        {
            return ResolveNameResult.Fail(
                409,
                $"One or more player names are ambiguous in names table: {trimmed}.");
        }

        return ResolveNameResult.Ok(uuids[0]);
    }

    private static ExperimentalDailyAllSpecsResponse MapExperimentalDailyAllSpecs(object? row)
    {
        var specs = new List<ExperimentalDailySpecStatsEntry>(ExperimentalSpecs.AllOrdered.Length);
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
            specs.Add(new ExperimentalDailySpecStatsEntry(spec, wins, losses, kills, deaths));
            totalWins += wins;
            totalLosses += losses;
            totalKills += kills;
            totalDeaths += deaths;
        }

        return new ExperimentalDailyAllSpecsResponse(
            specs,
            new ExperimentalDailySpecStatsEntry("Total", totalWins, totalLosses, totalKills, totalDeaths));
    }

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

    public sealed record ExperimentalDailyAllSpecsResponse(
        IReadOnlyList<ExperimentalDailySpecStatsEntry> Specs,
        ExperimentalDailySpecStatsEntry Total);

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

    public sealed record ExperimentalBalanceInputRequest(
        [property: JsonPropertyName("players")] IReadOnlyList<string> Players);

    private sealed record ResolvePlayersResult(
        bool Success,
        int StatusCode,
        string? Message,
        IReadOnlyList<Guid>? PlayerUuids)
    {
        public static ResolvePlayersResult Ok(IReadOnlyList<Guid> uuids) =>
            new(true, StatusCodes.Status200OK, null, uuids);

        public static ResolvePlayersResult Fail(int statusCode, string message) =>
            new(false, statusCode, message, null);
    }

    private sealed record ResolveNameResult(bool Success, int StatusCode, string? Message, Guid? Uuid)
    {
        public static ResolveNameResult Ok(Guid uuid) =>
            new(true, StatusCodes.Status200OK, null, uuid);

        public static ResolveNameResult Fail(int statusCode, string message) =>
            new(false, statusCode, message, null);
    }
}
