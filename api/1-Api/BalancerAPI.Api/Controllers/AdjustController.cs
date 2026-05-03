using Asp.Versioning;
using BalancerAPI.Common.Auth;
using BalancerAPI.Business.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BalancerAPI.Api.Controllers;

[ApiVersion("1.0")]
[ApiController]
[Route("api/v{version:apiVersion}/[controller]")]
public class AdjustController(
    IAdjustmentAutoDailyService adjustmentAutoDailyService,
    IManualWeightAdjustmentService manualWeightAdjustmentService) : ControllerBase
{
    [HttpPost("auto-daily")]
    [MapToApiVersion("1.0")]
    [Authorize(Policy = ApiPermissions.AdjustAuto)]
    [ProducesResponseType(typeof(AdjustmentAutoDailyResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<AdjustmentAutoDailyResponse>> AutoDaily(CancellationToken cancellationToken)
    {
        var result = await adjustmentAutoDailyService.ApplyAutoDailyAsync(cancellationToken);
        return Ok(result);
    }

    [HttpPatch("base/{player}")]
    [MapToApiVersion("1.0")]
    [Authorize(Policy = ApiPermissions.AdjustManual)]
    [ProducesResponseType(typeof(ManualBaseAdjustResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ManualBaseAdjustResponse>> PatchBase(
        string player,
        [FromBody] ManualAdjustBaseRequest? body,
        CancellationToken cancellationToken)
    {
        if (body is null)
        {
            return BadRequest("Request body is required.");
        }

        var result = await manualWeightAdjustmentService.PatchBaseAsync(player, body, cancellationToken);
        if (result.Success && result.Response is not null)
        {
            return Ok(result.Response);
        }

        return result.StatusCode switch
        {
            400 => BadRequest(result.Message),
            404 => NotFound(result.Message),
            409 => Conflict(result.Message),
            _ => StatusCode(result.StatusCode, result.Message)
        };
    }

    [HttpPatch("spec/{player}")]
    [MapToApiVersion("1.0")]
    [Authorize(Policy = ApiPermissions.AdjustManual)]
    [ProducesResponseType(typeof(ManualSpecAdjustResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ManualSpecAdjustResponse>> PatchSpec(
        string player,
        [FromBody] ManualAdjustSpecRequest? body,
        CancellationToken cancellationToken)
    {
        if (body is null)
        {
            return BadRequest("Request body is required.");
        }

        var result = await manualWeightAdjustmentService.PatchSpecAsync(player, body, cancellationToken);
        if (result.Success && result.Response is not null)
        {
            return Ok(result.Response);
        }

        return result.StatusCode switch
        {
            400 => BadRequest(result.Message),
            404 => NotFound(result.Message),
            409 => Conflict(result.Message),
            _ => StatusCode(result.StatusCode, result.Message)
        };
    }
}
