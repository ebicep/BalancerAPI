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
    IManualWeightAdjustmentService manualWeightAdjustmentService,
    IAdjustmentHistoryService adjustmentHistoryService) : ControllerBase
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

    [HttpPost("undo-auto-daily")]
    [MapToApiVersion("1.0")]
    [Authorize(Policy = ApiPermissions.AdjustAuto)]
    [ProducesResponseType(typeof(AdjustmentAutoDailyResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<AdjustmentAutoDailyResponse>> UndoAutoDaily(
        [FromBody] AdjustmentAutoDailyResponse? body,
        CancellationToken cancellationToken)
    {
        if (body is null)
        {
            return Problem(
                detail: "Request body is required.",
                statusCode: StatusCodes.Status400BadRequest);
        }

        var result = await adjustmentAutoDailyService.UndoAutoDailyAsync(body, cancellationToken);
        if (result.Success && result.Response is not null)
        {
            return Ok(result.Response);
        }

        return Problem(detail: result.Message, statusCode: result.StatusCode);
    }

    [HttpPatch("base/{player}")]
    [MapToApiVersion("1.0")]
    [Authorize(Policy = ApiPermissions.AdjustManual)]
    [ProducesResponseType(typeof(ManualBaseAdjustResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ManualBaseAdjustResponse>> PatchBase(
        string player,
        [FromBody] ManualAdjustBaseRequest? body,
        CancellationToken cancellationToken)
    {
        if (body is null)
        {
            return Problem(
                detail: "Request body is required.",
                statusCode: StatusCodes.Status400BadRequest);
        }

        var result = await manualWeightAdjustmentService.PatchBaseAsync(player, body, cancellationToken);
        if (result.Success && result.Response is not null)
        {
            return Ok(result.Response);
        }

        return Problem(detail: result.Message, statusCode: result.StatusCode);
    }

    [HttpPatch("spec/{player}")]
    [MapToApiVersion("1.0")]
    [Authorize(Policy = ApiPermissions.AdjustManual)]
    [ProducesResponseType(typeof(ManualSpecAdjustResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ManualSpecAdjustResponse>> PatchSpec(
        string player,
        [FromBody] ManualAdjustSpecRequest? body,
        CancellationToken cancellationToken)
    {
        if (body is null)
        {
            return Problem(
                detail: "Request body is required.",
                statusCode: StatusCodes.Status400BadRequest);
        }

        var result = await manualWeightAdjustmentService.PatchSpecAsync(player, body, cancellationToken);
        if (result.Success && result.Response is not null)
        {
            return Ok(result.Response);
        }

        return Problem(detail: result.Message, statusCode: result.StatusCode);
    }

    [HttpGet("history/base/{uuid:guid}")]
    [MapToApiVersion("1.0")]
    [Authorize(Policy = ApiPermissions.AdjustRead)]
    [ProducesResponseType(typeof(AdjustmentBaseHistoryResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AdjustmentBaseHistoryResponse>> GetBaseHistory(
        Guid uuid,
        CancellationToken cancellationToken)
    {
        var result = await adjustmentHistoryService.GetBaseHistoryAsync(uuid, cancellationToken);
        if (result is null)
        {
            return Problem(
                detail: "The requested resource was not found.",
                statusCode: StatusCodes.Status404NotFound);
        }

        return Ok(result);
    }

    [HttpGet("history/spec/{uuid:guid}")]
    [MapToApiVersion("1.0")]
    [Authorize(Policy = ApiPermissions.AdjustRead)]
    [ProducesResponseType(typeof(AdjustmentSpecHistoryResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AdjustmentSpecHistoryResponse>> GetSpecHistory(
        Guid uuid,
        CancellationToken cancellationToken)
    {
        var result = await adjustmentHistoryService.GetSpecHistoryAsync(uuid, cancellationToken);
        if (result is null)
        {
            return Problem(
                detail: "The requested resource was not found.",
                statusCode: StatusCodes.Status404NotFound);
        }

        return Ok(result);
    }
}
