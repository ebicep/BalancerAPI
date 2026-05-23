using Asp.Versioning;
using BalancerAPI.Business.Services;
using BalancerAPI.Common.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BalancerAPI.Api.Controllers;

[ApiVersion("1.0")]
[ApiController]
[Route("api/v{version:apiVersion}/[controller]")]
public class TrajectoryController(ITrajectoryService trajectoryService) : ControllerBase
{
    [HttpGet("list")]
    [MapToApiVersion("1.0")]
    [Authorize(Policy = ApiPermissions.PlayersRead)]
    [ProducesResponseType(typeof(IReadOnlyList<PlayerTrajectoryEntry>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<PlayerTrajectoryEntry>>> List(CancellationToken cancellationToken)
    {
        var list = await trajectoryService.ListAsync(cancellationToken);
        return Ok(list);
    }

    [HttpPost("{player}")]
    [MapToApiVersion("1.0")]
    [Authorize(Policy = ApiPermissions.AdjustManual)]
    [ProducesResponseType(typeof(PlayerTrajectoryEntry), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<PlayerTrajectoryEntry>> Set(
        string player,
        [FromBody] SetTrajectoryRequest? body,
        CancellationToken cancellationToken)
    {
        if (body is null)
        {
            return Problem(
                detail: "Request body is required.",
                statusCode: StatusCodes.Status400BadRequest);
        }

        var result = await trajectoryService.SetAsync(player, body, cancellationToken);
        if (result.Success && result.Response is not null)
        {
            return Ok(result.Response);
        }

        return Problem(detail: result.Message, statusCode: result.StatusCode);
    }
}
