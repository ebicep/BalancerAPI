using System.Text.Json.Serialization;
using Asp.Versioning;
using BalancerAPI.Business.Services;
using BalancerAPI.Common.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BalancerAPI.Api.Controllers;

[Route("api/v{version:apiVersion}/[controller]")]
[ApiVersion("1.0")]
[ApiController]
public class RegularController(
    IRegularBalanceService regularBalanceService,
    IPlayerKeyResolver playerKeyResolver) : ControllerBase
{
    [HttpPost("balance")]
    [MapToApiVersion("1.0")]
    [Authorize(Policy = ApiPermissions.RegularBalance)]
    [ProducesResponseType(typeof(RegularBalanceResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<RegularBalanceResponse>> Balance(
        [FromBody] RegularBalanceInputRequest request,
        CancellationToken cancellationToken)
    {
        var players = request.Players ?? [];
        var resolved = await playerKeyResolver.ResolveManyAsync(players, cancellationToken);
        if (!resolved.Success || resolved.Uuids is null)
        {
            return Problem(detail: resolved.Message, statusCode: resolved.StatusCode);
        }

        var result = await regularBalanceService.BalanceAsync(
            new RegularBalanceRequest(resolved.Uuids),
            cancellationToken);

        if (!result.Success)
        {
            return result.Error!.StatusCode switch
            {
                400 => BadRequest(new { error = result.Error.Message }),
                404 => NotFound(new { error = result.Error.Message, missingUuids = result.Error.MissingUuids }),
                409 => Conflict(new { error = result.Error.Message }),
                _ => StatusCode(result.Error.StatusCode, new { error = result.Error.Message })
            };
        }

        return Ok(result.Data);
    }

    public sealed record RegularBalanceInputRequest(
        [property: JsonPropertyName("players")] IReadOnlyList<string>? Players);
}
