using System.Text.Json.Serialization;
using Asp.Versioning;
using BalancerAPI.Common.Auth;
using BalancerAPI.Business.Services;
using BalancerAPI.Data.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.EntityFrameworkCore;

namespace BalancerAPI.Api.Controllers;

[ApiVersion("1.0")]
[ApiController]
[Route("api/v{version:apiVersion}/[controller]")]
public class ExperimentalController(
    ISpecWeightsService specWeightsService,
    IExperimentalBalanceService experimentalBalanceService,
    IExperimentalBalanceConfirmService experimentalBalanceConfirmService,
    IExperimentalBalanceInputService experimentalBalanceInputService,
    BalancerDbContext dbContext) : ControllerBase
{
    [HttpGet("spec-weights/{uuid:guid}")]
    [MapToApiVersion("1.0")]
    [Authorize(Policy = ApiPermissions.ExperimentalRead)]
    [ProducesResponseType(typeof(SpecWeightsResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<SpecWeightsResponse>> GetSpecWeights(Guid uuid, CancellationToken cancellationToken)
    {
        var result = await specWeightsService.GetCombinedAsync(uuid, cancellationToken);
        if (result is null)
        {
            return NotFound();
        }

        return Ok(result);
    }

    [HttpPost("balance")]
    [MapToApiVersion("1.0")]
    [Authorize(Policy = ApiPermissions.ExperimentalBalance)]
    [ProducesResponseType(typeof(ExperimentalBalanceResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ExperimentalBalanceResponse>> Balance(
        [FromBody] ExperimentalBalanceInputRequest request,
        CancellationToken cancellationToken)
    {
        var resolved = await ResolvePlayerUuidsAsync(request, cancellationToken);
        if (!resolved.Success)
        {
            return resolved.StatusCode switch
            {
                400 => BadRequest(resolved.Message),
                409 => Conflict(resolved.Message),
                _ => StatusCode(resolved.StatusCode, resolved.Message)
            };
        }

        var result = await experimentalBalanceService.BalanceAsync(
            new ExperimentalBalanceRequest(resolved.PlayerUuids!),
            cancellationToken);
        if (result.Success && result.Data is not null)
        {
            return Ok(result.Data);
        }

        var err = result.Error!;
        return err.StatusCode switch
        {
            400 => BadRequest(err.Message),
            404 => NotFound(err),
            409 => Conflict(err.Message),
            _ => StatusCode(err.StatusCode, err.Message)
        };
    }

    [HttpPost("balance/{balanceId:guid}/confirm")]
    [MapToApiVersion("1.0")]
    [Authorize(Policy = ApiPermissions.ExperimentalConfirm)]
    [ProducesResponseType(typeof(ExperimentalBalanceConfirmResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ExperimentalBalanceConfirmResponse>> ConfirmBalance(
        Guid balanceId,
        CancellationToken cancellationToken)
    {
        var result = await experimentalBalanceConfirmService.ConfirmAsync(balanceId, cancellationToken);
        if (result.Success)
        {
            return Ok(new ExperimentalBalanceConfirmResponse(balanceId));
        }

        return StatusCode(result.StatusCode, result.Message);
    }

    [HttpPost("balance/{balanceId:guid}/unconfirm")]
    [MapToApiVersion("1.0")]
    [Authorize(Policy = ApiPermissions.ExperimentalConfirm)]
    [ProducesResponseType(typeof(ExperimentalBalanceConfirmResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ExperimentalBalanceConfirmResponse>> UnconfirmBalance(
        Guid balanceId,
        CancellationToken cancellationToken)
    {
        var result = await experimentalBalanceConfirmService.UnconfirmAsync(balanceId, cancellationToken);
        if (result.Success)
        {
            return Ok(new ExperimentalBalanceConfirmResponse(balanceId));
        }

        return StatusCode(result.StatusCode, result.Message);
    }

    [HttpPost("balance/{balanceId:guid}/input")]
    [MapToApiVersion("1.0")]
    [Authorize(Policy = ApiPermissions.ExperimentalInput)]
    [ProducesResponseType(typeof(ExperimentalBalanceInputResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ExperimentalBalanceInputResponse>> InputBalance(
        Guid balanceId,
        [FromBody] ExperimentalBalanceInputBody? body,
        CancellationToken cancellationToken)
    {
        if (body is null)
        {
            return BadRequest("Request body is required.");
        }

        var result = await experimentalBalanceInputService.InputAsync(balanceId, body, cancellationToken);
        if (result.Success)
        {
            return Ok(result.Response!);
        }

        return StatusCode(result.StatusCode, result.Message);
    }

    [HttpPost("balance/{balanceId:guid}/uninput")]
    [MapToApiVersion("1.0")]
    [Authorize(Policy = ApiPermissions.ExperimentalInput)]
    [ProducesResponseType(typeof(ExperimentalBalanceInputResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ExperimentalBalanceInputResponse>> UninputBalance(
        Guid balanceId,
        [FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)]
        ExperimentalBalanceInputResponse? trajectoryEcho,
        CancellationToken cancellationToken)
    {
        var result = await experimentalBalanceInputService.UninputAsync(balanceId, trajectoryEcho, cancellationToken);
        if (result.Success)
        {
            return Ok(result.Response!);
        }

        return StatusCode(result.StatusCode, result.Message);
    }

    [HttpPost("balance/{balanceId:guid}/clear-input")]
    [MapToApiVersion("1.0")]
    [Authorize(Policy = ApiPermissions.ExperimentalInput)]
    [ProducesResponseType(typeof(ExperimentalBalanceInputResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ExperimentalBalanceInputResponse>> ClearInputBalance(
        Guid balanceId,
        CancellationToken cancellationToken)
    {
        var result = await experimentalBalanceInputService.ClearInputAsync(balanceId, cancellationToken);
        if (result.Success)
        {
            return Ok(result.Response!);
        }

        return StatusCode(result.StatusCode, result.Message);
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
}
