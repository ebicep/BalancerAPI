using Asp.Versioning;
using BalancerAPI.Business.Services;
using BalancerAPI.Common.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BalancerAPI.Api.Controllers;

[ApiVersion("1.0")]
[ApiController]
[Route("api/v{version:apiVersion}/[controller]")]
public class StatsController(
    IPlayerWeeklyWeightHistoryService playerWeeklyWeightHistoryService,
    IPlayerKeyResolver playerKeyResolver) : ControllerBase
{
    [HttpGet("weight/{nameOrUuid}")]
    [MapToApiVersion("1.0")]
    [Authorize(Policy = ApiPermissions.ExperimentalRead)]
    [ProducesResponseType(typeof(PlayerWeeklyWeightHistoryResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<PlayerWeeklyWeightHistoryResponse>> GetWeightHistory(
        string nameOrUuid,
        CancellationToken cancellationToken)
    {
        var resolved = await playerKeyResolver.ResolveAsync(nameOrUuid, cancellationToken);
        var problem = ProblemFrom(resolved);
        if (problem is not null)
        {
            return problem;
        }

        var result = await playerWeeklyWeightHistoryService.GetAsync(resolved.Uuid!.Value, cancellationToken);
        if (result is null)
        {
            return Problem(
                detail: "The requested resource was not found.",
                statusCode: StatusCodes.Status404NotFound);
        }

        return Ok(result);
    }

    private ActionResult? ProblemFrom(PlayerKeyResolveResult resolved) =>
        resolved is { Success: true, Uuid: not null }
            ? null
            : Problem(detail: resolved.Message, statusCode: resolved.StatusCode);
}
