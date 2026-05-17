using Asp.Versioning;
using BalancerAPI.Business.Services;
using BalancerAPI.Common.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BalancerAPI.Api.Controllers;

[ApiVersion("1.0")]
[ApiController]
[Route("api/v{version:apiVersion}/[controller]")]
public class PlayerController(
    IPlayerAddService playerAddService,
    IPlayerDeleteService playerDeleteService) : ControllerBase
{
    [HttpPost("add")]
    [MapToApiVersion("1.0")]
    [Authorize(Policy = ApiPermissions.PlayersAdd)]
    [ProducesResponseType(typeof(PlayerAddResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<PlayerAddResponse>> Add(
        [FromBody] PlayerAddRequest? body,
        CancellationToken cancellationToken)
    {
        if (body is null)
        {
            return Problem(
                detail: "Request body is required.",
                statusCode: StatusCodes.Status400BadRequest);
        }

        var (success, statusCode, message, payload) = await playerAddService.AddAsync(body.Uuid, body.BaseWeight, cancellationToken);
        if (!success || payload is null)
        {
            return Problem(detail: message, statusCode: statusCode);
        }

        return Ok(new PlayerAddResponse(
            payload.Name,
            payload.Uuid,
            payload.TablesAdded));

    }

    [HttpDelete("{uuid:guid}")]
    [MapToApiVersion("1.0")]
    [Authorize(Policy = ApiPermissions.PlayersDelete)]
    [ProducesResponseType(typeof(PlayerDeleteResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PlayerDeleteResponse>> Delete(Guid uuid, CancellationToken cancellationToken)
    {
        var (success, statusCode, message, payload) = await playerDeleteService.DeleteAsync(uuid, cancellationToken);
        if (!success || payload is null)
        {
            return Problem(detail: message, statusCode: statusCode);
        }

        return Ok(new PlayerDeleteResponse(
            payload.Name,
            payload.Uuid,
            payload.TablesRemoved,
            payload.Data));
    }
}

public sealed record PlayerAddRequest(Guid Uuid, int BaseWeight);

public sealed record PlayerAddResponse(
    string Name,
    Guid Uuid,
    string[] TablesAdded);

public sealed record PlayerDeleteResponse(
    string Name,
    Guid Uuid,
    string[] TablesRemoved,
    IReadOnlyDictionary<string, object> Data);
