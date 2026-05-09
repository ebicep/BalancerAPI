using Asp.Versioning;
using BalancerAPI.Business.Services;
using BalancerAPI.Common.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BalancerAPI.Api.Controllers;

[ApiVersion("1.0")]
[ApiController]
[Route("api/v{version:apiVersion}/[controller]")]
public class PlayerController(IPlayerAddService playerAddService) : ControllerBase
{
    [HttpPost("add")]
    [MapToApiVersion("1.0")]
    [Authorize(Policy = ApiPermissions.PlayersAdd)]
    [ProducesResponseType(typeof(PlayerAddResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<PlayerAddResponse>> Add(
        [FromBody] PlayerAddRequest? body,
        CancellationToken cancellationToken)
    {
        if (body is null)
        {
            return BadRequest("Request body is required.");
        }

        var (success, statusCode, message, payload) = await playerAddService.AddAsync(body.Uuid, body.BaseWeight, cancellationToken);
        if (!success || payload is null)
        {
            return statusCode switch
            {
                400 => BadRequest(message),
                404 => NotFound(message),
                409 => Conflict(message),
                _ => StatusCode(statusCode, message)
            };
        }

        return Ok(new PlayerAddResponse(
            payload.Name,
            payload.Uuid,
            payload.TablesAdded));

    }
}

public sealed record PlayerAddRequest(Guid Uuid, int BaseWeight);

public sealed record PlayerAddResponse(
    string Name,
    Guid Uuid,
    string[] TablesAdded);
