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
    IPlayerGetService playerGetService,
    IPlayerDeleteService playerDeleteService,
    IPlayerUuidUpdateService playerUuidUpdateService) : ControllerBase
{
    [HttpGet("{uuid:guid}")]
    [MapToApiVersion("1.0")]
    [Authorize(Policy = ApiPermissions.PlayersRead)]
    [ProducesResponseType(typeof(PlayerGetResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PlayerGetResponse>> Get(Guid uuid, CancellationToken cancellationToken)
    {
        var (success, statusCode, message, payload) = await playerGetService.GetAsync(uuid, cancellationToken);
        if (!success || payload is null)
        {
            return Problem(detail: message, statusCode: statusCode);
        }

        return Ok(new PlayerGetResponse(payload.Name, payload.Uuid, payload.Data));
    }

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

    [HttpPost("update-uuid")]
    [MapToApiVersion("1.0")]
    [Authorize(Policy = ApiPermissions.PlayersUpdateUuid)]
    [ProducesResponseType(typeof(PlayerUuidUpdateResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<PlayerUuidUpdateResponse>> UpdateUuid(
        [FromBody] PlayerUuidUpdateRequest? body,
        CancellationToken cancellationToken)
    {
        if (body is null)
        {
            return Problem(
                detail: "Request body is required.",
                statusCode: StatusCodes.Status400BadRequest);
        }

        if (body.OldUuid == body.NewUuid)
        {
            return Problem(
                detail: "oldUuid and newUuid must be different.",
                statusCode: StatusCodes.Status400BadRequest);
        }

        var (success, statusCode, message, payload) = await playerUuidUpdateService.UpdateAsync(
            body.OldUuid,
            body.NewUuid,
            cancellationToken);
        if (!success || payload is null)
        {
            return Problem(detail: message, statusCode: statusCode);
        }

        return Ok(new PlayerUuidUpdateResponse(
            payload.Name,
            payload.OldUuid,
            payload.NewUuid,
            payload.TablesUpdated));
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

public sealed record PlayerGetResponse(
    string Name,
    Guid Uuid,
    IReadOnlyDictionary<string, object> Data);

public sealed record PlayerDeleteResponse(
    string Name,
    Guid Uuid,
    string[] TablesRemoved,
    IReadOnlyDictionary<string, object> Data);

public sealed record PlayerUuidUpdateRequest(Guid OldUuid, Guid NewUuid);

public sealed record PlayerUuidUpdateResponse(
    string Name,
    Guid OldUuid,
    Guid NewUuid,
    string[] TablesUpdated);
